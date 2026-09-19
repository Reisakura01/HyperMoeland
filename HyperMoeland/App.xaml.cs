// 别名固定指向 WPF 的 Application，避免与 System.Windows.Forms.Application 歧义（CS0104）
using System.Threading.Tasks;
using Application = System.Windows.Application;
using HyperMoeland.Interop;
using HyperMoeland.Services;

namespace HyperMoeland;

public partial class App : Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        // 设置稳定的 AUMID（必须在通知/Toast 相关 WinRT 调用前），
        // 否则 UserNotificationListener 订阅会报 0x80070490 而收不到通知。
        NativeMethods.SetAppUserModelId("MoeOrigin.HyperMoeland");

        // 加载设置并应用语言
        SettingsService.Load();
        LocalizationService.SetLanguage(SettingsService.Current.Language);

        // 开机自启：打包版（Store 安装）走 StartupTask（异步），
        // 绿色版 / Inno 安装版走注册表 Run 键。
        // 先读回当前状态、再按设置应用，避免两边的异步写入互相覆盖。
        _ = ApplyAutoStartAsync();
    }

    private static async Task ApplyAutoStartAsync()
    {
        await AutoStart.RefreshAsync();
        AutoStart.Set(SettingsService.Current.AutoStart);
    }
}
