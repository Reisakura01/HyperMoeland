using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Islora.Interop;

/// <summary>
/// WASAPI 环回采集（loopback）：抓取「系统正在播放的声音」。
///
/// 用途：为音频可视化提供 PCM 采样（正弦/频谱分析的数据源）。
/// 实现方式：纯 COM 互操作，无第三方依赖（本机 nuget 不可用时也可编译）。
///
/// 参考：Windows Core Audio API（IMMDeviceEnumerator / IAudioClient / IAudioCaptureClient）。
/// </summary>
internal sealed class WasapiLoopbackCapture : IDisposable
{
    // ---- Core Audio 常量 ----
    private const int eRender = 0;              // 输出设备（环回采集的是渲染端）
    private const int eConsole = 0;             // 默认控制角色
    private const int CLSCTX_ALL = 0x17;
    private const int AUDCLNT_SHAREMODE_SHARED = 0;
    private const uint AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000;
    private const long REFTIMES_PER_SEC = 10_000_000;   // 100ns 单位

    private static readonly Guid IID_IAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    private static readonly Guid IID_IAudioCaptureClient = new("C8ADBD64-E71E-48a0-A4DE-185C395CD317");

    private readonly object _lock = new();
    private Thread? _thread;
    private volatile bool _running;

    // 最近一次采集到的单声道样本（供分析线程读取）
    private float[] _mono = new float[0];
    private int _monoCount;

    /// <summary>采样率（Hz）。</summary>
    public int SampleRate { get; private set; } = 48000;

    /// <summary>声道数。</summary>
    public int Channels { get; private set; } = 2;

    /// <summary>是否正在采集。</summary>
    public bool IsRunning => _running;

    /// <summary>最近一次采集错误信息（null 表示无错误）。</summary>
    public string? LastError { get; private set; }

    /// <summary>启动采集线程（非阻塞；内部自行重连设备）。</summary>
    public void Start()
    {
        if (_running) return;
        _running = true;
        _thread = new Thread(CaptureLoop) { IsBackground = true, Name = "Islora.Audio" };
        try { _thread.SetApartmentState(ApartmentState.MTA); } catch { /* 非 Windows 或已启动 */ }
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        try { _thread?.Join(1500); } catch { }
        _thread = null;
    }

    /// <summary>取最近采集的样本副本（返回实际拷贝长度；无数据返回 0）。</summary>
    public int ReadSamples(float[] destination)
    {
        lock (_lock)
        {
            if (_monoCount <= 0) return 0;
            int n = Math.Min(destination.Length, _monoCount);
            Array.Copy(_mono, 0, destination, 0, n);
            _monoCount = 0;   // 消费掉，避免重复分析同一段
            return n;
        }
    }

    // ---- 采集主循环 ----
    private void CaptureLoop()
    {
        while (_running)
        {
            IAudioClient? audioClient = null;
            IAudioCaptureClient? captureClient = null;
            try
            {
                audioClient = CreateLoopbackClient();
                if (audioClient is null)
                {
                    // 保留 CreateLoopbackClient 写入的具体原因，便于诊断
                    if (string.IsNullOrEmpty(LastError)) LastError = "无法创建环回采集客户端";
                    Thread.Sleep(1000);
                    continue;
                }

                var captureIid = IID_IAudioCaptureClient;
                var hr = audioClient.GetService(ref captureIid, out var svc);
                if (hr != 0 || svc is not IAudioCaptureClient cc)
                {
                    LastError = $"GetService(IAudioCaptureClient) 失败 hr=0x{hr:X8}";
                    Thread.Sleep(1000);
                    continue;
                }
                captureClient = cc;

                hr = audioClient.Start();
                if (hr != 0)
                {
                    LastError = $"IAudioClient.Start 失败 hr=0x{hr:X8}";
                    Thread.Sleep(1000);
                    continue;
                }

                LastError = null;
                CapturePackets(captureClient);

                try { audioClient.Stop(); } catch { }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Thread.Sleep(1000);
            }
            finally
            {
                if (captureClient is not null) Marshal.ReleaseComObject(captureClient);
                if (audioClient is not null) Marshal.ReleaseComObject(audioClient);
            }
        }
    }

    private void CapturePackets(IAudioCaptureClient captureClient)
    {
        var scratch = new float[4096];
        while (_running)
        {
            var hr = captureClient.GetNextPacketSize(out uint packetFrames);
            if (hr != 0) { Thread.Sleep(20); continue; }

            if (packetFrames == 0)
            {
                Thread.Sleep(8);   // 无新数据（静音/无播放）
                continue;
            }

            hr = captureClient.GetBuffer(out IntPtr data, out uint frames, out uint flags, out _, out _);
            if (hr != 0) { Thread.Sleep(20); continue; }

            const uint AUDCLNT_BUFFERFLAGS_SILENT = 0x2;
            if ((flags & AUDCLNT_BUFFERFLAGS_SILENT) == 0 && data != IntPtr.Zero && frames > 0)
            {
                int total = (int)frames * Channels;
                if (scratch.Length < total) scratch = new float[total];
                Marshal.Copy(data, scratch, 0, total);
                AppendDownmixed(scratch, (int)frames);
            }

            captureClient.ReleaseBuffer(frames);
        }
    }

    /// <summary>多声道降混为单声道并追加到缓冲。</summary>
    private void AppendDownmixed(float[] interleaved, int frames)
    {
        int ch = Channels <= 0 ? 1 : Channels;
        lock (_lock)
        {
            if (_mono.Length < _monoCount + frames)
            {
                // 保留最近 8192 个样本即可，避免无限增长
                int keep = Math.Min(_monoCount, 4096);
                var next = new float[Math.Max(8192, keep + frames)];
                if (keep > 0) Array.Copy(_mono, _monoCount - keep, next, 0, keep);
                _mono = next;
                _monoCount = keep;
            }

            for (int i = 0; i < frames; i++)
            {
                float sum = 0f;
                int baseIndex = i * ch;
                for (int c = 0; c < ch; c++) sum += interleaved[baseIndex + c];
                _mono[_monoCount++] = sum / ch;
            }
        }
    }

    /// <summary>创建默认渲染设备的环回 IAudioClient（已按混音格式初始化）。</summary>
    private IAudioClient? CreateLoopbackClient()
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();

            var hr = enumerator.GetDefaultAudioEndpoint(eRender, eConsole, out device);
            if (hr != 0 || device is null)
            {
                LastError = $"GetDefaultAudioEndpoint hr=0x{hr:X8}";
                return null;
            }

            var iid = IID_IAudioClient;
            hr = device.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out var obj);
            if (hr != 0 || obj is null)
            {
                LastError = $"IMMDevice.Activate hr=0x{hr:X8}";
                return null;
            }
            if (obj is not IAudioClient client)
            {
                LastError = "Activate 返回对象不是 IAudioClient";
                return null;
            }

            hr = client.GetMixFormat(out IntPtr pFormat);
            if (hr != 0 || pFormat == IntPtr.Zero)
            {
                LastError = $"GetMixFormat hr=0x{hr:X8}";
                Marshal.ReleaseComObject(client);
                return null;
            }

            try
            {
                var format = Marshal.PtrToStructure<WAVEFORMATEX>(pFormat);
                SampleRate = (int)format.nSamplesPerSec;
                Channels = format.nChannels;

                // 注意：必须传**原始指针**（pFormat），不能传 WAVEFORMATEX 结构副本。
                // 设备混音格式通常是 WAVEFORMATEXTENSIBLE（40 字节，cbSize=22），
                // 只传 18 字节基础结构会导致 API 读取越界 → E_INVALIDARG (0x80070057)。
                hr = client.Initialize(AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_LOOPBACK,
                    REFTIMES_PER_SEC, 0, pFormat, IntPtr.Zero);
                if (hr != 0)
                {
                    LastError = $"IAudioClient.Initialize hr=0x{hr:X8} (rate={format.nSamplesPerSec} ch={format.nChannels} bits={format.wBitsPerSample} cbSize={format.cbSize})";
                    Marshal.ReleaseComObject(client);
                    return null;
                }

                return client;
            }
            finally
            {
                Marshal.FreeCoTaskMem(pFormat);
            }
        }
        catch (Exception ex)
        {
            LastError = $"{ex.GetType().Name}: {ex.Message}";
            return null;
        }
        finally
        {
            if (device is not null) Marshal.ReleaseComObject(device);
            if (enumerator is not null) Marshal.ReleaseComObject(enumerator);
        }
    }

    public void Dispose() => Stop();

    // ---- COM 互操作定义 ----

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? endpoint);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice? device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object? ppInterface);
        [PreserveSig] int OpenPropertyStore(int stgmAccess, out IntPtr properties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string? id);
        [PreserveSig] int GetState(out int state);
    }

    [ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long hnsBufferDuration,
            long hnsPeriodicity, IntPtr format, IntPtr audioSessionGuid);
        [PreserveSig] int GetBufferSize(out uint bufferFrameCount);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint padding);
        [PreserveSig] int IsFormatSupported(int shareMode, ref WAVEFORMATEX format, out IntPtr closestMatch);
        [PreserveSig] int GetMixFormat(out IntPtr deviceFormat);
        [PreserveSig] int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
        [PreserveSig] int GetService(ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object? ppv);
    }

    [ComImport, Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
        [PreserveSig] int GetBuffer(out IntPtr data, out uint numFramesToRead, out uint flags,
            out ulong devicePosition, out ulong qpcPosition);
        [PreserveSig] int ReleaseBuffer(uint numFramesRead);
        [PreserveSig] int GetNextPacketSize(out uint numFramesInNextPacket);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    private struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }
}
