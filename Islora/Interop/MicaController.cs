using System.Windows;
using System.Windows.Interop;

namespace Islora.Interop;

/// <summary>
/// 岛窗口控制器（实心深/浅胶囊，不依赖磨砂玻璃）。
/// 应用：暗色模式 + 不进任务栏/不抢焦点 + 方形窗口角（胶囊形状由 Border 圆角决定）。
/// 说明：之前用 DWM 背景板（云母/亚克力）在 WPF 分层窗口上渲染不可靠导致黑屏，
/// 现改为实心主题色胶囊（Theme.Overlay 深浅），彻底规避该问题。
/// </summary>
internal static class MicaController
{
    public static void Apply(Window window, bool darkMode)
    {
        var hwnd = new WindowInteropHelper(window).Handle;

        NativeMethods.SetDarkMode(hwnd, darkMode);      // 日间浅色 / 夜间深色
        NativeMethods.SetToolWindowNoActivate(hwnd);    // 不进任务栏 + 不抢焦点
        NativeMethods.SetSquareCorners(hwnd);           // 窗口矩形不圆角，胶囊形状由 Border 决定
    }

    /// <summary>运行时切换日间 / 夜间（只改暗色模式；胶囊实心色由 ThemeManager 刷子自动切换）。</summary>
    public static void SetTheme(Window window, bool darkMode)
        => NativeMethods.SetDarkMode(new WindowInteropHelper(window).Handle, darkMode);
}
