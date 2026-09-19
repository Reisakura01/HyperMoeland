using System;
using System.Windows.Threading;

namespace Islora.Theme;

/// <summary>
/// 主题调度：每分钟检查一次，按 模式(自动/浅色/深色) + 时间段 决定日间/夜间，
/// 切换时触发事件。小时与模式可在运行时配置。
/// </summary>
internal sealed class ThemeScheduler
{
    /// <summary>日间开始小时（含）。</summary>
    public int DayStartHour { get; set; } = 6;

    /// <summary>夜间开始小时（含）。</summary>
    public int NightStartHour { get; set; } = 19;

    /// <summary>主题模式：自动/强制浅色/强制深色。</summary>
    public ThemePreference Mode { get; set; } = ThemePreference.Auto;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMinutes(1) };

    /// <summary>主题变化通知（仅在真正切换时触发）。</summary>
    public event Action<AppTheme>? ThemeChanged;

    public void Start()
    {
        _timer.Tick += (_, _) => Check();
        _timer.Start();
        Check();
    }

    public void Stop() => _timer.Stop();

    private void Check()
    {
        var h = DateTime.Now.Hour;
        var theme = Mode switch
        {
            ThemePreference.Light => AppTheme.Day,
            ThemePreference.Dark => AppTheme.Night,
            // 自动：支持跨午夜时段（如 日间 20:00 - 夜间 07:00）
            _ => DayStartHour == NightStartHour ? AppTheme.Night
               : DayStartHour < NightStartHour
                   ? (h >= DayStartHour && h < NightStartHour ? AppTheme.Day : AppTheme.Night)
                   : (h >= DayStartHour || h < NightStartHour ? AppTheme.Day : AppTheme.Night),
        };

        if (theme != ThemeManager.Current)
        {
            ThemeManager.Apply(theme);
            ThemeChanged?.Invoke(theme);
        }
    }

    /// <summary>手动触发一次判定（设置改变后立即应用）。</summary>
    public void ApplyNow() => Check();
}
