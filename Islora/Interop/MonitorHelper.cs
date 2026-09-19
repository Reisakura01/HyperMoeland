using System;
using System.Runtime.InteropServices;
using static Islora.Interop.NativeMethods;

namespace Islora.Interop;

/// <summary>显示器工作区（物理像素坐标）。</summary>
internal readonly record struct WorkArea(int X, int Y, int Width, int Height);

/// <summary>显示器辅助：主屏工作区、指定显示器工作区。</summary>
internal static class MonitorHelper
{
    public static WorkArea GetPrimaryWorkArea()
    {
        var monitor = MonitorFromPoint(new POINT { X = 0, Y = 0 }, MONITOR_DEFAULTTOPRIMARY);
        return GetWorkArea(monitor);
    }

    /// <summary>读取指定显示器的工作区。</summary>
    public static WorkArea GetWorkArea(IntPtr monitor)
    {
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info)) return new WorkArea(0, 0, 1920, 1080);
        var w = info.rcWork;
        return new WorkArea(w.Left, w.Top, w.Width, w.Height);
    }
}
