using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HyperMoeland.Services.Lyrics;

/// <summary>歌词抓取结果（LRC 原文 + 翻译 LRC）。</summary>
internal sealed record LyricsFetchResult(string? Lrc, string? Translation, string Source);

/// <summary>歌词源接口。</summary>
internal interface ILyricsProvider
{
    /// <summary>源名称（用于诊断）。</summary>
    string Name { get; }

    /// <summary>按曲名/歌手/时长抓取歌词；失败或未找到返回 null。</summary>
    Task<LyricsFetchResult?> FetchAsync(string title, string artist, double durationSeconds, CancellationToken ct);
}

/// <summary>
/// 歌词 HTTP 工具：优先**直连**（绕过系统代理），失败再退回系统代理。
///
/// 背景：部分环境配置了本地代理（如 127.0.0.1:7897）且无法访问音乐站点，
/// 而直连正常；因此两条通道都保留，互为兜底。
/// </summary>
internal static class LyricsHttp
{
    private static readonly HttpClient Direct = Create(useProxy: false);
    private static readonly HttpClient ViaSystemProxy = Create(useProxy: true);

    private static HttpClient Create(bool useProxy)
    {
        var handler = new HttpClientHandler { UseProxy = useProxy, AllowAutoRedirect = true };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) HyperMoeland/1.3");
        return client;
    }

    /// <summary>GET 文本：先直连，失败再用系统代理；都失败抛异常。</summary>
    public static async Task<string> GetStringAsync(string url, string? referer, CancellationToken ct)
    {
        Exception? first = null;
        foreach (var client in new[] { Direct, ViaSystemProxy })
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(referer))
                    req.Headers.TryAddWithoutValidation("Referer", referer);
                using var resp = await client.SendAsync(req, ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) { first ??= new HttpRequestException($"HTTP {(int)resp.StatusCode}"); continue; }
                return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                first ??= ex;
            }
        }
        throw first ?? new HttpRequestException("请求失败");
    }
}

/// <summary>网易云音乐歌词源（搜索 + 官方歌词接口，含翻译）。</summary>
internal sealed class NetEaseLyricsProvider : ILyricsProvider
{
    private const string Referer = "https://music.163.com";

    public string Name => "NetEase";

    public async Task<LyricsFetchResult?> FetchAsync(string title, string artist, double durationSeconds, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var query = Uri.EscapeDataString(string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}");
        var searchUrl = $"https://music.163.com/api/search/get/web?s={query}&type=1&offset=0&total=true&limit=5";
        var searchJson = await LyricsHttp.GetStringAsync(searchUrl, Referer, ct).ConfigureAwait(false);

        long? songId = null;
        using (var doc = JsonDocument.Parse(searchJson))
        {
            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.ValueKind == JsonValueKind.Object &&
                result.TryGetProperty("songs", out var songs) &&
                songs.ValueKind == JsonValueKind.Array)
            {
                foreach (var song in songs.EnumerateArray())
                {
                    if (!song.TryGetProperty("id", out var idEl)) continue;

                    // 尽量挑时长最接近的版本（同名不同版）
                    if (durationSeconds > 1 && song.TryGetProperty("duration", out var dEl) && dEl.TryGetInt64(out var ms))
                    {
                        var delta = Math.Abs(ms / 1000.0 - durationSeconds);
                        if (delta > 6) continue;
                    }

                    songId = idEl.GetInt64();
                    break;
                }

                if (songId is null && songs.GetArrayLength() > 0 && songs[0].TryGetProperty("id", out var firstId))
                    songId = firstId.GetInt64();
            }
        }

        if (songId is null) return null;

        var lyricUrl = $"https://music.163.com/api/song/lyric?id={songId}&lv=1&kv=1&tv=-1";
        var lyricJson = await LyricsHttp.GetStringAsync(lyricUrl, Referer, ct).ConfigureAwait(false);

        using var lyricDoc = JsonDocument.Parse(lyricJson);
        string? lrc = null, tlyric = null;

        if (lyricDoc.RootElement.TryGetProperty("lrc", out var lrcEl) &&
            lrcEl.ValueKind == JsonValueKind.Object &&
            lrcEl.TryGetProperty("lyric", out var lrcText))
            lrc = lrcText.GetString();

        if (lyricDoc.RootElement.TryGetProperty("tlyric", out var tEl) &&
            tEl.ValueKind == JsonValueKind.Object &&
            tEl.TryGetProperty("lyric", out var tText))
            tlyric = tText.GetString();

        if (string.IsNullOrWhiteSpace(lrc)) return null;
        return new LyricsFetchResult(lrc, tlyric, Name);
    }
}

/// <summary>LRCLIB 歌词源（开放 API，无需鉴权，覆盖欧美曲目较好）。</summary>
internal sealed class LrclibLyricsProvider : ILyricsProvider
{
    public string Name => "LRCLIB";

    public async Task<LyricsFetchResult?> FetchAsync(string title, string artist, double durationSeconds, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        // 1) 精确接口
        var getUrl = "https://lrclib.net/api/get" +
                     $"?artist_name={Uri.EscapeDataString(artist ?? string.Empty)}" +
                     $"&track_name={Uri.EscapeDataString(title)}" +
                     $"&album_name=" +
                     (durationSeconds > 1 ? $"&duration={Math.Round(durationSeconds)}" : string.Empty);
        try
        {
            var json = await LyricsHttp.GetStringAsync(getUrl, null, ct).ConfigureAwait(false);
            var synced = ExtractSynced(json);
            if (!string.IsNullOrWhiteSpace(synced)) return new LyricsFetchResult(synced, null, Name);
        }
        catch { /* 未命中或网络失败 → 走搜索 */ }

        // 2) 搜索接口，取第一条带同步歌词的
        var q = Uri.EscapeDataString(string.IsNullOrWhiteSpace(artist) ? title : $"{title} {artist}");
        var searchJson = await LyricsHttp.GetStringAsync($"https://lrclib.net/api/search?q={q}", null, ct).ConfigureAwait(false);

        using var doc = JsonDocument.Parse(searchJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var synced = ExtractSynced(item.GetRawText());
            if (!string.IsNullOrWhiteSpace(synced)) return new LyricsFetchResult(synced, null, Name);
        }

        return null;
    }

    private static string? ExtractSynced(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (doc.RootElement.TryGetProperty("syncedLyrics", out var el) &&
                el.ValueKind == JsonValueKind.String)
                return el.GetString();
            return null;
        }
        catch { return null; }
    }
}
