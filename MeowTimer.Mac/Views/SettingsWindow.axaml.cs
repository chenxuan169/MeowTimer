using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MeowTimer.Services;

namespace MeowTimerMac.Views;

public sealed class ColorOpt
{
    public string Name { get; init; } = "";
    public string Value { get; init; } = "";
    public IBrush Swatch { get; init; } = Brushes.White;
}

public partial class SettingsWindow : Window
{
    public List<Theme> Themes { get; } = ThemeService.All.ToList();

    public List<ColorOpt> TextColors { get; } = new()
    {
        new() { Name = "跟随主题", Value = "", Swatch = new SolidColorBrush(Color.Parse("#FFB3CC")) },
        new() { Name = "白色", Value = "#FFFFFF", Swatch = Brushes.White },
        new() { Name = "近黑", Value = "#1C1C1E", Swatch = new SolidColorBrush(Color.Parse("#1C1C1E")) },
        new() { Name = "深灰", Value = "#4A3844", Swatch = new SolidColorBrush(Color.Parse("#4A3844")) },
        new() { Name = "粉", Value = "#F2558B", Swatch = new SolidColorBrush(Color.Parse("#F2558B")) },
        new() { Name = "紫", Value = "#8B7BE8", Swatch = new SolidColorBrush(Color.Parse("#8B7BE8")) },
        new() { Name = "蓝", Value = "#3B82F6", Swatch = new SolidColorBrush(Color.Parse("#3B82F6")) },
        new() { Name = "绿", Value = "#10B981", Swatch = new SolidColorBrush(Color.Parse("#10B981")) },
        new() { Name = "橙", Value = "#F59E0B", Swatch = new SolidColorBrush(Color.Parse("#F59E0B")) }
    };

    public List<SoundItem> Sounds { get; } = BuildSounds();

    private static List<SoundItem> BuildSounds()
    {
        var list = SoundService.Builtins.ToList();

        try
        {
            var dir = string.IsNullOrWhiteSpace(App.Config.SoundDir)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeowTimer", "sounds")
                : App.Config.SoundDir;

            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.EnumerateFiles(dir).OrderBy(x => x))
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext is not (".wav" or ".mp3" or ".m4a" or ".aiff" or ".flac")) continue;

                    list.Add(new SoundItem
                    {
                        Id = "file:" + f,
                        NameZh = "我的 · " + Path.GetFileNameWithoutExtension(f),
                        NameEn = "Mine · " + Path.GetFileNameWithoutExtension(f)
                    });
                }
            }
        }
        catch { }

        return list;
    }

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = this;

        LoadValues();

        OpacitySlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
                OpacityLabel.Text = $"{(int)Math.Round(OpacitySlider.Value)}%";
        };
    }

    private void LoadValues()
    {
        var cfg = App.Config;

        ThemeBox.SelectedItem = ThemeService.All.FirstOrDefault(t => t.Id == cfg.Theme) ?? ThemeService.All[0];

        BgBox.SelectedIndex = cfg.BackgroundStyle switch
        {
            "solid" => 1,
            "none" => 2,
            _ => 0
        };

        OpacitySlider.Value = Math.Round(Math.Clamp(cfg.CardOpacity, 0, 1) * 100);
        OpacityLabel.Text = $"{(int)OpacitySlider.Value}%";

        TextColorBox.SelectedItem = TextColors.FirstOrDefault(c => c.Value == cfg.TextColor) ?? TextColors[0];

        FontSizeBox.Value = (decimal)Math.Clamp(cfg.FontSize, 18, 130);

        int ds = Math.Max(0, cfg.DefaultSeconds);
        DefHour.Value = (decimal)(ds / 3600);
        DefMin.Value = (decimal)(ds % 3600 / 60);
        DefSec.Value = (decimal)(ds % 60);

        SoundCheck.IsChecked = cfg.SoundEnabled;
        SoundBox.SelectedItem = Sounds.FirstOrDefault(s => s.Id == cfg.SoundFinish) ?? Sounds[0];

        TopmostCheck.IsChecked = cfg.Topmost;
        LockCheck.IsChecked = cfg.LockPosition;

        UpdateBgState();
    }

    private void OnBgChanged(object? sender, SelectionChangedEventArgs e) => UpdateBgState();

    private void UpdateBgState()
    {
        bool none = BgBox.SelectedIndex == 2;
        OpacitySlider.IsEnabled = !none;
        OpacitySlider.Opacity = none ? 0.35 : 1;
        OpacityLabel.Opacity = none ? 0.3 : 0.6;
    }

    private void OnPreviewSound(object? sender, RoutedEventArgs e)
    {
        if (SoundBox.SelectedItem is SoundItem s)
            SoundService.Play(s.Id, App.Config.SoundVolume);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var cfg = App.Config;

        cfg.Theme = (ThemeBox.SelectedItem as Theme)?.Id ?? "sakura";
        cfg.BackgroundStyle = BgBox.SelectedIndex switch { 1 => "solid", 2 => "none", _ => "gradient" };
        cfg.CardOpacity = Math.Round(OpacitySlider.Value / 100.0, 2);
        cfg.TextColor = (TextColorBox.SelectedItem as ColorOpt)?.Value ?? "#FFFFFF";
        cfg.FontSize = (int)Math.Round(FontSizeBox.Value ?? 46);

        int h = (int)(DefHour.Value ?? 0);
        int m = (int)(DefMin.Value ?? 0);
        int s = (int)(DefSec.Value ?? 0);
        cfg.DefaultSeconds = Math.Max(1, h * 3600 + m * 60 + s);

        cfg.SoundEnabled = SoundCheck.IsChecked == true;
        cfg.SoundFinish = (SoundBox.SelectedItem as SoundItem)?.Id ?? "builtin:meow";
        cfg.Topmost = TopmostCheck.IsChecked == true;
        cfg.LockPosition = LockCheck.IsChecked == true;

        App.Save();
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
