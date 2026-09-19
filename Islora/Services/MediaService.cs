using System;
using System.Threading.Tasks;
using Islora.Models;
using Windows.Media.Control;

namespace Islora.Services;

/// <summary>
/// 媒体实时活动：通过系统级媒体会话（SMTC）获取全局正在播放的曲名 / 歌手。
/// </summary>
internal sealed class MediaService : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private double _lastRawPos = -1;
    private DateTime _lastRawTime;
    private readonly System.Threading.SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>媒体会话变化（null 表示当前无会话）。</summary>
    public event Action<MediaSessionInfo?>? SessionChanged;

    public async Task InitializeAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += OnCurrentSessionChanged;
        _manager.SessionsChanged += OnSessionsChanged;
        await RefreshAsync();
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        => _ = RefreshAsync();

    private void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
        => _ = RefreshAsync();

    // 同一播放器内切歌：会话不变、媒体属性变化 → 必须监听会话级事件才能感知
    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        => _ = RefreshAsync();

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        // 串行化：避免多个 WinRT 事件并发触发导致旧会话覆盖新会话、UI 显示错标题
        await _refreshLock.WaitAsync();
        try
        {
            var session = _manager?.GetCurrentSession();
            AttachSession(session);
            if (session is null)
            {
                SessionChanged?.Invoke(null);
                return;
            }

            var props = await session.TryGetMediaPropertiesAsync();
            var playback = session.GetPlaybackInfo();

            SessionChanged?.Invoke(new MediaSessionInfo(
                props?.Title ?? string.Empty,
                props?.Artist ?? string.Empty,
                props?.Thumbnail,
                playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing));
        }
        catch
        {
            // 单路刷新失败不影响其它路（避免 UI 停在旧状态）
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // ---- 播放控制（展开卡片控制按钮调用；无会话时静默忽略） ----

    public async Task TogglePlayPauseAsync()
    {
        if (_session is null) return;
        try { await _session.TryTogglePlayPauseAsync(); }
        catch { }
    }

    public async Task SkipNextAsync()
    {
        if (_session is null) return;
        try { await _session.TrySkipNextAsync(); }
        catch { }
    }

    public async Task SkipPreviousAsync()
    {
        if (_session is null) return;
        try { await _session.TrySkipPreviousAsync(); }
        catch { }
    }

    /// <summary>跳转到指定进度（秒）。源支持才生效，否则静默忽略。</summary>
    public async Task SeekAsync(double seconds)
    {
        if (_session is null) return;
        try { await _session.TryChangePlaybackPositionAsync(TimeSpan.FromSeconds(seconds).Ticks); }
        catch { }
    }

    // ---- 播放进度（系统媒体时间轴） ----

    /// <summary>返回当前播放进度（秒）：(当前, 总时长)；无会话时返回 null。总时长未知时返回 0。</summary>
    public (double Position, double Duration)? GetProgress()
    {
        // 每次取最新当前会话，避免用到过期会话
        var session = _manager?.GetCurrentSession();
        if (session is null) return null;
        try
        {
            var t = session.GetTimelineProperties();
            var end = t.EndTime.TotalSeconds;
            var max = t.MaxSeekTime.TotalSeconds;
            var rawPos = t.Position.TotalSeconds;
            // 总时长优先 EndTime，其次 MaxSeekTime，都未知则为 0
            var dur = end > 0 ? end : (max > 0 ? max : 0);
            var playing = session.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            var now = DateTime.UtcNow;
            var pos = rawPos;
            // 平滑：源位置更新稀疏（如浏览器）时按墙钟外推，让进度条/时间连续走。
            //
            // ⚠️ 基准 _lastRawTime 必须同时覆盖「暂停中」的情况：
            // 它原来只在 rawPos 变化时更新，暂停期间既不外推也不刷新基准，
            // 于是恢复播放的第一帧会把**整个暂停时长**加进位置（暂停 10 分钟 → +600 秒），
            // 位置被截断到总时长，进度条瞬间显示「已播完」。
            if (dur > 0 && playing && _lastRawPos >= 0 && rawPos == _lastRawPos)
            {
                var extrapolated = (now - _lastRawTime).TotalSeconds;
                // 再加一道保险：外推最多 2 秒，避免任何异常基准导致跳变
                if (extrapolated > 0 && extrapolated <= 2.0) pos = rawPos + extrapolated;
            }
            if (rawPos != _lastRawPos || !playing) { _lastRawPos = rawPos; _lastRawTime = now; }

            if (pos < 0) pos = 0;
            if (dur > 0 && pos > dur) pos = dur;
            return (pos, dur);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>跟随当前会话：订阅其切歌/播放状态事件（换会话时先退订旧会话，避免重复触发）。</summary>
    private void AttachSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (ReferenceEquals(_session, session)) return;
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }
        _session = session;
        if (_session is not null)
        {
            _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _session.PlaybackInfoChanged += OnPlaybackInfoChanged;
        }
    }

    public void Dispose()
    {
        if (_manager is not null)
        {
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
            _manager.SessionsChanged -= OnSessionsChanged;
        }
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }
    }
}
