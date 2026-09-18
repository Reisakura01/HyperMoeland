namespace HyperMoeland.Core;

/// <summary>灵动岛的状态。</summary>
public enum IslandState
{
    Compact,
    Expanded,
}

/// <summary>胶囊 / 卡片的尺寸（逻辑像素）。</summary>
public static class IslandMetrics
{
    public const double CompactWidth = 232;
    public const double CompactHeight = 46;
    public const double ExpandedWidth = 250;        // 时钟卡
    public const double ExpandedHeight = 150;
    public const double MediaExpandedWidth = 460;   // 媒体大面板（小米超级岛）
    public const double MediaExpandedHeight = 320;  // 含音频频谱 + 歌词区
}
