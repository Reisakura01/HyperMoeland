using System;
using Windows.Devices.Power;
using Windows.System.Power;

namespace Islora.Services;

/// <summary>电量实时活动：监听系统电量变化，上报百分比与插拔电状态。</summary>
internal sealed class BatteryService : IDisposable
{
    /// <summary>电量百分比变化（0~100）。</summary>
    public event Action<double>? ChargePercentChanged;

    /// <summary>插拔电状态变化（true = 插电/充电中，false = 使用电池）。</summary>
    public event Action<bool>? PowerStateChanged;

    private bool _isCharging;
    private bool _disposed;

    public void Start()
    {
        Battery.AggregateBattery.ReportUpdated += OnReportUpdated;
        PowerManager.PowerSupplyStatusChanged += OnPowerSupplyChanged;
        Raise();
        UpdateCharging();
    }

    private void OnReportUpdated(Battery sender, object args)
    {
        Raise();
        UpdateCharging();
    }

    private void OnPowerSupplyChanged(object? sender, object e) => UpdateCharging();

    private void Raise()
    {
        var report = Battery.AggregateBattery.GetReport();
        if (report.FullChargeCapacityInMilliwattHours is int full && full > 0 &&
            report.RemainingCapacityInMilliwattHours is int remaining)
        {
            ChargePercentChanged?.Invoke(Math.Round(remaining / (double)full * 100.0, 1));
        }
    }

    /// <summary>
    /// 根据电源供给状态判断是否插电，并在变化时派发事件。
    ///
    /// 注意先确认设备**真的有电池**：PowerSupplyStatus.Adequate 的含义是
    /// 「接了交流电、供电充足」，台式机/一体机没有电池时它同样是 Adequate，
    /// 于是会常驻显示充电闪电（而电量永远是「--」）。
    /// </summary>
    private void UpdateCharging()
    {
        bool hasBattery;
        try
        {
            var report = Battery.AggregateBattery.GetReport();
            // 无电池设备会返回 NotPresent / 容量为 0
            hasBattery = report.Status != Windows.System.Power.BatteryStatus.NotPresent
                         && (report.FullChargeCapacityInMilliwattHours ?? 0) > 0;
        }
        catch { hasBattery = false; }

        bool charging = hasBattery && PowerManager.PowerSupplyStatus == PowerSupplyStatus.Adequate;
        if (charging != _isCharging)
        {
            _isCharging = charging;
            PowerStateChanged?.Invoke(charging);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Battery.AggregateBattery.ReportUpdated -= OnReportUpdated;
        PowerManager.PowerSupplyStatusChanged -= OnPowerSupplyChanged;
    }
}
