using System;
using System.Collections.Generic;
using Islora.Models;

namespace Islora.Services;

/// <summary>
/// 界面本地化：维护当前语言 + 中英文词条字典，切换语言时触发事件。
/// 界面元素通过 T(key) 获取当前语言文本，并监听 LanguageChanged 刷新。
/// </summary>
internal static class LocalizationService
{
    public static AppLanguage Current { get; private set; } = AppLanguage.Chinese;

    /// <summary>语言切换通知。</summary>
    public static event Action? LanguageChanged;

    private static readonly Dictionary<string, (string Zh, string En)> Entries = new()
    {
        // ---- 设置窗口 ----
        ["Settings.Title"]        = ("Islora · 设置", "Islora · Settings"),
        ["Settings.Theme"]        = ("主题", "Theme"),
        ["Settings.Mode"]         = ("模式", "Mode"),
        ["Settings.ModeAuto"]     = ("自动（按时间）", "Auto (by time)"),
        ["Settings.ModeLight"]    = ("强制浅色", "Light"),
        ["Settings.ModeDark"]     = ("强制深色", "Dark"),
        ["Settings.DayStart"]     = ("日间开始", "Day starts"),
        ["Settings.NightStart"]   = ("夜间开始", "Night starts"),
        ["Settings.System"]       = ("系统", "System"),
        ["Settings.AutoStart"]    = ("开机自启", "Launch at startup"),
        ["Settings.AutoUpdate"]   = ("启动时检查更新", "Check for updates at startup"),
        ["Settings.Neon"]         = ("霓虹动画", "Neon animation"),
        ["Settings.NeonHint"]     = ("卡片霓虹背景的节拍快慢，播放音乐时生效", "Neon pulse tempo; visible while playing"),
        ["Settings.Language"]     = ("语言", "Language"),
        ["Settings.Cancel"]       = ("取消", "Cancel"),
        ["Settings.Save"]         = ("保存", "Save"),

        // ---- 托盘菜单 ----
        ["Tray.OpenSettings"]     = ("打开设置", "Settings"),
        ["Tray.AutoStart"]        = ("开机自启", "Launch at startup"),
        ["Tray.TestNotif"]        = ("测试通知", "Test notification"),
        ["Tray.Exit"]             = ("退出", "Exit"),
        ["Tray.Tooltip"]          = ("Islora", "Islora"),
        ["Tray.UpdateBalloon"]    = ("发现新版本 v{0}，点击下载更新", "New version v{0} available — click to download"),

        // ---- 卡片 ----
        ["Card.Like"]             = ("喜欢", "Like"),
        ["Card.Previous"]         = ("上一首", "Previous"),
        ["Card.PlayPause"]        = ("播放 / 暂停", "Play / Pause"),
        ["Card.Next"]             = ("下一首", "Next"),
        ["Card.Battery"]          = ("电量 {0}%", "Battery {0}%"),
        ["Card.BatteryUnknown"]   = ("电量 --", "Battery --"),

        // ---- 系统小组件 ----
        ["Widget.Cpu"]            = ("CPU", "CPU"),
        ["Widget.Memory"]         = ("内存", "Memory"),
        ["Widget.CpuTip"]         = ("CPU 占用 {0:F0}%", "CPU usage {0:F0}%"),
        ["Widget.MemoryTip"]      = ("已用 {0:F1} / {1:F1} GB", "{0:F1} / {1:F1} GB used"),
        ["Settings.Widgets"]      = ("显示 CPU / 内存小组件", "Show CPU / memory widgets"),

        // ---- 通知 ----
        ["Notif.UnknownApp"]      = ("通知", "Notification"),
        ["Notif.Test"]            = ("📩 Islora · 这是一条测试通知，通知功能正常", "📩 Islora · This is a test notification — notifications work!"),
        ["Notif.PermissionHint"]  = ("请在系统设置中开启通知访问权限：设置 → 系统 → 通知 → 通知访问权限 → 打开 Islora", "Enable notification access: Settings → System → Notifications → Notification access → enable Islora"),
    };

    public static string T(string key)
    {
        if (Entries.TryGetValue(key, out var pair))
            return Current == AppLanguage.Chinese ? pair.Zh : pair.En;
        return key; // 未命中时返回 key，便于发现漏翻译
    }

    /// <summary>格式化取词（如 T("Tray.UpdateBalloon", version)）。</summary>
    public static string T(string key, params object[] args)
        => string.Format(T(key), args);

    public static void SetLanguage(AppLanguage lang)
    {
        if (lang == Current) return;
        Current = lang;
        LanguageChanged?.Invoke();
    }
}
