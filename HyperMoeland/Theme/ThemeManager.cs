using System.Windows;
using System.Windows.Media;

namespace HyperMoeland.Theme;

/// <summary>
/// 主题管理：维护前景 / 次要 / 边框 / 覆盖四支画刷，
/// 按日间 / 夜间替换 App 资源，XAML 用 DynamicResource 自动刷新。
/// </summary>
internal static class ThemeManager
{
    public const string ForegroundKey = "Theme.Foreground";
    public const string MutedKey = "Theme.Muted";
    public const string BorderKey = "Theme.Border";
    public const string OverlayKey = "Theme.Overlay";
    public const string ChargingKey = "Theme.Charging";
    public const string AccentKey = "Theme.Accent";

    public static AppTheme Current { get; private set; } = AppTheme.Day;

    public static void Apply(AppTheme theme)
    {
        Current = theme;
        var resources = Application.Current.Resources;
        bool dark = theme == AppTheme.Night;

        resources[ForegroundKey] = new SolidColorBrush(dark ? Colors.White : Color.FromRgb(0x1B, 0x1B, 0x1B));
        resources[MutedKey]     = new SolidColorBrush(dark ? Color.FromRgb(0xB3, 0xB3, 0xB3) : Color.FromRgb(0x5F, 0x5F, 0x5F));
        resources[BorderKey]    = new SolidColorBrush(dark ? Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x40, 0x00, 0x00, 0x00));
        // 充电指示色：醒目绿色（日夜间一致）
        resources[ChargingKey]  = new SolidColorBrush(Color.FromRgb(0x00, 0xC2, 0x7A));
        // 强调色：小组件等点缀元素（日间偏深蓝，夜间提亮以保证对比度）
        resources[AccentKey]    = new SolidColorBrush(dark ? Color.FromRgb(0x4C, 0xA8, 0xFF) : Color.FromRgb(0x0A, 0x6C, 0xD6));
        // 半透明胶囊：日间浅色 #E6F5F5F7，夜间深色 #E61C1C1E（≈90% 半透明，小米超级岛质感）
        resources[OverlayKey]   = new SolidColorBrush(dark ? Color.FromArgb(0xE6, 0x1C, 0x1C, 0x1E) : Color.FromArgb(0xE6, 0xF5, 0xF5, 0xF7));
    }
}
