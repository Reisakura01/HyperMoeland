namespace HyperMoeland.Models;

/// <summary>一行歌词：时间戳 + 正文（可选翻译）。</summary>
public sealed class LyricsLine
{
    /// <summary>该行出现的时间点（歌曲内偏移）。</summary>
    public TimeSpan Time { get; init; }

    /// <summary>歌词正文。</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>翻译（来自 tlyric；无则为空串）。</summary>
    public string Translation { get; init; } = string.Empty;
}
