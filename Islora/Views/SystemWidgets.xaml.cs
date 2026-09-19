using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Islora.Services;

namespace Islora.Views;

/// <summary>
/// 系统小组件：以环形进度显示 CPU 占用率与内存占用率。
///
/// 环形进度用 StrokeDashArray 实现：椭圆周长换算成"线宽倍数"后按百分比切成两段，
/// 再把椭圆旋转 -90° 让起点落在正上方（12 点方向）。
/// </summary>
public partial class SystemWidgets : UserControl
{
    private const double RingDiameter = 32.0;
    private const double RingThickness = 3.0;

    /// <summary>虚线长度的总单位数：周长 ÷ 线宽（StrokeDashArray 以线宽为单位）。</summary>
    private static readonly double DashTotal =
        2 * Math.PI * (RingDiameter / 2 - RingThickness / 2) / RingThickness;

    public SystemWidgets()
    {
        InitializeComponent();
        ApplyLanguage();
        LocalizationService.LanguageChanged += ApplyLanguage;
        Unloaded += (_, _) => LocalizationService.LanguageChanged -= ApplyLanguage;
    }

    /// <summary>
    /// 刷新小组件。cpu / mem 为 0~100 的百分比，小于 0 表示尚未就绪（显示 --）。
    /// </summary>
    public void Set(double cpu, double mem, double usedGb, double totalGb)
    {
        CpuValue.Text = cpu < 0 ? "--" : $"{cpu:F0}%";
        SetRing(CpuRing, cpu);
        CpuWidget.ToolTip = cpu < 0
            ? null
            : LocalizationService.T("Widget.CpuTip", cpu);

        MemoryValue.Text = mem < 0 ? "--" : $"{mem:F0}%";
        SetRing(MemoryRing, mem);
        MemoryWidget.ToolTip = mem < 0
            ? null
            : LocalizationService.T("Widget.MemoryTip", usedGb, totalGb);
    }

    /// <summary>按百分比更新圆环进度（0~100，负数视为 0）。</summary>
    private static void SetRing(System.Windows.Shapes.Shape ring, double percent)
    {
        double ratio = double.IsNaN(percent) || percent <= 0 ? 0 : Math.Min(percent, 100) / 100.0;
        ring.StrokeDashArray = new DoubleCollection { DashTotal * ratio, DashTotal * (1 - ratio) };
    }

    private void ApplyLanguage()
    {
        Dispatcher.InvokeAsync(() =>
        {
            CpuLabel.Text = LocalizationService.T("Widget.Cpu");
            MemoryLabel.Text = LocalizationService.T("Widget.Memory");
        });
    }
}
