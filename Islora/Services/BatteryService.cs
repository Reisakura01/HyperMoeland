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

    /// <summary>根据电源供给状态判断是否插电，并在变化时派发事件。</summary>
    private void UpdateCharging()
    {
        var status = PowerManager.PowerSupplyStatus;
        bool charging = status == PowerSupplyStatus.Adequate;
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
