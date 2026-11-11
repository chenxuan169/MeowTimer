using Avalonia.Media;

namespace MeowTimer.Services;

public sealed class Theme
{
    public string Id { get; init; } = "";
    public string NameZh { get; init; } = "";
    public string NameEn { get; init; } = "";

    public Color From { get; init; }
    public Color To { get; init; }
    public Color Text { get; init; }
    public Color SubText { get; init; }
    public Color Accent { get; init; }
    public Color Glow { get; init; }
    public Color Shadow { get; init; } = Colors.Black;

    public bool IsDark { get; init; }

    public string Name => L.Lang == "en" ? NameEn : NameZh;
}

public static class ThemeService
{
    private static Color C(string hex) => Color.Parse(hex);

    public static readonly Theme[] All =
    {
        new()
        {
            Id = "sakura", NameZh = "樱花粉", NameEn = "Sakura",
            From = C("#F9558B"), To = C("#FF9EC0"),
            Text = C("#FFFFFF"), SubText = C("#FFF0F5"),
            Accent = C("#FFE1EC"), Glow = C("#FFB3CC"), Shadow = C("#7A2543")
        },
        new()
        {
            Id = "cream", NameZh = "奶油布丁", NameEn = "Cream",
            From = C("#FFD79A"), To = C("#FFB07C"),
            Text = C("#7A4B2A"), SubText = C("#9C6A44"),
            Accent = C("#FFF1DC"), Glow = C("#FFCF9E"), Shadow = C("#6B3E1F")
        },
        new()
        {
            Id = "mint", NameZh = "薄荷苏打", NameEn = "Mint",
            From = C("#6FD6B8"), To = C("#A8E8D8"),
            Text = C("#0F4C3A"), SubText = C("#2C6E5A"),
            Accent = C("#E8FBF3"), Glow = C("#8FE8C9"), Shadow = C("#0B3D2E")
        },
        new()
        {
            Id = "night", NameZh = "夜空紫", NameEn = "Night",
            From = C("#2B2A4A"), To = C("#4A3D7A"),
            Text = C("#F2EDFF"), SubText = C("#C3B8EE"),
            Accent = C("#6E5BB8"), Glow = C("#8B7BE8"), Shadow = C("#171632"),
            IsDark = true
        },
        new()
        {
            Id = "sunset", NameZh = "落日橘", NameEn = "Sunset",
            From = C("#FF7E5F"), To = C("#FFB88C"),
            Text = C("#FFFFFF"), SubText = C("#FFF0E6"),
            Accent = C("#FFD9C7"), Glow = C("#FF9E7A"), Shadow = C("#7D3018")
        },
        new()
        {
            Id = "mono", NameZh = "极简黑", NameEn = "Mono",
            From = C("#1C1C1E"), To = C("#3A3A3C"),
            Text = C("#F5F5F7"), SubText = C("#AEAEB2"),
            Accent = C("#4A4A4D"), Glow = C("#8E8E93"), Shadow = C("#000000"),
            IsDark = true
        }
    };

    public static Theme Get(string? id)
        => All.FirstOrDefault(t => t.Id == id) ?? All[0];

    public static Color Parse(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try { return Color.Parse(hex); }
        catch { return fallback; }
    }

    public static Color WithAlpha(Color c, byte a) => Color.FromArgb(a, c.R, c.G, c.B);

    public static double Luminance(Color c)
        => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;

    /// <summary>卡片背景：渐变 / 纯色 / 透明。</summary>
    public static IBrush BuildBackground(Models.AppConfig cfg, Theme t)
    {
        bool noBg = cfg.BackgroundStyle == "none" || cfg.CardOpacity <= 0.001;
        if (noBg) return Brushes.Transparent;

        byte alpha = (byte)Math.Clamp((int)Math.Round(cfg.CardOpacity * 255), 0, 255);

        if (cfg.BackgroundStyle == "solid")
        {
            var c = Parse(cfg.BackgroundColor, t.From);
            return new SolidColorBrush(WithAlpha(c, alpha));
        }

        return new LinearGradientBrush
        {
            StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
            EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(WithAlpha(t.From, alpha), 0),
                new GradientStop(WithAlpha(t.To, alpha), 1)
            }
        };
    }
}
