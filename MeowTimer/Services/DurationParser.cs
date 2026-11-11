using System.Globalization;
using System.Text.RegularExpressions;

namespace MeowTimer.Services;

/// <summary>解析用户输入的时长：25 / 25m / 90s / 1h30m / 1:30 / 25分钟。</summary>
public static class DurationParser
{
    public static double? Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var s = input.Trim().ToLowerInvariant()
            .Replace("分钟", "m").Replace("分", "m")
            .Replace("小时", "h").Replace("时", "h")
            .Replace("秒", "s")
            .Replace(" ", "")
            .Replace("：", ":");

        // 纯数字 = 分钟
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var plain))
            return plain > 0 ? plain * 60 : null;

        // mm:ss 或 hh:mm:ss
        if (s.Contains(':') && !s.Any(char.IsLetter))
        {
            var parts = s.Split(':');
            if (parts.All(p => int.TryParse(p, out _)))
            {
                int[] nums = parts.Select(int.Parse).ToArray();
                return nums.Length switch
                {
                    2 => nums[0] * 60 + nums[1],
                    3 => nums[0] * 3600 + nums[1] * 60 + nums[2],
                    _ => null
                };
            }
            return null;
        }

        // 1h30m / 90s 形式
        var matches = Regex.Matches(s, @"(\d+(?:\.\d+)?)([hms])");
        if (matches.Count == 0) return null;

        double total = 0;
        foreach (Match m in matches)
        {
            double v = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            total += m.Groups[2].Value switch
            {
                "h" => v * 3600,
                "m" => v * 60,
                _ => v
            };
        }

        return total > 0 ? total : null;
    }

    public static string Humanize(double seconds)
    {
        int total = (int)Math.Round(seconds);
        int h = total / 3600, m = total % 3600 / 60, s = total % 60;
        if (h > 0 && m > 0) return $"{h}h{m}m";
        if (h > 0) return $"{h}h";
        if (m > 0 && s > 0) return $"{m}m{s}s";
        if (m > 0) return $"{m}m";
        return $"{s}s";
    }
}
