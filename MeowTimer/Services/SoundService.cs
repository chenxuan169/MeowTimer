using System.IO;
using System.Media;
using System.Windows.Media;

namespace MeowTimer.Services;

/// <summary>内置音效定义（全部实时合成，不占体积）。</summary>
public sealed class SoundItem
{
    public string Id { get; init; } = "";
    public string NameZh { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string Display => L.Lang == "en" ? NameEn : NameZh;
    public override string ToString() => Display;
}

public static class SoundService
{
    private static SoundPlayer? _player;
    private static MediaPlayer? _media;
    private static string? _currentMediaPath;

    /// <summary>默认音效目录：%LOCALAPPDATA%\MeowTimer\sounds</summary>
    public static string DefaultDir => Path.Combine(ConfigService.DataDir, "sounds");

    public static readonly string[] AllowedExtensions = { ".wav", ".mp3", ".m4a", ".wma", ".flac", ".aac", ".aiff" };

    public static IReadOnlyList<SoundItem> Builtins { get; } = new List<SoundItem>
    {
        new() { Id = "builtin:meow",    NameZh = "喵叫",     NameEn = "Meow" },
        new() { Id = "builtin:chime",   NameZh = "叮咚",     NameEn = "Chime" },
        new() { Id = "builtin:bell",    NameZh = "铃铛",     NameEn = "Bell" },
        new() { Id = "builtin:marimba", NameZh = "木琴三音", NameEn = "Marimba" },
        new() { Id = "builtin:drop",    NameZh = "水滴",     NameEn = "Water drop" },
        new() { Id = "builtin:notify",  NameZh = "三连提示", NameEn = "Triple beep" },
        new() { Id = "builtin:soft",    NameZh = "轻柔双音", NameEn = "Soft ding" }
    };

    public static string DisplayOf(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "";
        var b = Builtins.FirstOrDefault(x => x.Id == id);
        if (b != null) return b.Display;
        if (id.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return Path.GetFileNameWithoutExtension(id.Substring(5));
        return id;
    }

    /// <summary>扫描目录里所有可用音效文件。</summary>
    public static IEnumerable<string> ScanFiles(string? dir)
    {
        var d = string.IsNullOrWhiteSpace(dir) ? DefaultDir : dir;
        if (!Directory.Exists(d)) yield break;

        List<string> files;
        try { files = Directory.EnumerateFiles(d).OrderBy(x => Path.GetFileName(x), StringComparer.OrdinalIgnoreCase).ToList(); }
        catch { yield break; }

        foreach (var f in files)
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (AllowedExtensions.Contains(ext)) yield return f;
        }
    }

    /// <summary>首次运行时把几个内置音效导出成 wav，顺带放一份使用说明。</summary>
    public static void EnsureSampleFiles()
    {
        try
        {
            var dir = DefaultDir;
            Directory.CreateDirectory(dir);

            var readme = Path.Combine(dir, "把音效放这里.txt");
            if (!File.Exists(readme))
            {
                File.WriteAllText(readme,
                    "把想用来提醒的音频文件（.wav / .mp3 / .m4a / .wma）放进这个文件夹，\r\n" +
                    "然后在「设置 → 提醒 → 音效」里就能选到它们啦。\r\n\r\n" +
                    "下面 3 个是自带的示例，可以删掉。\r\n",
                    System.Text.Encoding.UTF8);
            }

            var samples = new (string file, string id)[]
            {
                ("示例-叮咚.wav", "builtin:chime"),
                ("示例-铃铛.wav", "builtin:bell"),
                ("示例-木琴.wav", "builtin:marimba")
            };

            foreach (var (file, id) in samples)
            {
                var path = Path.Combine(dir, file);
                if (!File.Exists(path)) File.WriteAllBytes(path, Synth(id, 85));
            }
        }
        catch
        {
            // 目录不可写时忽略
        }
    }

    // ==================== 播放 ====================

    // 合成一次就缓存住，重复提醒直接用，不用每次现算
    private static readonly Dictionary<string, byte[]> Cache = new();
    private const int MaxCache = 24;

    public static void Play(string? id, int volume)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        try
        {
            if (id.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase))
            {
                var key = id + "@" + volume;

                if (!Cache.TryGetValue(key, out var wav))
                {
                    wav = Synth(id, volume);
                    if (Cache.Count >= MaxCache) Cache.Clear();
                    Cache[key] = wav;
                }

                // 复用同一个 SoundPlayer，少一次对象创建
                _player ??= new SoundPlayer();
                _player.Stream = new MemoryStream(wav);
                _player.Play();
                return;
            }

            if (id.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                var path = id.Substring(5);

                // 文件被移走/删掉了：退回内置喵叫，至少还有声
                if (!File.Exists(path))
                {
                    Play("builtin:meow", volume);
                    return;
                }

                if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    _player ??= new SoundPlayer();
                    _player.SoundLocation = path;
                    _player.Play();
                }
                else
                {
                    // MediaPlayer 复用：同一个文件不重复 Open，避免每次都要重新解析
                    if (_media == null)
                    {
                        _media = new MediaPlayer();
                        _media.MediaFailed += (_, _) => { };
                    }

                    _media.Volume = Math.Clamp(volume, 0, 100) / 100.0;

                    if (!string.Equals(_currentMediaPath, path, StringComparison.OrdinalIgnoreCase))
                    {
                        _media.Close();
                        _media.Open(new Uri(path));
                        _currentMediaPath = path;
                    }

                    _media.Position = TimeSpan.Zero;
                    _media.Play();
                }
            }
        }
        catch
        {
            // 播放失败不影响计时
        }
    }

    public static void PlaySystem(int volume)
    {
        try { SystemSounds.Asterisk.Play(); } catch { }
    }

    /// <summary>提前把提醒音准备好，真正响的时候不用现加载（避免"慢半拍"）。</summary>
    public static void Preload(string? id, int volume)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id)) return;

            if (id.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase))
            {
                var key = id + "@" + volume;
                if (Cache.ContainsKey(key)) return;

                if (Cache.Count >= MaxCache) Cache.Clear();
                Cache[key] = Synth(id, volume);
                return;
            }

            if (id.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                var path = id.Substring(5);
                if (!File.Exists(path)) return;
                if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) return;

                if (_media == null)
                {
                    _media = new MediaPlayer();
                    _media.MediaFailed += (_, _) => { };
                }

                if (!string.Equals(_currentMediaPath, path, StringComparison.OrdinalIgnoreCase))
                {
                    _media.Close();
                    _media.Open(new Uri(path));
                    _currentMediaPath = path;
                }
            }
        }
        catch
        {
            // 预加载失败不影响正常播放
        }
    }

    public static void StopAll()
    {
        try { _player?.Stop(); } catch { }
        try { _media?.Close(); _media = null; } catch { }
    }

    // ==================== 合成 ====================

    public static byte[] Synth(string id, int volume)
    {
        var key = id.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase)
            ? id.Substring(8).ToLowerInvariant()
            : id.ToLowerInvariant();

        return key switch
        {
            "chime" => Chime(volume),
            "bell" => Bell(volume),
            "marimba" => Marimba(volume),
            "drop" => Drop(volume),
            "notify" => Notify(volume),
            "soft" => Soft(volume),
            _ => Meow(volume)
        };
    }

    /// <summary>生成 16bit 单声道 44.1kHz 的 wav。</summary>
    private static byte[] Render(double duration, int volume, Func<double, double> gen)
    {
        const int rate = 44100;
        int n = (int)(rate * duration);
        double amp = Math.Clamp(volume, 0, 100) / 100.0 * 0.62;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int dataSize = n * 2;
        bw.Write("RIFF"u8.ToArray());
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8.ToArray());
        bw.Write("fmt "u8.ToArray());
        bw.Write(16);
        bw.Write((short)1);        // PCM
        bw.Write((short)1);        // mono
        bw.Write(rate);
        bw.Write(rate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write("data"u8.ToArray());
        bw.Write(dataSize);

        for (int i = 0; i < n; i++)
        {
            double s = Math.Clamp(gen((double)i / rate) * amp, -1, 1);
            bw.Write((short)(s * 32767));
        }

        bw.Flush();
        return ms.ToArray();
    }

    private static byte[] Meow(int vol)
    {
        const double dur = 0.46;
        double phase = 0;
        return Render(dur, vol, t =>
        {
            double x = Math.Min(1, t / dur);
            double f = 505 + 430 * Math.Sin(Math.PI * Math.Pow(x, 0.82)) - 130 * x;
            f += 11 * Math.Sin(2 * Math.PI * 23 * t);
            phase += 2 * Math.PI * f / 44100.0;

            double env = Math.Pow(Math.Sin(Math.PI * Math.Min(1, x * 1.04)), 0.65) * Math.Min(1, t / 0.025);
            env *= Math.Max(0, 1 - Math.Pow(x, 6) * 0.5);

            return (Math.Sin(phase) * 0.70 + Math.Sin(phase * 2) * 0.20 + Math.Sin(phase * 3) * 0.07) * env;
        });
    }

    /// <summary>叮——咚，两个钟音。</summary>
    private static byte[] Chime(int vol)
    {
        const double dur = 1.25;
        double p1 = 0, p2 = 0;
        return Render(dur, vol, t =>
        {
            p1 += 2 * Math.PI * 1046.5 / 44100.0;
            double s = Math.Sin(p1) * Math.Exp(-6.2 * t) * Math.Min(1, t / 0.005);

            if (t >= 0.17)
            {
                double t2 = t - 0.17;
                p2 += 2 * Math.PI * 1318.5 / 44100.0;
                double e2 = Math.Exp(-4.4 * t2) * Math.Min(1, t2 / 0.005);
                s += (Math.Sin(p2) + Math.Sin(p2 * 2) * 0.22) * e2 * 0.92;
            }
            return s * 0.72;
        });
    }

    private static byte[] Bell(int vol)
    {
        const double dur = 1.7;
        double p = 0;
        return Render(dur, vol, t =>
        {
            p += 2 * Math.PI * 880 / 44100.0;
            double e = Math.Exp(-2.6 * t) * Math.Min(1, t / 0.004);
            return (Math.Sin(p) + Math.Sin(p * 2.01) * 0.45 + Math.Sin(p * 3.02) * 0.2) * e * 0.6;
        });
    }

    /// <summary>木琴三音上行。</summary>
    private static byte[] Marimba(int vol)
    {
        const double dur = 0.95;
        double[] freqs = { 523.25, 659.25, 783.99 };
        double[] p = new double[3];
        return Render(dur, vol, t =>
        {
            double s = 0;
            for (int k = 0; k < 3; k++)
            {
                double start = k * 0.135;
                if (t < start) continue;
                double tt = t - start;
                p[k] += 2 * Math.PI * freqs[k] / 44100.0;
                double e = Math.Exp(-8.5 * tt) * Math.Min(1, tt / 0.003);
                s += (Math.Sin(p[k]) + Math.Sin(p[k] * 4) * 0.22) * e;
            }
            return s * 0.5;
        });
    }

    private static byte[] Drop(int vol)
    {
        const double dur = 0.55;
        double p = 0;
        return Render(dur, vol, t =>
        {
            double f = 300 + 1200 * Math.Exp(-9 * t);
            p += 2 * Math.PI * f / 44100.0;
            double e = Math.Exp(-7 * t) * Math.Min(1, t / 0.003);
            return Math.Sin(p) * e * 0.85;
        });
    }

    /// <summary>三连短提示，最像系统提示音。</summary>
    private static byte[] Notify(int vol)
    {
        const double dur = 0.78;
        double p = 0;
        return Render(dur, vol, t =>
        {
            int idx = (int)(t / 0.15);
            if (idx > 2) return 0;
            double local = t - idx * 0.15;
            if (local > 0.085) return 0;

            double f = idx == 2 ? 1174.7 : 880;
            p += 2 * Math.PI * f / 44100.0;
            double e = Math.Sin(Math.PI * local / 0.085);
            return Math.Sin(p) * e * 0.55;
        });
    }

    /// <summary>轻柔双音，适合"回来啦"这种场景。</summary>
    private static byte[] Soft(int vol)
    {
        const double dur = 1.1;
        double p1 = 0, p2 = 0;
        return Render(dur, vol, t =>
        {
            p1 += 2 * Math.PI * 659.25 / 44100.0;
            p2 += 2 * Math.PI * 987.77 / 44100.0;

            double e1 = Math.Exp(-3.4 * t) * Math.Min(1, t / 0.006);
            double e2 = 0;
            if (t >= 0.12)
            {
                double t2 = t - 0.12;
                e2 = Math.Exp(-3.4 * t2) * Math.Min(1, t2 / 0.006);
            }
            return (Math.Sin(p1) * e1 + Math.Sin(p2) * e2 * 0.62) * 0.45;
        });
    }

    public static byte[] BuildMeowWav(int volume) => Meow(volume);
}
