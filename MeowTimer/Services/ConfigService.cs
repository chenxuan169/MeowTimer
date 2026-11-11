using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeowTimer.Models;

namespace MeowTimer.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    /// <summary>数据目录。可用环境变量 MEOWTIMER_DATA 覆盖（方便做多套配置测试）。</summary>
    public static string DataDir { get; } =
        Environment.GetEnvironmentVariable("MEOWTIMER_DATA") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeowTimer");

    public static string ConfigPath => Path.Combine(DataDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json, Options);
                if (cfg != null)
                {
                    bool changed = MigrateLegacySound(cfg);
                    changed |= MigrateLegacyDefaultTime(cfg);
                    changed |= MigrateTextColor(cfg);
                    if (changed) Save(cfg);
                    return cfg;
                }
            }
        }
        catch
        {
            // 配置损坏时退回默认值，不影响启动
        }
        return new AppConfig();
    }

    /// <summary>文字颜色以前是"跟随主题"，现在改成固定值：把当时的颜色固化下来。</summary>
    private static bool MigrateTextColor(AppConfig cfg)
    {
        if (!string.IsNullOrWhiteSpace(cfg.TextColor)) return false;

        var t = ThemeService.Get(cfg.Theme);
        var c = t.Text;
        cfg.TextColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        return true;
    }

    /// <summary>旧版只有"默认倒计时（分钟）"，迁移成秒。</summary>
    private static bool MigrateLegacyDefaultTime(AppConfig cfg)
    {
        bool changed = false;

        if (cfg.DefaultMinutes is > 0)
        {
            cfg.DefaultSeconds = cfg.DefaultMinutes.Value * 60;
            cfg.DefaultMinutes = null;
            changed = true;
        }

        if (cfg.PomodoroWork is > 0)
        {
            cfg.PomodoroWorkSeconds = cfg.PomodoroWork.Value * 60;
            cfg.PomodoroWork = null;
            changed = true;
        }

        if (cfg.PomodoroShortBreak is > 0)
        {
            cfg.PomodoroShortBreakSeconds = cfg.PomodoroShortBreak.Value * 60;
            cfg.PomodoroShortBreak = null;
            changed = true;
        }

        if (cfg.PomodoroLongBreak is > 0)
        {
            cfg.PomodoroLongBreakSeconds = cfg.PomodoroLongBreak.Value * 60;
            cfg.PomodoroLongBreak = null;
            changed = true;
        }

        if (cfg.CycleFocusMinutes is > 0)
        {
            cfg.CycleFocusSeconds = cfg.CycleFocusMinutes.Value * 60;
            cfg.CycleFocusMinutes = null;
            changed = true;
        }

        if (cfg.CycleBreakMinutes is > 0)
        {
            cfg.CycleBreakSeconds = cfg.CycleBreakMinutes.Value * 60;
            cfg.CycleBreakMinutes = null;
            changed = true;
        }

        return changed;
    }

    /// <summary>老版本只有"单个自定义音效"，迁移成新的 SoundFinish。</summary>
    private static bool MigrateLegacySound(AppConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.SoundMode) && string.IsNullOrWhiteSpace(cfg.CustomSoundPath))
            return false;

        if (string.Equals(cfg.SoundMode, "custom", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(cfg.CustomSoundPath) &&
            File.Exists(cfg.CustomSoundPath))
        {
            cfg.SoundFinish = "file:" + cfg.CustomSoundPath;
        }
        else if (string.Equals(cfg.SoundMode, "system", StringComparison.OrdinalIgnoreCase))
        {
            cfg.SoundFinish = "builtin:notify";
        }

        cfg.SoundMode = null;
        cfg.CustomSoundPath = null;
        return true;
    }

    public static void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonSerializer.Serialize(config, Options);
            var tmp = ConfigPath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, ConfigPath, true);
            File.Delete(tmp);
        }
        catch
        {
            // 静默失败：不因为写配置失败打断计时
        }
    }

    /// <summary>深拷贝一份配置，供设置窗口编辑后再决定是否写回。</summary>
    public static AppConfig Clone(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, Options);
            return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    /// <summary>把 src 的所有属性复制到 dst（保持 dst 实例引用不变）。</summary>
    public static void CopyInto(AppConfig src, AppConfig dst)
    {
        foreach (var p in typeof(AppConfig).GetProperties())
        {
            if (!p.CanRead || !p.CanWrite) continue;
            p.SetValue(dst, p.GetValue(src));
        }
    }
}
