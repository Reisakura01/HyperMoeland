using System;
using System.IO;
using System.Text.Json;
using Islora.Models;

namespace Islora.Services;

/// <summary>
/// 设置读写：加载/保存到 %LOCALAPPDATA%\Islora\settings.json。
///
/// 兼容性：本项目原名 HyperMoeland，老版本把设置写在 %LOCALAPPDATA%\HyperMoeland\。
/// 新目录里没有设置文件时会回退读取旧位置，让老用户升级后不丢偏好；
/// 之后一旦保存，就会写到新位置。
/// </summary>
internal static class SettingsService
{
    public static AppSettings Current { get; private set; } = new();

    private static string SettingsDirectory
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Islora");

    private static string FilePath => Path.Combine(SettingsDirectory, "settings.json");

    /// <summary>旧版（HyperMoeland 时期）的设置文件位置。</summary>
    private static string LegacyFilePath
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HyperMoeland", "settings.json");

    public static void Load()
    {
        foreach (var path in new[] { FilePath, LegacyFilePath })
        {
            try
            {
                if (!File.Exists(path)) continue;
                var json = File.ReadAllText(path);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s is not null) Current = s;
                return;   // 找到就用，新位置优先
            }
            catch { }
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
