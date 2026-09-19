// 别名固定指向 WPF 的 Application，避免与 System.Windows.Forms.Application 歧义（CS0104）
using System;
using System.Threading;
using System.Threading.Tasks;
using Application = System.Windows.Application;
using Islora.Interop;
using Islora.Services;

namespace Islora;

public partial class App : Application
{
    /// <summary>
    /// 单实例互斥体。岛是「屏幕顶部常驻 + 托盘图标 + 全局鼠标钩子 + 音频环回采集」的形态，
    /// 跑两份会出现两个岛、两个托盘图标、两套采集与钩子，必须互斥。
    /// 用 Local\ 前缀 = 按登录会话隔离（多用户各自可以开一个）。
    /// 注意：绿色版 / Inno 版 / Store 版共用同一个名字——这正是想要的，
    /// 避免「装了两种形态导致同时出现两个岛」。
    /// </summary>
    private const string SingleInstanceMutexName = @"Local\Islora.SingleInstance";

    private Mutex? _singleInstance;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // 已有实例在跑 → 本实例直接退出（不做 IPC 唤醒，静默让位即可）
        _singleInstance = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
        if (!createdNew)
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        // 设置稳定的 AUMID（必须在通知/Toast 相关 WinRT 调用前），
        // 否则 UserNotificationListener 订阅会报 0x80070490 而收不到通知。
        NativeMethods.SetAppUserModelId("MoeOrigin.Islora");

        // 加载设置并应用语言
        SettingsService.Load();
        LocalizationService.SetLanguage(SettingsService.Current.Language);

        // 开机自启：打包版（Store 安装）走 StartupTask（异步），
        // 绿色版 / Inno 安装版走注册表 Run 键。
        // 先读回当前状态、再按设置应用，避免两边的异步写入互相覆盖。
        _ = ApplyAutoStartAsync();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        try
        {
            _singleInstance?.ReleaseMutex();
            _singleInstance?.Dispose();
        }
        catch { }
        _singleInstance = null;
        base.OnExit(e);
    }

    private static async Task ApplyAutoStartAsync()
    {
        await AutoStart.RefreshAsync();
        AutoStart.Set(SettingsService.Current.AutoStart);
    }
}
