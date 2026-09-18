using HyperMoeland.Theme;

namespace HyperMoeland.Models;

/// <summary>应用设置（持久化到 %LOCALAPPDATA%\HyperMoeland\settings.json）。</summary>
public class AppSettings
{
    /// <summary>主题模式：自动（按时间）/ 强制浅色 / 强制深色。</summary>
    public ThemePreference ThemeMode { get; set; } = ThemePreference.Auto;

    /// <summary>日间开始小时（含），默认 6。</summary>
    public int DayStartHour { get; set; } = 6;

    /// <summary>夜间开始小时（含），默认 19。</summary>
    public int NightStartHour { get; set; } = 19;

    /// <summary>开机自启。</summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>启动时检查 GitHub 更新。</summary>
    public bool AutoUpdate { get; set; } = true;

    /// <summary>霓虹动画节拍间隔（毫秒，越小越快），默认 900。</summary>
    public int NeonSpeedMs { get; set; } = 900;

    /// <summary>界面语言，默认中文。</summary>
    public AppLanguage Language { get; set; } = AppLanguage.Chinese;

    /// <summary>在展开卡片上显示 CPU / 内存小组件。</summary>
    public bool ShowSystemWidgets { get; set; } = true;
}
