using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Islora.Interop;

namespace Islora.Services;

/// <summary>
/// 运行形态判定：区分三种情况，供自启 / 更新检查等功能分支使用。
///
///   * 无包身份            → 直接运行 exe（绿色版 / Inno 安装版）
///   * 稀疏包身份          → 自行注册了稀疏包，exe 仍在包安装目录之外（外部位置）
///   * 完整包（Store 安装）→ exe 就位于 Package.Current.InstalledLocation 之内
///
/// 判定依据是「进程 exe 路径是否落在包安装目录内」：
/// 稀疏包的 InstalledLocation 只含清单与图标，exe 在 AllowExternalContent 指向的真实目录里，
/// 因此两者能可靠区分。
/// </summary>
internal static class PackageContext
{
    private static readonly Lazy<string?> FamilyNameLazy = new(() =>
    {
        try { return NativeMethods.TryGetPackageFamilyName(); }
        catch { return null; }
    });

    private static readonly Lazy<bool> FullPackageLazy = new(() =>
    {
        try
        {
            var installed = Windows.ApplicationModel.Package.Current.InstalledLocation?.Path;
            if (string.IsNullOrEmpty(installed)) return false;

            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exe)) return false;

            return exe.StartsWith(installed, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Package.Current 在无身份时抛异常；单文件自解压等特殊形态也可能取不到路径
            return false;
        }
    });

    /// <summary>包家族名；无包身份时为 null。</summary>
    public static string? FamilyName => FamilyNameLazy.Value;

    /// <summary>是否具有包身份（稀疏包或完整包）。</summary>
    public static bool HasIdentity => FamilyName is not null;

    /// <summary>是否以完整包形态运行（Microsoft Store 安装的版本）。</summary>
    public static bool IsFullPackage => HasIdentity && FullPackageLazy.Value;

    /// <summary>包安装目录（无身份时为 null）。</summary>
    public static string? InstalledLocationPath
    {
        get
        {
            try { return Windows.ApplicationModel.Package.Current?.InstalledLocation?.Path; }
            catch { return null; }
        }
    }

    /// <summary>包版本（清单里的四段式版本号）。</summary>
    public static string? PackageVersion
    {
        get
        {
            try
            {
                var v = Windows.ApplicationModel.Package.Current?.Id?.Version;
                return v is null ? null : $"{v.Value.Major}.{v.Value.Minor}.{v.Value.Build}.{v.Value.Revision}";
            }
            catch { return null; }
        }
    }

    /// <summary>查询清单里声明的 startupTask 当前状态；失败返回异常描述。</summary>
    public static async Task<string> GetStartupTaskStateAsync(string taskId)
    {
        try
        {
            var task = await Windows.ApplicationModel.StartupTask.GetAsync(taskId);
            return task.State.ToString();
        }
        catch (Exception ex)
        {
            return $"异常 {ex.GetType().Name}: {ex.Message}";
        }
    }
}
