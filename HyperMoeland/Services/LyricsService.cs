using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HyperMoeland.Models;
using HyperMoeland.Services.Lyrics;

namespace HyperMoeland.Services;

/// <summary>
/// 歌词服务：按曲名/歌手抓取歌词（多源依次回退）、解析为时间轴，并按播放位置给出当前行。
///
/// 位置来源：
///   • 有 SMTC 时间轴（浏览器/Spotify 等）→ 用真实播放位置；
///   • 无时间轴（网易云等）→ 用本地时钟（从切歌开始计时，暂停即停表）。
/// 这样即使源不上报位置，歌词也能大致跟唱。
/// </summary>
internal sealed class LyricsService : IDisposable
{
    private readonly ILyricsProvider[] _providers = { new NetEaseLyricsProvider(), new LrclibLyricsProvider() };
    private readonly Dictionary<string, List<LyricsLine>> _cache = new();
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    private string _currentKey = string.Empty;
    private List<LyricsLine> _lines = new();
    private int _lastIndex = -2;

    // 本地时钟（无时间轴时使用）
    private bool _useLocalClock;
    private double _localElapsed;
    private DateTime _localAnchor;
    private bool _localPlaying;

    /// <summary>歌词或当前行发生变化。</summary>
    public event Action? Changed;

    /// <summary>是否已加载到歌词。</summary>
    public bool HasLyrics => _lines.Count > 0;

    /// <summary>歌词来源名称（诊断用）。</summary>
    public string? Source { get; private set; }

    /// <summary>当前行 / 下一行 / 翻译（无歌词时均为空）。</summary>
    public (string Current, string Next, string Translation, int Index) Snapshot()
    {
        if (_lines.Count == 0) return (string.Empty, string.Empty, string.Empty, -1);
        int i = CurrentIndex();
        if (i < 0) return (string.Empty, _lines.Count > 0 ? _lines[0].Text : string.Empty, string.Empty, -1);

        var cur = _lines[i];
        var next = i + 1 < _lines.Count ? _lines[i + 1].Text : string.Empty;
        return (cur.Text, next, cur.Translation, i);
    }

    /// <summary>切换曲目：重置并异步抓取歌词。</summary>
    public void SetTrack(string? title, string? artist, double durationSeconds, bool hasTimeline)
    {
        var key = $"{title}|{artist}";
        _useLocalClock = !hasTimeline;
        _localElapsed = 0;
        _localAnchor = DateTime.UtcNow;
        _localPlaying = false;
        _lastIndex = -2;

        if (string.Equals(key, _currentKey, StringComparison.Ordinal)) return;
        _currentKey = key;
        _lines = new List<LyricsLine>();
        _lastIndex = -2;
        Changed?.Invoke();

        if (string.IsNullOrWhiteSpace(title)) return;
        _ = FetchAsync(key, title!, artist ?? string.Empty, durationSeconds);
    }

    /// <summary>清空（无媒体时调用）。</summary>
    public void Clear()
    {
        _currentKey = string.Empty;
        _lines = new List<LyricsLine>();
        _lastIndex = -2;
        _localElapsed = 0;
        _localPlaying = false;
        Changed?.Invoke();
    }

    /// <summary>每次进度刷新时调用，更新播放位置（用于歌词同步）。</summary>
    public void Update(double? smtcPositionSeconds, bool playing)
    {
        double pos;

        if (!_useLocalClock && smtcPositionSeconds is double p)
        {
            pos = p;
        }
        else
        {
            // 本地时钟：播放时累加，暂停即停表
            var now = DateTime.UtcNow;
            if (playing)
            {
                if (!_localPlaying) { _localAnchor = now; _localPlaying = true; }
                pos = _localElapsed + (now - _localAnchor).TotalSeconds;
            }
            else
            {
                if (_localPlaying)
                {
                    _localElapsed += (now - _localAnchor).TotalSeconds;
                    _localPlaying = false;
                }
                pos = _localElapsed;
            }
        }

        Position = pos;

        int idx = CurrentIndex();
        if (idx != _lastIndex)
        {
            _lastIndex = idx;
            Changed?.Invoke();
        }
    }

    /// <summary>当前播放位置（秒），供诊断/调试。</summary>
    public double Position { get; private set; }

    private int CurrentIndex()
    {
        if (_lines.Count == 0) return -1;
        var pos = TimeSpan.FromSeconds(Position);

        // 二分查找最后一个 Time <= pos 的行
        int lo = 0, hi = _lines.Count - 1, result = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (_lines[mid].Time <= pos) { result = mid; lo = mid + 1; }
            else hi = mid - 1;
        }
        return result;
    }

    private async Task FetchAsync(string key, string title, string artist, double duration)
    {
        await _fetchLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                Apply(key, cached, null);
                return;
            }

            foreach (var provider in _providers)
            {
                try
                {
                    var result = await provider.FetchAsync(title, artist, duration, CancellationToken.None)
                        .ConfigureAwait(false);
                    if (result is null) continue;

                    var parsed = LrcParser.Parse(result.Lrc, result.Translation);
                    if (parsed.Count == 0) continue;

                    _cache[key] = parsed;
                    Apply(key, parsed, result.Source);
                    return;
                }
                catch
                {
                    // 该源失败 → 尝试下一个
                }
            }

            // 负缓存：避免同一首歌反复请求
            _cache[key] = new List<LyricsLine>();
            Apply(key, new List<LyricsLine>(), null);
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    private void Apply(string key, List<LyricsLine> lines, string? source)
    {
        if (!string.Equals(key, _currentKey, StringComparison.Ordinal)) return;   // 已切歌，丢弃
        _lines = lines;
        Source = source;
        _lastIndex = -2;
        Changed?.Invoke();
    }

    public void Dispose() => _fetchLock.Dispose();
}
