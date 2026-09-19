using System;
using Islora.Interop;

namespace Islora.Services;

/// <summary>
/// 系统资源监视（供岛内小组件使用）：CPU 占用率 + 物理内存占用。
///
/// CPU 占用率通过两次 GetSystemTimes 采样的差值计算：
///   total = (kernel - idle) + user   ← kernel 时间含 idle，必须扣除
///   busy  = total - idleDelta
/// 首次采样只作为基准，返回 -1（界面显示 "--"）。
/// </summary>
internal sealed class SystemMonitorService
{
    private ulong _prevIdle, _prevKernel, _prevUser;
    private bool _primed;

    /// <summary>CPU 占用率（0~100）；首次采样前为 -1。</summary>
    public double CpuPercent { get; private set; } = -1;

    /// <summary>内存占用率（0~100）；读取失败为 -1。</summary>
    public double MemoryPercent { get; private set; } = -1;

    /// <summary>已用内存（GB）。</summary>
    public double MemoryUsedGb { get; private set; }

    /// <summary>物理内存总量（GB）。</summary>
    public double MemoryTotalGb { get; private set; }

    /// <summary>数据更新通知（(cpu, mem)，取值 -1 表示尚未就绪）。</summary>
    public event Action<double, double>? Changed;

    /// <summary>采样一次（由外部定时器每秒调用）。</summary>
    public void Poll()
    {
        bool cpuOk = PollCpu();
        bool memOk = PollMemory();
        if (cpuOk || memOk)
            Changed?.Invoke(CpuPercent, MemoryPercent);
    }

    private bool PollCpu()
    {
        if (!SystemInfo.TryGetCpuTimes(out var idle, out var kernel, out var user))
            return false;

        if (!_primed)
        {
            _primed = true;
            _prevIdle = idle;
            _prevKernel = kernel;
            _prevUser = user;
            return false;   // 首次只建立基准
        }

        // 计数回绕（极少见）时跳过本次
        if (idle < _prevIdle || kernel < _prevKernel || user < _prevUser)
        {
            _prevIdle = idle;
            _prevKernel = kernel;
            _prevUser = user;
            return false;
        }

        ulong idleDelta = idle - _prevIdle;
        ulong kernelDelta = kernel - _prevKernel;
        ulong userDelta = user - _prevUser;
        _prevIdle = idle;
        _prevKernel = kernel;
        _prevUser = user;

        // kernel 时间包含 idle，因此总时间 = kernel + user
        ulong total = kernelDelta + userDelta;
        if (total == 0) return false;

        ulong busy = total > idleDelta ? total - idleDelta : 0;
        double percent = busy * 100.0 / total;

        // 平滑处理：避免单帧尖峰让圆环跳动
        CpuPercent = CpuPercent < 0 ? percent : CpuPercent * 0.4 + percent * 0.6;
        return true;
    }

    private bool PollMemory()
    {
        if (!SystemInfo.TryGetMemory(out var total, out var avail, out var load))
            return false;

        const double gb = 1024.0 * 1024.0 * 1024.0;
        MemoryTotalGb = total / gb;
        MemoryUsedGb = (total - avail) / gb;

        // 优先使用系统给出的 load，缺失时自行换算
        double percent = load > 0 ? load : (total > 0 ? (total - avail) * 100.0 / total : 0);
        MemoryPercent = Math.Clamp(percent, 0, 100);
        return true;
    }
}
