using System.Windows.Media;

namespace MeowTimer.Services;

public sealed class Theme
{
    public string Id { get; init; } = "sakura";
    public string NameZh { get; init; } = "";
    public string NameEn { get; init; } = "";
    public Color From { get; init; }
    public Color To { get; init; }
    public Color Text { get; init; }
    public Color SubText { get; init; }
    public Color Accent { get; init; }
    public Color Glow { get; init; }

    /// <summary>无背景时给文字做描边的深色（跟着主题走，这样换主题能看出来）。</summary>
    public Color Shadow { get; init; } = Colors.Black;

    /// <summary>深色主题：整套界面用深色底 + 浅色字。</summary>
    public bool IsDark { get; init; }

    public string DisplayName(string lang) => lang == "en" ? NameEn : NameZh;

    /// <summary>供下拉框直接绑定。</summary>
    public string Display => L.Lang == "en" ? NameEn : NameZh;

    public override string ToString() => Display;
}

public static class ThemeService
{
    public static IReadOnlyList<Theme> All { get; } = new List<Theme>
    {
        new()
        {
            Id = "sakura", NameZh = "樱花粉", NameEn = "Sakura",
            From = C("#F9558B"), To = C("#FF8FB2"), Text = C("#FFFFFF"),
            SubText = C("#FFF0F5"), Accent = C("#FFE1EC"), Glow = C("#FFB3CC"), Shadow = C("#7A2543")
        },
        new()
        {
            Id = "cream", NameZh = "奶油布丁", NameEn = "Cream",
            From = C("#FFD79A"), To = C("#FFB07C"), Text = C("#7A4B2A"),
            SubText = C("#9C6A44"), Accent = C("#FFF1DC"), Glow = C("#FFCF9E"), Shadow = C("#6B3E1F")
        },
        new()
        {
            Id = "mint", NameZh = "薄荷苏打", NameEn = "Mint",
            From = C("#6FD6B8"), To = C("#A8E6CF"), Text = C("#0F4C3A"),
            SubText = C("#2C6E5A"), Accent = C("#E8FBF3"), Glow = C("#8FE8C9"), Shadow = C("#0B3D2E")
        },
        new()
        {
            Id = "night", NameZh = "夜空紫", NameEn = "Night",
            From = C("#2B2A4A"), To = C("#514287"), Text = C("#F2EDFF"),
            SubText = C("#C3B8EE"), Accent = C("#6E5BB8"), Glow = C("#8B7BE8"), Shadow = C("#171632"), IsDark = true
        },
        new()
        {
            Id = "sunset", NameZh = "落日橘", NameEn = "Sunset",
            From = C("#FF7E5F"), To = C("#FEB47B"), Text = C("#FFFFFF"),
            SubText = C("#FFF0E6"), Accent = C("#FFD9C7"), Glow = C("#FF9E7A"), Shadow = C("#7D3018")
        },
        new()
        {
            Id = "mono", NameZh = "极简黑", NameEn = "Mono",
            From = C("#1C1C1E"), To = C("#3A3A3C"), Text = C("#F5F5F7"),
            SubText = C("#AEAEB2"), Accent = C("#4A4A4D"), Glow = C("#8E8E93"), Shadow = C("#000000"), IsDark = true
        }
    };

    public static Theme Get(string id)
        => All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public static Color C(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Colors.Transparent;
        if (!hex.StartsWith("#")) hex = "#" + hex;
        return (Color)ColorConverter.ConvertFromString(hex);
    }

    public static LinearGradientBrush CardBrush(Theme t, double opacity)
    {
        var a = (byte)Math.Clamp(opacity * 255, 0, 255);
        var brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1)
        };
        brush.GradientStops.Add(new GradientStop(WithAlpha(t.From, a), 0));
        brush.GradientStops.Add(new GradientStop(WithAlpha(t.To, a), 1));
        brush.Freeze();
        return brush;
    }

    public static SolidColorBrush Solid(Color c) => new(c);

    /// <summary>0~1 的相对亮度，用来判断字色深浅。</summary>
    public static double Luminance(Color c)
        => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;

    public static Color WithAlpha(Color c, byte a) => Color.FromArgb(a, c.R, c.G, c.B);

    /// <summary>按用户设置生成窗口背景：渐变 / 纯色 / 完全透明。</summary>
    public static Brush BuildBackground(Models.AppConfig cfg, Theme t)
    {
        double op = Math.Clamp(cfg.CardOpacity, 0, 1);
        byte alpha = (byte)Math.Round(op * 255);

        if (op <= 0.001) return Brushes.Transparent;

        switch ((cfg.BackgroundStyle ?? "gradient").ToLowerInvariant())
        {
            case "none":
                return Brushes.Transparent;

            case "solid":
                var c = string.IsNullOrWhiteSpace(cfg.BackgroundColor) ? t.From : C(cfg.BackgroundColor);
                return Solid(WithAlpha(c, alpha));

            default:
                return CardBrush(t, op);
        }
    }
}
