using System;
using System.Runtime.InteropServices;

namespace HyperMoeland.Interop;

/// <summary>
/// Win32 / DWM 互操作：云母背景、暗色模式、圆角、窗口样式、显示器查询。
/// </summary>
internal static class NativeMethods
{
    // ---- DWM 属性 ----
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    // ---- 背景类型 ----
    public const int DWMSBT_AUTO = 0;
    public const int DWMSBT_NONE = 1;
    public const int DWMSBT_MAINWINDOW = 2;      // 云母 Mica
    public const int DWMSBT_TRANSIENTWINDOW = 3; // 亚克力 Acrylic
    public const int DWMSBT_TABBEDWINDOW = 4;    // MicaAlt

    // ---- 圆角 ----
    public const int DWMWCP_DEFAULT = 0;
    public const int DWMWCP_DONOTROUND = 1;
    public const int DWMWCP_ROUND = 2;

    // ---- 扩展样式 ----
    private const int GWL_EXSTYLE = -20;
    private const int GWL_STYLE = -16;
    private const int WS_EX_TOOLWINDOW = 0x00000080;   // 不进任务栏
    private const int WS_EX_NOACTIVATE = 0x08000000;   // 永不抢焦点

    // ---- 窗口样式（用于区分"真·全屏" 与 "最大化普通窗口"） ----
    private const int WS_CAPTION = 0x00C00000;     // 有标题栏（最大化普通窗口仍保留）
    private const int WS_THICKFRAME = 0x00040000;  // 可拖拽边框（普通窗口保留）
    private const int WS_MAXIMIZE = 0x01000000;    // 处于最大化状态

    // ---- 主机背景（透明窗口承载系统背景板） ----
    private const int WCA_ACCENT_POLICY = 19;
    private const int ACCENT_ENABLE_HOSTBACKDROP = 5;   // 宿主背景（Win11，WPF 下不可靠，易黑）
    private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4; // 亚克力模糊（Win10/11 均可，WPF 分层窗口可靠）

    // ---- 显示器 ----
    public const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromPoint(POINT point, uint flags);

    [DllImport("user32.dll")]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO info);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    /// <summary>
    /// 判断前台窗口是否为"真正免费的全屏"（如视频/游戏全屏、无边框独占窗口），
    /// 排除"最大化的普通窗口"。最大化窗口仍带标题栏（WS_CAPTION）且 IsZoomed 为真，
    /// 而真全屏通常是无标题栏（无 WS_CAPTION）且窗口覆盖整个屏幕。
    /// </summary>
    public static bool IsTrueFullscreen(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        // 最大化窗口（IsZoomed）不计入全屏 —— 这是"时有时无"误判的主因
        if (IsZoomed(hwnd)) return false;
        // 带系统标题栏/可拖拽边框的普通窗口不计入
        int style = GetWindowLong(hwnd, GWL_STYLE);
        if ((style & WS_CAPTION) != 0) return false;
        return true;
    }

    [DllImport("user32.dll")]
    public static extern int GetDpiForWindow(IntPtr hwnd);

    // ---- 系统拖拽（无边框窗口标准方案） ----
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int WM_EXITSIZEMOVE = 0x0232;
    public const int HTCAPTION = 2;

    // ---- 置顶保持 ----
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    /// <summary>
    /// 为进程设置稳定的 AUMID（AppUserModelID）。无打包 Win32 应用必须显式设置，
    /// 否则 UserNotificationListener.NotificationChanged 订阅会报 0x80070490 (ERROR_NOT_FOUND)。
    /// </summary>
    public static void SetAppUserModelId(string appId)
        => SetCurrentProcessExplicitAppUserModelID(appId);

    // ---- 包身份（Sparse Package Identity） ----

    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFamilyName(ref int packageFamilyNameLength,
        [MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder? packageFamilyName);

    /// <summary>
    /// 取当前进程的包家族名（Package Family Name）。
    /// 返回 null 表示进程**没有**包身份（未注册稀疏包，或不是从已注册的外部位置启动）。
    /// 有身份时 UserNotificationListener 的事件订阅才可用（否则报 0x80070490）。
    /// </summary>
    public static string? TryGetPackageFamilyName()
    {
        try
        {
            int length = 0;
            int hr = GetCurrentPackageFamilyName(ref length, null);
            if (hr != ERROR_INSUFFICIENT_BUFFER || length <= 0) return null;

            var sb = new System.Text.StringBuilder(length);
            hr = GetCurrentPackageFamilyName(ref length, sb);
            if (hr != 0) return null;
            return sb.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>强制把窗口置于 Z 序最顶层（不抢焦点、不改变位置尺寸、不强制显示）。
    /// 注意：不带 SWP_SHOWWINDOW，否则会把 Hide() 隐藏的窗口（全屏隐藏）又显示出来。</summary>
    public static void KeepTopmost(IntPtr hwnd)
        => SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

    // ---- 操作方法 ----

    public static void SetDarkMode(IntPtr hwnd, bool dark)
    {
        int value = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    public static void SetMicaBackdrop(IntPtr hwnd)
    {
        int value = DWMSBT_MAINWINDOW; // 云母
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref value, sizeof(int));
    }

    /// <summary>设置系统背景板类型（Win11 22H2+ 原生 API）。</summary>
    public static void SetSystemBackdrop(IntPtr hwnd, int type)
    {
        int value = type;
        DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref value, sizeof(int));
    }

    public static void SetRoundedCorners(IntPtr hwnd)
    {
        int value = DWMWCP_ROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref value, sizeof(int));
    }

    /// <summary>取消 DWM 窗口圆角：让胶囊/卡片的形状完全由 Border 的 CornerRadius 决定（实心胶囊方案）。</summary>
    public static void SetSquareCorners(IntPtr hwnd)
    {
        int value = DWMWCP_DONOTROUND;
        DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref value, sizeof(int));
    }

    public static void SetToolWindowNoActivate(IntPtr hwnd)
    {
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    /// <summary>
    /// 启用亚克力效果：让 AllowsTransparency 的透明 WPF 窗口显示"磨砂玻璃"模糊。
    /// tintColor 为 ABGR 格式（0xAABBGGRR），用于给磨砂基调色（日间浅 / 夜间深）。
    /// 这是透明窗口 + 磨砂质感最可靠的方案（云母背景板在 WPF 分层窗口上不可靠）。
    /// </summary>
    public static void EnableAcrylic(IntPtr hwnd, uint tintColor)
    {
        var accent = new AccentPolicy
        {
            AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 2,
            GradientColor = tintColor,
        };
        var data = new WindowCompositionAttributeData
        {
            Attribute = WCA_ACCENT_POLICY,
            Data = Marshal.AllocHGlobal(Marshal.SizeOf<AccentPolicy>()),
            SizeOfData = Marshal.SizeOf<AccentPolicy>(),
        };
        try
        {
            Marshal.StructureToPtr(accent, data.Data, false);
            SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(data.Data);
        }
    }

    // ---- 结构体 ----

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }
}
