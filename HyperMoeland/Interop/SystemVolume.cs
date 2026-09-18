using System;
using System.Runtime.InteropServices;

namespace HyperMoeland.Interop;

/// <summary>
/// 系统主音量（WASAPI IAudioEndpointVolume）读写访问。
/// 纯 COM 互操作，无第三方依赖。
///
/// 注意：IAudioEndpointVolume 的方法**顺序必须与 vtable 完全一致**，
/// 因此这里声明了全部 18 个方法（即使只用到其中几个）。
/// </summary>
internal sealed class SystemVolume : IDisposable
{
    private const int eRender = 0;
    private const int eConsole = 0;
    private const int CLSCTX_ALL = 0x17;

    private static readonly Guid IID_IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    private IAudioEndpointVolume? _volume;
    private IMMDeviceEnumerator? _enumerator;

    /// <summary>尝试打开默认输出设备的音量接口；成功返回 true。</summary>
    public bool TryOpen()
    {
        if (_volume is not null) return true;
        try
        {
            _enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            var hr = _enumerator.GetDefaultAudioEndpoint(eRender, eConsole, out var device);
            if (hr != 0 || device is null) return false;

            try
            {
                var iid = IID_IAudioEndpointVolume;
                hr = device.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out var obj);
                if (hr != 0 || obj is not IAudioEndpointVolume vol) return false;
                _volume = vol;
                return true;
            }
            finally
            {
                Marshal.ReleaseComObject(device);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>读取主音量（0~1）与静音状态；失败返回 false。</summary>
    public bool TryRead(out float level, out bool muted)
    {
        level = 0f;
        muted = false;
        if (_volume is null && !TryOpen()) return false;

        try
        {
            if (_volume!.GetMasterVolumeLevelScalar(out level) != 0) return false;
            bool m = false;
            if (_volume.GetMute(out m) != 0) return false;
            muted = m;
            if (level < 0f) level = 0f;
            if (level > 1f) level = 1f;
            return true;
        }
        catch
        {
            // 设备切换（如换耳机）后接口可能失效 → 下次重开
            Close();
            return false;
        }
    }

    /// <summary>设置主音量（0~1）。</summary>
    public void SetLevel(float level)
    {
        if (_volume is null) return;
        try { _volume.SetMasterVolumeLevelScalar(Math.Clamp(level, 0f, 1f), IntPtr.Zero); } catch { }
    }

    private void Close()
    {
        if (_volume is not null) { try { Marshal.ReleaseComObject(_volume); } catch { } _volume = null; }
    }

    public void Dispose()
    {
        Close();
        if (_enumerator is not null) { try { Marshal.ReleaseComObject(_enumerator); } catch { } _enumerator = null; }
    }

    // ---- COM 定义 ----

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

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr pNotify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr pNotify);
        [PreserveSig] int GetChannelCount(out uint channelCount);
        [PreserveSig] int SetMasterVolumeLevel(float levelDb, IntPtr eventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, IntPtr eventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, IntPtr eventContext);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, IntPtr eventContext);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, IntPtr eventContext);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
        [PreserveSig] int GetVolumeStepInfo(out uint step, out uint stepCount);
        [PreserveSig] int VolumeStepUp(IntPtr eventContext);
        [PreserveSig] int VolumeStepDown(IntPtr eventContext);
        [PreserveSig] int QueryHardwareSupport(out uint hardwareSupportMask);
        [PreserveSig] int GetVolumeRange(out float min, out float max, out float increment);
    }
}
