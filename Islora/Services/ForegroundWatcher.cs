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
        if (fg == IntPtr.Zero)
        {
            // 取不到前台窗口（切换瞬间/前台应用刚退出）：按"不全屏"处理，
            // 否则一旦卡在全屏状态，岛就会永久隐藏、再也回不来。
            SetFullscreen(false);
            return;
        }

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
        // 另外排除了桌面/任务栏等系统外壳窗口：它们同样铺满整屏且无标题栏，
        // 但不该被当成全屏应用（否则点一下桌面空白处岛就消失）。
        SetFullscreen(coversScreen && NativeMethods.IsTrueFullscreen(fg));
    }

    private void SetFullscreen(bool fullscreen)
    {
        if (fullscreen == _wasFullscreen) return;
        _wasFullscreen = fullscreen;
        FullscreenChanged?.Invoke(fullscreen);
    }
}
