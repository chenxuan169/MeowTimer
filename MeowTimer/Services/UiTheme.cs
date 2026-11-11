using System.Windows;
using System.Windows.Media;

namespace MeowTimer.Services;

/// <summary>把当前主题的配色同步到全局资源，让设置面板、对话框都跟着换色。</summary>
public static class UiTheme
{
    /// <summary>a 占 amountA，b 占 (1-amountA)。</summary>
    public static Color Mix(Color a, Color b, double amountA)
        => Color.FromRgb(
            (byte)(a.R * amountA + b.R * (1 - amountA)),
            (byte)(a.G * amountA + b.G * (1 - amountA)),
            (byte)(a.B * amountA + b.B * (1 - amountA)));

    /// <summary>keep = 保留多少亮度（0.5 就是暗一半）。</summary>
    private static Color Shade(Color c, double keep) => Mix(c, Colors.Black, keep);

    private static void SetColor(string key, Color c)
    {
        var res = Application.Current?.Resources;
        if (res == null) return;

        if (res[key] is SolidColorBrush b && !b.IsFrozen)
            b.Color = c;
        else if (res[key] is Color)
            res[key] = c;                 // 渐变点用的 Color 资源
        else
            res[key] = new SolidColorBrush(c);
    }

    public static void Apply(Theme t)
    {
        if (Application.Current == null) return;

        if (t.IsDark) ApplyDark(t);
        else ApplyLight(t);
    }

    /// <summary>浅色主题：白底，由深到浅铺一层主题色。</summary>
    private static void ApplyLight(Theme t)
    {
        SetColor("AccentBrush", t.From);
        SetColor("AccentSoftBrush", Mix(t.From, Colors.White, 0.16));
        SetColor("LineBrush", Mix(t.From, Colors.White, 0.13));
        SetColor("TextBrush", t.Shadow);
        SetColor("PrimaryFromColor", t.From);
        SetColor("PrimaryToColor", t.To);
        SetColor("SubTextBrush", Mix(t.Shadow, Colors.White, 0.52));

        SetColor("PanelBrush", Mix(t.From, Colors.White, 0.085));
        SetColor("PanelEdgeBrush", Mix(t.From, Colors.White, 0.18));
        SetColor("FieldBrush", Mix(t.From, Colors.White, 0.062));
        SetColor("CardBrush", Mix(t.From, Colors.White, 0.030));
        SetColor("CardEdgeBrush", Mix(t.From, Colors.White, 0.14));
    }

    /// <summary>深色主题：整体压暗，强调色换成更亮的那一档。</summary>
    private static void ApplyDark(Theme t)
    {
        var accent = t.Glow;

        SetColor("AccentBrush", accent);
        SetColor("AccentSoftBrush", Mix(accent, Shade(t.From, 0.45), 0.42));
        SetColor("LineBrush", Mix(t.From, Colors.White, 0.24));
        SetColor("TextBrush", Color.FromRgb(0xF2, 0xF2, 0xF5));
        SetColor("PrimaryFromColor", accent);
        SetColor("PrimaryToColor", Mix(accent, t.From, 0.70));
        SetColor("SubTextBrush", Mix(t.From, Colors.White, 0.60));

        SetColor("PanelBrush", Shade(t.From, 0.50));
        SetColor("PanelEdgeBrush", Mix(t.From, Colors.White, 0.28));
        SetColor("FieldBrush", Shade(t.From, 0.60));
        SetColor("CardBrush", Shade(t.From, 0.72));
        SetColor("CardEdgeBrush", Mix(t.From, Colors.White, 0.20));
    }
}
