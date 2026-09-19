using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Islora.Services;

/// <summary>检查 GitHub 最新 Release，提示用户下载更新。</summary>
internal static class UpdateChecker
{
    private const string ApiUrl = "https://api.github.com/repos/Reisakura01/Islora/releases/latest";

    /// <summary>当前应用版本（来自程序集版本）。</summary>
    public static Version CurrentVersion
        => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    /// <summary>查询 GitHub 最新版本；若比当前新则返回 (版本, 下载页 URL)，否则返回 null。</summary>
    public static async Task<(Version Version, string Url)?> CheckAsync()
    {
        // Store 安装的版本由 Microsoft Store 负责更新：
        // 商店政策不允许引导用户从商店外获取更新，直接跳过检查。
        if (PackageContext.IsFullPackage) return null;

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Islora");
            var json = await http.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.TryGetProperty("tag_name", out var tp) ? tp.GetString() ?? "" : "";
            var url = root.TryGetProperty("html_url", out var up) ? up.GetString() ?? "" : "";
            var verStr = tag.TrimStart('v', 'V');

            if (System.Version.TryParse(verStr, out var latest) && latest > CurrentVersion && !string.IsNullOrEmpty(url))
                return (latest, url);
            return null;
        }
        catch
        {
            return null;
        }
    }
}
