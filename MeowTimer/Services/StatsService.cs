using System.IO;
using System.Text.Json;

namespace MeowTimer.Services;

public sealed class DayStat
{
    public int Pomodoros { get; set; }
    public int FocusMinutes { get; set; }
}

public sealed class StatsData
{
    public Dictionary<string, DayStat> Days { get; set; } = new();
}

public static class StatsService
{
    private static StatsData _data = new();

    private static string FilePath => Path.Combine(ConfigService.DataDir, "stats.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                _data = JsonSerializer.Deserialize<StatsData>(json, Options) ?? new StatsData();
            }
        }
        catch
        {
            _data = new StatsData();
        }
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigService.DataDir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(_data, Options));
        }
        catch { }
    }

    private static string Key(DateTime d) => d.ToString("yyyy-MM-dd");

    public static void Add(int minutes, bool pomodoro)
    {
        if (minutes <= 0) return;
        var k = Key(DateTime.Now);
        if (!_data.Days.TryGetValue(k, out var day))
        {
            day = new DayStat();
            _data.Days[k] = day;
        }
        day.FocusMinutes += minutes;
        if (pomodoro) day.Pomodoros++;
        Save();
    }

    public static DayStat GetDay(DateTime d)
        => _data.Days.TryGetValue(Key(d), out var v) ? v : new DayStat();

    public static (int pomodoros, int minutes) Sum(DateTime from, DateTime to)
    {
        int p = 0, m = 0;
        foreach (var kv in _data.Days)
        {
            if (!DateTime.TryParse(kv.Key, out var d)) continue;
            if (d.Date < from.Date || d.Date > to.Date) continue;
            p += kv.Value.Pomodoros;
            m += kv.Value.FocusMinutes;
        }
        return (p, m);
    }

    public static (DateTime date, int minutes)[] Last7Days()
    {
        var list = new (DateTime, int)[7];
        var today = DateTime.Today;
        for (int i = 0; i < 7; i++)
        {
            var d = today.AddDays(i - 6);
            list[i] = (d, GetDay(d).FocusMinutes);
        }
        return list;
    }

    public static (int pomodoros, int minutes) Total()
    {
        int p = 0, m = 0;
        foreach (var v in _data.Days.Values)
        {
            p += v.Pomodoros;
            m += v.FocusMinutes;
        }
        return (p, m);
    }

    public static int ActiveDays() => _data.Days.Count(v => v.Value.FocusMinutes > 0);
}
