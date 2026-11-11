using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MeowTimer.Services;

public sealed class SoundItem
{
    public string Id { get; init; } = "";
    public string NameZh { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string Name => L.Lang == "en" ? NameEn : NameZh;
}

/// <summary>
/// 音效播放。合成用共用的 SoundSynth，播放按平台走：
/// macOS 用系统自带的 afplay，Windows 用 winmm 的 PlaySound。
/// </summary>
public static class SoundService
{
    public static IReadOnlyList<SoundItem> Builtins { get; } = new List<SoundItem>
    {
        new() { Id = "builtin:meow",    NameZh = "喵叫",   NameEn = "Meow" },
        new() { Id = "builtin:chime",   NameZh = "叮咚",   NameEn = "Chime" },
        new() { Id = "builtin:bell",    NameZh = "铃铛",   NameEn = "Bell" },
        new() { Id = "builtin:marimba", NameZh = "木琴三音", NameEn = "Marimba" },
        new() { Id = "builtin:drop",    NameZh = "水滴",   NameEn = "Drop" },
        new() { Id = "builtin:notify",  NameZh = "三连提示", NameEn = "Notify" },
        new() { Id = "builtin:soft",    NameZh = "轻柔双音", NameEn = "Soft" }
    };

    private static readonly Dictionary<string, string> FileCache = new();

    private static string CacheDir
    {
        get
        {
            var dir = Path.Combine(Path.GetTempPath(), "meowtimer-sounds");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string sound, IntPtr hmod, uint flags);

    /// <summary>把音效落成临时 wav 文件（afplay 需要一个真实路径）。</summary>
    private static string? ResolveFile(string? id, int volume)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        if (id.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase))
        {
            var key = id + "@" + volume;
            if (FileCache.TryGetValue(key, out var cached) && File.Exists(cached)) return cached;

            var name = id.Substring(8).ToLowerInvariant();
            var path = Path.Combine(CacheDir, $"{name}-{volume}.wav");
            if (!File.Exists(path)) File.WriteAllBytes(path, SoundSynth.Synth(id, volume));

            FileCache[key] = path;
            return path;
        }

        if (id.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            var path = id.Substring(5);
            return File.Exists(path) ? path : null;
        }

        return null;
    }

    public static void Play(string? id, int volume)
    {
        try
        {
            var path = ResolveFile(id, volume);
            if (path == null) return;

            if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo("afplay", $"\"{path}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            else if (OperatingSystem.IsWindows())
            {
                // SND_FILENAME | SND_ASYNC
                PlaySound(path, IntPtr.Zero, 0x00020000 | 0x0001);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo("paplay", $"\"{path}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
        }
        catch
        {
            // 播不出声不影响计时
        }
    }
}
