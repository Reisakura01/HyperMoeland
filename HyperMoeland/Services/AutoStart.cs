using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace HyperMoeland.Services;

/// <summary>
/// 开机自启管理，按运行形态走两条完全不同的路径：
///
///   * 绿色版 / Inno 安装版 → 写当前用户 Run 键（原有行为，未改动）
///   * 完整包（Store 安装）→ 走清单里声明的 <c>windows.startupTask</c>；
///     打包应用对 HKCU\...\Run 的写入会被虚拟化，必须改用 StartupTask API。
///
/// StartupTask 的读写都是异步的，而托盘菜单需要在构建时同步拿到状态，
/// 因此这里维护一个缓存值：启动时 RefreshAsync() 刷新一次，切换时乐观更新。
/// </summary>
internal static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string AppName = "HyperMoeland";

    /// <summary>清单里 StartupTask 的 TaskId，必须与 AppxManifest 完全一致。</summary>
    private const string StartupTaskId = "HyperMoelandStartup";

    private static bool _packagedEnabled;

    public static bool IsEnabled()
        => PackageContext.IsFullPackage ? _packagedEnabled : IsRegistryEnabled();

    public static void Set(bool enabled)
    {
        if (PackageContext.IsFullPackage)
        {
            _packagedEnabled = enabled;              // 乐观更新，供托盘菜单立即反映
            _ = ApplyStartupTaskAsync(enabled);
            return;
        }
        SetRegistry(enabled);
    }

    public static void Enable() => Set(true);
    public static void Disable() => Set(false);

    /// <summary>启动时调用一次：把打包版的自启状态读进缓存。</summary>
    public static async Task RefreshAsync()
    {
        if (!PackageContext.IsFullPackage) return;
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            _packagedEnabled = task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }
        catch
        {
            _packagedEnabled = false;
        }
    }

    private static async Task ApplyStartupTaskAsync(bool enabled)
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (enabled)
            {
                // 用户可能已在「任务管理器 → 启动」里手动禁用；此时 RequestEnableAsync 不会再打开
                var state = await task.RequestEnableAsync();
                _packagedEnabled = state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }
            else
            {
                task.Disable();
                _packagedEnabled = false;
            }
        }
        catch
        {
            // 取不到任务（清单未声明等）时静默失败，不影响主流程
        }
    }

    // ---- 非打包：注册表 Run 键 ----

    private static bool IsRegistryEnabled()
    {
        try { using var k = Registry.CurrentUser.OpenSubKey(RunKeyPath); return k?.GetValue(AppName) != null; }
        catch { return false; }
    }

    private static void SetRegistry(bool enabled)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (k is null) return;
            if (enabled) k.SetValue(AppName, "\"" + ExePath + "\"");
            else k.DeleteValue(AppName, false);
        }
        catch { }
    }

    private static string ExePath
    {
        get { try { return Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty; } catch { return string.Empty; } }
    }
}
