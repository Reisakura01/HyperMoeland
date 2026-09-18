using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HyperMoeland.Models;

namespace HyperMoeland.Services.Lyrics;

/// <summary>
/// LRC 歌词解析器。
/// 支持：
///   • 标准时间标签 [mm:ss.xx] / [mm:ss.xxx] / [mm:ss]
///   • 一行多个时间标签（复读行） [00:10.00][01:20.00]文本
///   • 全局偏移 [offset:+/-ms]
///   • 元信息标签（[ti:] [ar:] [al:] 等，忽略）
///   • 翻译歌词（tlyric）：按时间戳合并到对应行
/// </summary>
internal static class LrcParser
{
    private static readonly Regex TimeTag = new(@"\[(\d{1,3}):(\d{1,2})(?:[.:](\d{1,3}))?\]", RegexOptions.Compiled);
    private static readonly Regex OffsetTag = new(@"\[offset:\s*([+-]?\d+)\s*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MetaTag = new(@"^\[[a-zA-Z]{2,}:[^\]]*\]$", RegexOptions.Compiled);

    /// <summary>
    /// 制作人员/版权行（网易云等常把它们全部标在 [00:00.00]，会污染开头显示）。
    /// 形如「作词 : 某某」「作曲：某某」「和声Backing Vocalist：某某」。
    /// </summary>
    private static readonly Regex CreditLine = new(
        @"^\s*(作词|作曲|编曲|制作人|监制|录音|混音|母带|和声|吉他|贝斯|鼓|键盘|弦乐|出品|发行|统筹|企划|策划|封面|词|曲|OP|SP|Music|Lyrics|Composer|Arranger|Producer|Mixing|Mastering|Backing|Vocal|Guitar|Bass|Drum|Piano|Strings)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>判断是否为制作人员/版权信息行（应跳过）。</summary>
    private static bool IsCreditLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        // 必须带冒号才算"署名行"，避免误伤歌词正文
        if (!text.Contains('：') && !text.Contains(':')) return false;
        return CreditLine.IsMatch(text);
    }

    /// <summary>解析主歌词与（可选）翻译歌词，返回按时间升序的行列表。</summary>
    public static List<LyricsLine> Parse(string? lrc, string? translation = null)
    {
        var lines = ParseSingle(lrc);
        if (lines.Count == 0) return lines;

        var offsetMs = ExtractOffset(lrc);
        if (offsetMs != 0)
        {
            for (int i = 0; i < lines.Count; i++)
                lines[i] = new LyricsLine
                {
                    Time = lines[i].Time + TimeSpan.FromMilliseconds(offsetMs),
                    Text = lines[i].Text,
                    Translation = lines[i].Translation,
                };
        }

        var translations = ParseSingle(translation);
        if (translations.Count > 0)
            MergeTranslations(lines, translations);

        // 过滤制作人员块：
        //  网易云等把「作词/作曲/和声/戏腔…」整块都标在 [00:00.00]，
        //  特征就是**多条歌词共享 0.0 秒**（正常歌词极少这样），整段丢弃。
        if (lines.Count(l => l.Time == TimeSpan.Zero) >= 2)
            lines = lines.Where(l => l.Time != TimeSpan.Zero).ToList();

        // 再按关键词清理残余的署名行
        var filtered = lines.Where(l => !IsCreditLine(l.Text)).ToList();
        if (filtered.Count > 0) lines = filtered;

        lines.Sort((a, b) => a.Time.CompareTo(b.Time));
        return lines;
    }

    private static List<LyricsLine> ParseSingle(string? text)
    {
        var result = new List<LyricsLine>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (MetaTag.IsMatch(line)) continue;
            if (line.StartsWith("[offset:", StringComparison.OrdinalIgnoreCase)) continue;

            var matches = TimeTag.Matches(line);
            if (matches.Count == 0) continue;

            // 文本 = 最后一个时间标签之后的内容
            var last = matches[^1];
            var content = line[(last.Index + last.Length)..].Trim();

            foreach (Match m in matches)
            {
                if (!TryParseTime(m, out var time)) continue;
                result.Add(new LyricsLine { Time = time, Text = content });
            }
        }

        return result;
    }

    private static bool TryParseTime(Match m, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        if (!int.TryParse(m.Groups[1].Value, out int minutes)) return false;
        if (!int.TryParse(m.Groups[2].Value, out int seconds)) return false;

        int ms = 0;
        if (m.Groups[3].Success)
        {
            var frac = m.Groups[3].Value;
            // 两位=厘秒，三位=毫秒
            ms = frac.Length switch
            {
                1 => int.Parse(frac, CultureInfo.InvariantCulture) * 100,
                2 => int.Parse(frac, CultureInfo.InvariantCulture) * 10,
                _ => int.Parse(frac, CultureInfo.InvariantCulture),
            };
        }

        time = new TimeSpan(0, 0, minutes, seconds, ms);
        return true;
    }

    private static int ExtractOffset(string? lrc)
    {
        if (string.IsNullOrEmpty(lrc)) return 0;
        var m = OffsetTag.Match(lrc);
        return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
    }

    /// <summary>按时间戳把翻译合并进主歌词（容差 300ms）。</summary>
    private static void MergeTranslations(List<LyricsLine> lines, List<LyricsLine> translations)
    {
        foreach (var t in translations)
        {
            if (string.IsNullOrWhiteSpace(t.Text)) continue;

            int best = -1;
            double bestDelta = double.MaxValue;
            for (int i = 0; i < lines.Count; i++)
            {
                var delta = Math.Abs((lines[i].Time - t.Time).TotalMilliseconds);
                if (delta < bestDelta) { bestDelta = delta; best = i; }
            }

            if (best >= 0 && bestDelta <= 300)
            {
                lines[best] = new LyricsLine
                {
                    Time = lines[best].Time,
                    Text = lines[best].Text,
                    Translation = t.Text,
                };
            }
        }
    }
}
