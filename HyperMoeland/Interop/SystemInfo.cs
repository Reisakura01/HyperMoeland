using System;
using System.Runtime.InteropServices;

namespace HyperMoeland.Interop;

/// <summary>
/// 系统资源读取：CPU 占用率（GetSystemTimes）与物理内存（GlobalMemoryStatusEx）。
/// 两者都是纯 Win32 调用，无需管理员权限，也不会引入额外依赖。
/// </summary>
internal static class SystemInfo
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        /// <summary>合并为 100ns 为单位的 64 位计数。</summary>
        public readonly ulong ToUInt64() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;       // 已用内存百分比（0~100）
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    /// <summary>
    /// 读取 CPU 累计时间（idle / kernel / user，单位 100ns）。
    /// kernel 时间已包含 idle 时间，计算占用率时需要减去。
    /// </summary>
    public static bool TryGetCpuTimes(out ulong idle, out ulong kernel, out ulong user)
    {
        idle = kernel = user = 0;
        try
        {
            if (!GetSystemTimes(out var i, out var k, out var u)) return false;
            idle = i.ToUInt64();
            kernel = k.ToUInt64();
            user = u.ToUInt64();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>读取物理内存：总字节数 / 可用字节数 / 占用百分比。</summary>
    public static bool TryGetMemory(out ulong totalBytes, out ulong availBytes, out int loadPercent)
    {
        totalBytes = availBytes = 0;
        loadPercent = 0;
        try
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (!GlobalMemoryStatusEx(ref status)) return false;
            totalBytes = status.ullTotalPhys;
            availBytes = status.ullAvailPhys;
            loadPercent = (int)status.dwMemoryLoad;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
