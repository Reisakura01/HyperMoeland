using System;
using HyperMoeland.Interop;

namespace HyperMoeland.Services;

/// <summary>
/// 系统音量监听：轮询主音量，变化时通知界面（用于在岛内显示音量条）。
///
/// 采用轮询而非 WASAPI 回调：避免实现 COM 回调接口（CCW）的复杂度，
/// 100~200ms 的轮询对用户按键的响应已经足够即时。
/// </summary>
internal sealed class VolumeService : IDisposable
{
    private readonly SystemVolume _volume = new();
    private float _lastLevel = -1f;
    private bool _lastMuted;
    private bool _primed;

    /// <summary>音量变化：(0~1 的音量, 是否静音)。</summary>
    public event Action<float, bool>? VolumeChanged;

    /// <summary>轮询一次（由外部定时器调用）。首次读取只做基线，不触发事件。</summary>
    public void Poll()
    {
        if (!_volume.TryRead(out var level, out var muted))
            return;   // 设备切换/初始化失败 → 下个周期重试

        if (!_primed)
        {
            _primed = true;
            _lastLevel = level;
            _lastMuted = muted;
            return;
        }

        if (Math.Abs(level - _lastLevel) > 0.002f || muted != _lastMuted)
        {
            _lastLevel = level;
            _lastMuted = muted;
            VolumeChanged?.Invoke(level, muted);
        }
    }

    /// <summary>当前音量（0~1）；未就绪返回 0。</summary>
    public float CurrentLevel => _lastLevel < 0 ? 0 : _lastLevel;

    public void Dispose() => _volume.Dispose();
}
