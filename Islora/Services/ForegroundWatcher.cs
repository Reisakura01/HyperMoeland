using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Islora.Interop;

namespace Islora.Services;

/// <summary>
/// 前台窗口监视：每 500ms 检查前台窗口——
/// 1) 前台窗口全屏（视频/游戏）时通知隐藏岛；
/// 2) 前台窗口所在屏变化时通知跟随。
/// </summary>
internal sealed class ForegroundWatcher
{
    public event Action<bool>? FullscreenChanged;
    public event Action<WorkArea>? MonitorChanged;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private bool _wasFullscreen;
    private IntPtr _lastMonitor = IntPtr.Zero;

    public void Start()
    {
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void Tick()
    {
        var fg = NativeMethods.GetForegroundWindow();
        if (fg == IntPtr.Zero) return;

        var monitor = NativeMethods.MonitorFromWindow(fg, NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (monitor != _lastMonitor)
        {
            _lastMonitor = monitor;
            MonitorChanged?.Invoke(MonitorHelper.GetWorkArea(monitor));
        }

        NativeMethods.GetWindowRect(fg, out var rect);
        var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        NativeMethods.GetMonitorInfo(monitor, ref info);

        bool coversScreen =
            rect.Left <= info.rcMonitor.Left && rect.Top <= info.rcMonitor.Top &&
            rect.Right >= info.rcMonitor.Right && rect.Bottom >= info.rcMonitor.Bottom;

        // 真正全屏：覆盖屏幕 且 无标题栏（排除最大化普通窗口）——
        // 避免把最大化/贴边窗口误判为全屏导致岛"时有时无"。
        bool fullscreen = coversScreen && NativeMethods.IsTrueFullscreen(fg);

        if (fullscreen != _wasFullscreen)
        {
            _wasFullscreen = fullscreen;
            FullscreenChanged?.Invoke(fullscreen);
        }
    }
}
