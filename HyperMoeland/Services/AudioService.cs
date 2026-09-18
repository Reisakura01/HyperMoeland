using System;
using System.Threading;
using HyperMoeland.Interop;

namespace HyperMoeland.Services;

/// <summary>
/// 音频分析：从 WASAPI 环回采集系统播放的声音，做 1024 点 FFT，
/// 输出 6 个频段的强度（0~1，带自适应归一化），供 UI 做"随音乐律动"的视觉反馈。
///
/// 频段划分（48kHz / 1024 点，bin 宽度约 47Hz）：
///   [2,8) 低音 · [8,20) 低中 · [20,50) 中音 · [50,120) 中高 · [120,280) 高音 · [280,511) 极高
///
/// 自适应归一化：每频段做"峰值保持 + 衰减"，使小声段落也有明显视觉反馈
/// （与 WinIsland 的 adaptive_max 思路一致：衰减 0.995、下限 0.01）。
/// </summary>
internal sealed class AudioService : IDisposable
{
    private const int FftLength = 1024;
    private const int BandCount = 6;
    private const double AdaptiveDecay = 0.995;
    private const float AdaptiveFloor = 0.005f;
    private const float ActiveThreshold = 0.002f;   // 判定"有声音"的峰值阈值

    private static readonly (int Start, int End)[] BandRanges =
    {
        (2, 8), (8, 20), (20, 50), (50, 120), (120, 280), (280, 511),
    };

    private readonly WasapiLoopbackCapture _capture = new();
    private readonly float[] _bands = new float[BandCount];
    private readonly float[] _adaptiveMax = new float[BandCount];
    private readonly object _lock = new();

    private Thread? _analyzer;
    private volatile bool _running;
    private int _sampleRate = 48000;

    // FFT 工作缓冲
    private readonly float[] _window = new float[FftLength];
    private readonly float[] _re = new float[FftLength];
    private readonly float[] _im = new float[FftLength];
    private readonly float[] _magnitude = new float[FftLength / 2 + 1];
    private readonly float[] _hann = new float[FftLength];
    private readonly float[] _pcm = new float[8192];

    // 连续采样环形缓冲：保证每次 FFT 用的是**连续**的 1024 个样本
    // （若用不连续/循环补齐的样本会造成频谱泄漏，导致幅值严重偏低）
    private const int RingSize = 8192;
    private readonly float[] _ring = new float[RingSize];
    private int _ringWrite;
    private int _ringCount;

    /// <summary>当前总体强度（0~1）。</summary>
    public float Level { get; private set; }

    /// <summary>是否有音频活动（用于判断是否"在播放声音"）。</summary>
    public bool IsActive { get; private set; }

    /// <summary>采集是否运行中。</summary>
    public bool IsCapturing => _capture.IsRunning;

    /// <summary>最近错误（诊断用）。</summary>
    public string? LastError => _capture.LastError;

    public AudioService()
    {
        for (int i = 0; i < FftLength; i++)
            _hann[i] = (float)(0.5 * (1 - Math.Cos(2 * Math.PI * i / (FftLength - 1))));
        for (int i = 0; i < BandCount; i++) _adaptiveMax[i] = AdaptiveFloor;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _capture.Start();
        _analyzer = new Thread(AnalyzeLoop) { IsBackground = true, Name = "HyperMoeland.AudioFft" };
        _analyzer.Start();
    }

    public void Stop()
    {
        _running = false;
        try { _analyzer?.Join(1500); } catch { }
        _analyzer = null;
        _capture.Stop();
    }

    /// <summary>把当前 6 个频段强度拷进目标数组（返回拷贝个数）。</summary>
    public int CopyBands(float[] destination)
    {
        lock (_lock)
        {
            int n = Math.Min(destination.Length, BandCount);
            Array.Copy(_bands, destination, n);
            return n;
        }
    }

    private void AnalyzeLoop()
    {
        while (_running)
        {
            int got = _capture.ReadSamples(_pcm);
            if (got <= 0)
            {
                // 无音频包（静音/未播放）→ 频段平滑回落
                DecayBands();
                SetLevels(0f, false);
                Thread.Sleep(30);
                continue;
            }

            _sampleRate = _capture.SampleRate <= 0 ? 48000 : _capture.SampleRate;

            // 追加进环形缓冲
            for (int i = 0; i < got; i++)
            {
                _ring[_ringWrite] = _pcm[i];
                _ringWrite = (_ringWrite + 1) % RingSize;
            }
            _ringCount = Math.Min(_ringCount + got, RingSize);

            if (_ringCount < FftLength)
            {
                Thread.Sleep(10);   // 样本不足一个 FFT 窗口，稍后再来
                continue;
            }

            // 取最近 FFT_LEN 个**连续**样本
            int start = (_ringWrite - FftLength + RingSize) % RingSize;
            for (int i = 0; i < FftLength; i++)
                _window[i] = _ring[(start + i) % RingSize];

            AnalyzeWindow();

            Thread.Sleep(40);   // 约 25fps 的视觉更新率
        }
    }

    /// <summary>对 _window 做加窗 + FFT + 频段聚合 + 自适应归一化。</summary>
    private void AnalyzeWindow()
    {
        for (int i = 0; i < FftLength; i++)
        {
            _re[i] = _window[i] * _hann[i];
            _im[i] = 0f;
        }

        Fft(_re, _im);

        // 幅值归一化：Hann 窗下满幅正弦的峰值谱线约为 N/4
        // → 除以 N/4 后，满幅正弦在对应频段上的读数 ≈ 1.0
        const float scale = FftLength / 4f;
        int bins = FftLength / 2;
        for (int i = 0; i <= bins; i++)
            _magnitude[i] = MathF.Sqrt(_re[i] * _re[i] + _im[i] * _im[i]) / scale;

        float peak = 0f;
        lock (_lock)
        {
            for (int b = 0; b < BandCount; b++)
            {
                var (start, end) = BandRanges[b];
                if (end > bins) end = bins;
                if (start >= end) { _bands[b] = 0f; continue; }

                // 取频段内**最大值**（纯音不会被平均稀释，视觉更灵敏）
                float max = 0f;
                for (int i = start; i < end; i++)
                    if (_magnitude[i] > max) max = _magnitude[i];

                // 自适应增益：峰值保持 + 缓慢衰减（小声段落也能看到起伏）
                if (max > _adaptiveMax[b]) _adaptiveMax[b] = max;
                else _adaptiveMax[b] = (float)(_adaptiveMax[b] * AdaptiveDecay);
                if (_adaptiveMax[b] < AdaptiveFloor) _adaptiveMax[b] = AdaptiveFloor;

                float normalized = max / _adaptiveMax[b];
                if (normalized > 1f) normalized = 1f;
                if (normalized < 0f) normalized = 0f;
                _bands[b] = normalized;
                if (normalized > peak) peak = normalized;
            }
        }

        SetLevels(peak, peak > ActiveThreshold);
    }

    /// <summary>无音频时让频段缓慢回落，避免视觉上"卡住"。</summary>
    private void DecayBands()
    {
        lock (_lock)
        {
            for (int b = 0; b < BandCount; b++)
            {
                _bands[b] *= 0.85f;
                if (_bands[b] < 0.001f) _bands[b] = 0f;
            }
        }
    }

    private void SetLevels(float level, bool active)
    {
        Level = level;
        IsActive = active;
    }

    /// <summary>原地基-2 迭代 FFT（长度须为 2 的幂）。</summary>
    private static void Fft(float[] real, float[] imag)
    {
        int n = real.Length;

        // 位反转置换
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imag[i], imag[j]) = (imag[j], imag[i]);
            }
        }

        // 蝶形运算
        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = -2 * Math.PI / len;
            float wRe = (float)Math.Cos(angle);
            float wIm = (float)Math.Sin(angle);
            int half = len >> 1;

            for (int i = 0; i < n; i += len)
            {
                float curRe = 1f, curIm = 0f;
                for (int k = 0; k < half; k++)
                {
                    int u = i + k;
                    int v = u + half;

                    float vRe = real[v] * curRe - imag[v] * curIm;
                    float vIm = real[v] * curIm + imag[v] * curRe;

                    real[v] = real[u] - vRe;
                    imag[v] = imag[u] - vIm;
                    real[u] += vRe;
                    imag[u] += vIm;

                    float nextRe = curRe * wRe - curIm * wIm;
                    curIm = curRe * wIm + curIm * wRe;
                    curRe = nextRe;
                }
            }
        }
    }

    public void Dispose() => Stop();
}
