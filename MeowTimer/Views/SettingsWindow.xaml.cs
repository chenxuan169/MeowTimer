using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using MeowTimer.Models;
using MeowTimer.Services;

namespace MeowTimer.Views;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _cfg;
    private readonly AppConfig _original;
    private AppConfig _work;
    private bool _loading;
    private bool _initialized;
    private DispatcherTimer? _previewTimer;

    public SettingsWindow(AppConfig cfg)
    {
        _cfg = cfg;
        _original = ConfigService.Clone(cfg);
        _work = ConfigService.Clone(cfg);

        InitializeComponent();
        _initialized = true;

        Localize();
        FillCombos();
        BuildPalette();
        LoadValues();
        StartSoundWatcher();

        ApplyThemeResources(ThemeService.Get(_work.Theme));
    }

    // ==================== 文本 ====================

    private void Localize()
    {
        HeaderText.Text = L.T("menu.settings").TrimEnd('…', '.');

        SecAppearance.Text = L.T("sec.appearance");
        PreviewLabel.Text = L.T("preview.label");
        LblMinUnit.Text = L.T("unit.minute");
        LblSecUnit.Text = L.T("unit.second");
        LblTheme.Text = L.T("lbl.theme");
        LblFontSize.Text = L.T("lbl.fontSize");
        LblFont.Text = L.T("lbl.font");
        LblOpacity.Text = L.T("lbl.cardOpacity");
        LblBgStyle.Text = L.T("lbl.bgStyle");
        LblBgColor.Text = L.T("lbl.bgColor");
        LblTextColor.Text = L.T("lbl.textColor");
        ShowCatCheck.Content = L.T("lbl.showCat");
        ShowProgressCheck.Content = L.T("lbl.showProgress");
        ShowStatusCheck.Content = L.T("lbl.showStatus");
        TopmostCheck.Content = L.T("lbl.topmost");
        LockCheck.Content = L.T("lbl.lock");

        SecTimer.Text = L.T("sec.timer");
        LblDefaultMin.Text = L.T("lbl.defaultMinutes");
        FlashCheck.Content = L.T("lbl.flash");
        LaunchStartCheck.Content = L.T("lbl.startTimerOnLaunch");

        SecPomodoro.Text = L.T("sec.pomodoro");
        LblWork.Text = L.T("lbl.work");
        LblShort.Text = L.T("lbl.shortBreak");
        LblLong.Text = L.T("lbl.longBreak");
        LblRounds.Text = L.T("lbl.rounds");
        AutoNextCheck.Content = L.T("lbl.autoNext");

        SecCycle.Text = L.T("sec.cycle");
        LblFocusLen.Text = L.T("lbl.focusLen");
        LblBreakLen.Text = L.T("lbl.breakLen");
        LblCycleRounds.Text = L.T("lbl.cycleRounds");
        CycleAutoCheck.Content = L.T("lbl.autoNext");

        SecAlert.Text = L.T("sec.alert");
        SoundCheck.Content = L.T("lbl.sound");
        LblSoundDir.Text = L.T("lbl.soundDir");
        ChangeDirBtn.Content = L.T("btn.changeDir");
        OpenDirBtn.Content = L.T("btn.open");
        ResetDirBtn.ToolTip = L.T("btn.resetDir");
        DropHint.Text = L.T("drop.hint");
        LblSoundFinish.Text = L.T("lbl.soundFinish");
        LblSoundBreak.Text = L.T("lbl.soundBreak");
        LblSoundPre.Text = L.T("lbl.soundPre");
        LblVolume.Text = L.T("lbl.volume");
        NotifyCheck.Content = L.T("lbl.notify");
        PreAlertCheck.Content = L.T("lbl.preAlert");
        LblPreSeconds.Text = L.T("lbl.preSeconds");

        SecSystem.Text = L.T("sec.system");
        LblLang.Text = L.T("lbl.language");
        LblTrayMode.Text = L.T("lbl.trayMode");
        AutoStartCheck.Content = L.T("lbl.autoStart");
        StartMinCheck.Content = L.T("lbl.startMin");

        SecHotkey.Text = L.T("sec.hotkey");
        LblHkPause.Text = L.T("menu.start") + " / " + L.T("menu.pause");
        LblHkReset.Text = L.T("menu.reset");
        LblHkTimer.Text = L.T("menu.setTimer").TrimEnd('…', '.');
        LblHkClock.Text = L.T("menu.clock").Replace("显示", "").Replace("Show ", "");
        LblHkShow.Text = L.T("menu.hide").Replace("窗口", "").Replace("Window", "");
        LblHkTop.Text = L.T("menu.topmost");
        LblHkPomo.Text = L.T("menu.pomodoro");
        LblHkCycle.Text = L.T("sec.cycle");

        SaveBtn.Content = L.T("btn.ok");
        CancelBtn.Content = L.T("btn.cancel");
        DefaultBtn.Content = L.T("btn.reset");

        SetUnitLabels();
    }

    /// <summary>各个输入框后面的单位文字。</summary>
    private void SetUnitLabels()
    {
        string hour = L.T("unit.hour");
        string min = L.T("unit.minute");
        string sec = L.T("unit.second");

        LblHourUnit.Text = hour;
        LblWorkHour.Text = hour;
        LblShortHour.Text = hour;
        LblLongHour.Text = hour;
        LblFocusHour.Text = hour;
        LblBreakHour.Text = hour;

        LblWorkMin.Text = min;
        LblShortMin.Text = min;
        LblLongMin.Text = min;
        LblFocusMin.Text = min;
        LblBreakMin.Text = min;
        LblMinUnit.Text = min;

        LblWorkSec.Text = sec;
        LblShortSec.Text = sec;
        LblLongSec.Text = sec;
        LblFocusSec.Text = sec;
        LblBreakSec.Text = sec;
        LblPreSecUnit.Text = sec;
        LblSecUnit.Text = sec;

        LblRoundsUnit.Text = L.T("unit.count");
        LblCycleRoundsUnit.Text = L.T("unit.round");
        LblVolumePct.Text = "%";
        LblOpacityPct.Text = "%";
        LblFontPx.Text = "px";
    }

    // ==================== 下拉框 ====================

    private void FillCombos()
    {
        ThemeCombo.ItemsSource = ThemeService.All;

        FontCombo.ItemsSource = new List<string>
        {
            "Microsoft YaHei UI", "Microsoft YaHei", "等线", "幼圆", "楷体",
            "宋体", "黑体", "Segoe UI", "Consolas", "Arial", "Comic Sans MS"
        };

        LangCombo.Items.Clear();
        LangCombo.Items.Add(new ComboBoxItem { Content = "中文", Tag = "zh" });
        LangCombo.Items.Add(new ComboBoxItem { Content = "English", Tag = "en" });

        TrayModeCombo.Items.Clear();
        TrayModeCombo.Items.Add(new ComboBoxItem { Content = L.T("lbl.trayCat"), Tag = "cat" });
        TrayModeCombo.Items.Add(new ComboBoxItem { Content = L.T("lbl.trayCpu"), Tag = "cpu" });
        TrayModeCombo.Items.Add(new ComboBoxItem { Content = L.T("lbl.trayMem"), Tag = "mem" });

        BgStyleCombo.Items.Clear();
        BgStyleCombo.Items.Add(new ComboBoxItem { Content = L.T("lbl.bgGradient"), Tag = "gradient" });
        BgStyleCombo.Items.Add(new ComboBoxItem { Content = L.T("lbl.bgSolid"), Tag = "solid" });
        BgStyleCombo.Items.Add(new ComboBoxItem { Content = L.T("lbl.bgNone"), Tag = "none" });
    }

    // ==================== 背景色板 ====================

    private static readonly string[] PaletteColors =
    {
        "#F2558B", "#FF8A5C", "#FFC46B", "#5ED3A8",
        "#6BB8FF", "#9B7BFF", "#2B2A4A", "#FFFFFF"
    };

    // 第一个空串代表"跟随主题"
    private static readonly string[] TextPaletteColors =
    {
        "", "#FFFFFF", "#2B2A4A", "#4A3844", "#F2558B",
        "#8B7BE8", "#3B82F6", "#10B981", "#F59E0B"
    };

    private void BuildPalette()
    {
        FillSwatches(ColorPalette, PaletteColors, OnColorPick);
        FillSwatches(TextColorPalette, TextPaletteColors, OnTextColorPick);
        UpdateColorSelection();
    }

    private void FillSwatches(WrapPanel host, string[] colors, MouseButtonEventHandler handler)
    {
        host.Children.Clear();

        var theme = ThemeService.Get(_work.Theme);

        foreach (var hex in colors)
        {
            var b = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Margin = new Thickness(0, 0, 7, 0),
                Background = hex.Length == 0
                    ? new SolidColorBrush(theme.Text)
                    : new SolidColorBrush(ThemeService.C(hex)),
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.Transparent,
                Tag = hex,
                Cursor = Cursors.Hand,
                ToolTip = hex.Length == 0 ? L.T("lbl.followTheme") : hex
            };

            b.MouseLeftButtonDown += handler;
            host.Children.Add(b);
        }
    }

    private void OnColorPick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border b || b.Tag is not string hex) return;

        _work.BackgroundColor = hex;

        // 点色板就自动切到纯色，符合直觉
        if ((TagOf(BgStyleCombo) ?? "gradient") != "solid")
        {
            SelectTag(BgStyleCombo, "solid");
            UpdateBgControls();
        }

        UpdateColorSelection();
        UpdateThemePreview();
        RequestPreview();
        e.Handled = true;
    }

    private void OnTextColorPick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border b || b.Tag is not string hex) return;

        _work.TextColor = hex;
        UpdateColorSelection();
        RequestPreview();
        e.Handled = true;
    }

    private void UpdateColorSelection()
    {
        MarkSelected(ColorPalette, _work.BackgroundColor);
        MarkSelected(TextColorPalette, _work.TextColor ?? "");
    }

    private static void MarkSelected(WrapPanel host, string current)
    {
        foreach (var child in host.Children)
        {
            if (child is not Border b) continue;

            bool on = string.Equals(b.Tag as string ?? "", current, StringComparison.OrdinalIgnoreCase);
            b.BorderBrush = on ? Brushes.White : Brushes.Transparent;
            b.Effect = on
                ? new DropShadowEffect
                {
                    Color = ThemeService.C("#F2558B"),
                    BlurRadius = 9,
                    ShadowDepth = 0,
                    Opacity = 0.9
                }
                : null;
        }
    }

    private void OnBgStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _loading) return;
        UpdateBgControls();
        UpdateThemePreview();
        RequestPreview();
    }

    private void UpdateBgControls()
    {
        string style = TagOf(BgStyleCombo) ?? "gradient";
        bool solid = style == "solid";
        bool none = style == "none";

        ColorPalette.IsEnabled = solid;
        ColorPalette.Opacity = solid ? 1 : 0.3;
        LblBgColor.Opacity = solid ? 1 : 0.45;

        OpacityBox.IsEnabled = !none;
        OpacityBox.Opacity = none ? 0.3 : 1;
        LblOpacity.Opacity = none ? 0.45 : 1;
        LblOpacityPct.Opacity = none ? 0.3 : 1;
    }

    private string SoundDirEffective()
        => string.IsNullOrWhiteSpace(_work.SoundDir) ? SoundService.DefaultDir : _work.SoundDir;

    private List<SoundItem> BuildSoundItems()
    {
        var list = new List<SoundItem>();
        list.AddRange(SoundService.Builtins);

        var inFolder = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in SoundService.ScanFiles(SoundDirEffective()))
        {
            inFolder.Add(NormPath(f));
            list.Add(MakeFileItem(f, external: false));
        }

        // 已经绑定、但文件不在音效目录里的，也要列出来，否则一保存就会丢
        foreach (var id in new[] { _work.SoundFinish, _work.SoundBreakEnd, _work.SoundPreAlert })
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (!id.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) continue;

            var path = id.Substring(5);
            if (inFolder.Contains(NormPath(path))) continue;
            if (!File.Exists(path)) continue;

            inFolder.Add(NormPath(path));
            list.Add(MakeFileItem(path, external: true));
        }

        return list;
    }

    /// <summary>统一成完整路径（顺带把 / 和 \ 的差异抹平）。</summary>
    private static string NormPath(string path)
    {
        try { return Path.GetFullPath(path); }
        catch { return path; }
    }

    private static string NormSoundId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "";
        if (!id.StartsWith("file:", StringComparison.OrdinalIgnoreCase)) return id;
        return "file:" + NormPath(id.Substring(5));
    }

    private static SoundItem MakeFileItem(string path, bool external)
    {
        var full = NormPath(path);
        var name = Path.GetFileNameWithoutExtension(full);
        var suffix = external ? (L.Lang == "en" ? " (elsewhere)" : "（目录外）") : "";
        return new SoundItem
        {
            Id = "file:" + full,
            NameZh = "我的 · " + name + suffix,
            NameEn = "Mine · " + name + suffix
        };
    }

    private void FillSoundCombos()
    {
        var items = BuildSoundItems();

        foreach (var cb in new[] { SoundFinishCombo, SoundBreakCombo, SoundPreCombo })
        {
            var keep = SoundIdOf(cb);
            cb.ItemsSource = items;
            cb.DisplayMemberPath = nameof(SoundItem.Display);
            if (!string.IsNullOrEmpty(keep)) SelectSound(cb, keep);
        }

        SoundDirText.Text = ShortDir(SoundDirEffective());
        SoundDirText.ToolTip = SoundDirEffective();
        UpdateSoundDirText();
    }

    private void UpdateSoundDirText()
    {
        if (SoundDirText == null) return;

        var dir = SoundDirEffective();
        int count = SoundService.ScanFiles(dir).Count();
        SoundDirText.Text = $"{ShortDir(dir)} · {count}";
        SoundDirText.ToolTip = $"{dir}\n{L.T("lbl.soundDir")}: {count}";
    }

    /// <summary>路径太长时只显示最后两级，完整路径放在 tooltip 里。</summary>
    private static string ShortDir(string dir)
    {
        try
        {
            var full = dir.TrimEnd('\\', '/');
            if (full.Length <= 2 && full.EndsWith(":")) return full + "\\";   // 盘符根目录

            var leaf = Path.GetFileName(full);
            var parent = Path.GetFileName(Path.GetDirectoryName(full) ?? "");
            return string.IsNullOrEmpty(parent) ? full : Path.Combine(parent, leaf);
        }
        catch
        {
            return dir;
        }
    }

    private DispatcherTimer? _soundWatch;
    private HashSet<string> _knownSoundFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>盯着音效目录：有新文件进来就自动加进列表并选中，不用自己去找。</summary>
    private void StartSoundWatcher()
    {
        _knownSoundFiles = new HashSet<string>(SoundService.ScanFiles(SoundDirEffective()),
            StringComparer.OrdinalIgnoreCase);

        _soundWatch = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _soundWatch.Tick += (_, _) =>
        {
            if (SoundFinishCombo.IsDropDownOpen || SoundBreakCombo.IsDropDownOpen || SoundPreCombo.IsDropDownOpen)
                return;

            var now = SoundService.ScanFiles(SoundDirEffective()).ToList();
            var added = now.Where(f => !_knownSoundFiles.Contains(f)).ToList();
            bool removed = now.Count < _knownSoundFiles.Count;

            if (added.Count == 0 && !removed) return;

            _knownSoundFiles = new HashSet<string>(now, StringComparer.OrdinalIgnoreCase);
            RefreshSoundCombos();

            if (added.Count > 0)
            {
                // 自动选中新加的音效，并提示一下
                SelectSound(SoundFinishCombo, "file:" + added[0]);
                DropHint.Text = L.T("drop.ok", added.Count);
            }
        };

        _soundWatch.Start();
    }

    private string SoundSignature()
        => string.Join("|", SoundService.ScanFiles(SoundDirEffective()).Select(Path.GetFileName));

    /// <summary>重建音效下拉，并保留当前选择。</summary>
    private void RefreshSoundCombos()
    {
        var a = SoundIdOf(SoundFinishCombo);
        var b = SoundIdOf(SoundBreakCombo);
        var c = SoundIdOf(SoundPreCombo);

        if (!string.IsNullOrEmpty(a)) _work.SoundFinish = a;
        if (!string.IsNullOrEmpty(b)) _work.SoundBreakEnd = b;
        if (!string.IsNullOrEmpty(c)) _work.SoundPreAlert = c;

        FillSoundCombos();

        SelectSound(SoundFinishCombo, a);
        SelectSound(SoundBreakCombo, b);
        SelectSound(SoundPreCombo, c);
    }

    private static string SoundIdOf(ComboBox cb)
        => (cb.SelectedItem as SoundItem)?.Id ?? "";

    private static void SelectSound(ComboBox cb, string? id)
    {
        var target = NormSoundId(id);

        if (cb.ItemsSource is IEnumerable<SoundItem> items && !string.IsNullOrEmpty(target))
        {
            var hit = items.FirstOrDefault(x => NormSoundId(x.Id) == target);
            if (hit != null)
            {
                cb.SelectedItem = hit;
                return;
            }
        }
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
    }

    // ==================== 载入 / 读取 ====================

    private void LoadValues()
    {
        _loading = true;

        ThemeCombo.SelectedItem = ThemeService.Get(_work.Theme);
        FontSizeBox.Text = Math.Clamp((int)Math.Round(_work.FontSize), 18, 130).ToString();
        SelectFont(_work.FontFamily);
        OpacityBox.Text = ((int)Math.Round(Math.Clamp(_work.CardOpacity, 0, 1) * 100)).ToString();

        SelectTag(BgStyleCombo, string.IsNullOrWhiteSpace(_work.BackgroundStyle) ? "gradient" : _work.BackgroundStyle);
        UpdateColorSelection();
        UpdateBgControls();

        ShowCatCheck.IsChecked = _work.ShowCat;
        ShowProgressCheck.IsChecked = _work.ShowProgress;
        ShowStatusCheck.IsChecked = _work.ShowStatus;
        TopmostCheck.IsChecked = _work.Topmost;
        LockCheck.IsChecked = _work.LockPosition;

        int defSec = Math.Max(0, _work.DefaultSeconds);
        DefaultMinBox.Text = (defSec / 60).ToString();
        DefaultSecBox.Text = (defSec % 60).ToString();
        FlashCheck.IsChecked = _work.FlashOnFinish;
        LaunchStartCheck.IsChecked = _work.StartTimerOnLaunch;

        SetDuration(WorkHourBox, WorkMinBox, WorkSecBox, _work.PomodoroWorkSeconds);
        SetDuration(ShortHourBox, ShortMinBox, ShortSecBox, _work.PomodoroShortBreakSeconds);
        SetDuration(LongHourBox, LongMinBox, LongSecBox, _work.PomodoroLongBreakSeconds);
        RoundsBox.Text = Math.Clamp(_work.PomodoroRoundsBeforeLong, 1, 20).ToString();
        AutoNextCheck.IsChecked = _work.PomodoroAutoNext;

        SetDuration(FocusHourBox, FocusMinBox, FocusSecBox, _work.CycleFocusSeconds);
        SetDuration(BreakHourBox, BreakMinBox, BreakSecBox, _work.CycleBreakSeconds);
        CycleRoundsBox.Text = Math.Clamp(_work.CycleRounds, 0, 99).ToString();
        CycleAutoCheck.IsChecked = _work.CycleAutoNext;

        SoundCheck.IsChecked = _work.SoundEnabled;
        FillSoundCombos();
        SelectSound(SoundFinishCombo, string.IsNullOrWhiteSpace(_work.SoundFinish) ? "builtin:meow" : _work.SoundFinish);
        SelectSound(SoundBreakCombo, string.IsNullOrWhiteSpace(_work.SoundBreakEnd) ? "builtin:soft" : _work.SoundBreakEnd);
        SelectSound(SoundPreCombo, string.IsNullOrWhiteSpace(_work.SoundPreAlert) ? "builtin:notify" : _work.SoundPreAlert);
        VolumeBox.Text = Math.Clamp(_work.SoundVolume, 0, 100).ToString();
        NotifyCheck.IsChecked = _work.NotifyEnabled;
        PreAlertCheck.IsChecked = _work.PreAlertEnabled;
        PreSecBox.Text = Math.Clamp(_work.PreAlertSeconds, 1, 600).ToString();

        SelectTag(LangCombo, _work.Language);
        SelectTag(TrayModeCombo, _work.TrayMode);
        AutoStartCheck.IsChecked = _work.AutoStart || StartupService.IsEnabled();
        StartMinCheck.IsChecked = _work.StartMinimized;

        HkPause.Text = _work.HotkeyTogglePause;
        HkReset.Text = _work.HotkeyReset;
        HkTimer.Text = _work.HotkeySetTimer;
        HkClock.Text = _work.HotkeyClock;
        HkShow.Text = _work.HotkeyShowHide;
        HkTop.Text = _work.HotkeyTopmost;
        HkPomo.Text = _work.HotkeyPomodoro;
        HkCycle.Text = _work.HotkeyCycle;

        UpdateThemePreview();
        UpdatePreview();

        _loading = false;
    }

    /// <summary>选中字体；配置里的字体不在列表里就临时插到最前面。</summary>
    private void SelectFont(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) name = "Microsoft YaHei UI";

        if (FontCombo.ItemsSource is List<string> fonts)
        {
            if (!fonts.Contains(name)) fonts.Insert(0, name);
            FontCombo.SelectedItem = name;
        }
    }

    // ==================== 数值输入辅助 ====================

    private static void SetDuration(TextBox hourBox, TextBox minBox, TextBox secBox, int totalSeconds)
    {
        totalSeconds = Math.Max(0, totalSeconds);
        hourBox.Text = (totalSeconds / 3600).ToString();
        minBox.Text = (totalSeconds % 3600 / 60).ToString();
        secBox.Text = (totalSeconds % 60).ToString();
    }

    private static int ReadDuration(TextBox hourBox, TextBox minBox, TextBox secBox)
    {
        int h = int.TryParse(hourBox.Text, out var hh) ? Math.Clamp(hh, 0, 99) : 0;
        int m = int.TryParse(minBox.Text, out var mm) ? Math.Clamp(mm, 0, 59) : 0;
        int s = int.TryParse(secBox.Text, out var ss) ? Math.Clamp(ss, 0, 59) : 0;
        return h * 3600 + m * 60 + s;
    }

    private static int ReadInt(TextBox box, int min, int max, int fallback)
    {
        if (!int.TryParse(box.Text, out var v)) return fallback;
        return Math.Clamp(v, min, max);
    }

    private void ReadValues()
    {
        if (ThemeCombo.SelectedItem is Theme t) _work.Theme = t.Id;
        _work.FontSize = ReadInt(FontSizeBox, 18, 130, 46);
        _work.FontFamily = FontCombo.SelectedItem as string ?? "Microsoft YaHei UI";
        _work.CardOpacity = ReadInt(OpacityBox, 0, 100, 62) / 100.0;
        _work.BackgroundStyle = TagOf(BgStyleCombo) ?? "gradient";

        _work.ShowCat = ShowCatCheck.IsChecked == true;
        _work.ShowProgress = ShowProgressCheck.IsChecked == true;
        _work.ShowStatus = ShowStatusCheck.IsChecked == true;
        _work.Topmost = TopmostCheck.IsChecked == true;
        _work.LockPosition = LockCheck.IsChecked == true;

        _work.DefaultSeconds = ReadDuration(DefaultHourBox, DefaultMinBox, DefaultSecBox);
        _work.FlashOnFinish = FlashCheck.IsChecked == true;
        _work.StartTimerOnLaunch = LaunchStartCheck.IsChecked == true;

        _work.PomodoroWorkSeconds = Math.Max(1, ReadDuration(WorkHourBox, WorkMinBox, WorkSecBox));
        _work.PomodoroShortBreakSeconds = Math.Max(1, ReadDuration(ShortHourBox, ShortMinBox, ShortSecBox));
        _work.PomodoroLongBreakSeconds = Math.Max(1, ReadDuration(LongHourBox, LongMinBox, LongSecBox));
        _work.PomodoroRoundsBeforeLong = ReadInt(RoundsBox, 1, 20, 4);
        _work.PomodoroAutoNext = AutoNextCheck.IsChecked == true;

        _work.CycleFocusSeconds = Math.Max(1, ReadDuration(FocusHourBox, FocusMinBox, FocusSecBox));
        _work.CycleBreakSeconds = Math.Max(1, ReadDuration(BreakHourBox, BreakMinBox, BreakSecBox));
        _work.CycleRounds = ReadInt(CycleRoundsBox, 0, 99, 0);
        _work.CycleAutoNext = CycleAutoCheck.IsChecked == true;

        _work.SoundEnabled = SoundCheck.IsChecked == true;
        _work.SoundFinish = Fallback(SoundIdOf(SoundFinishCombo), "builtin:meow");
        _work.SoundBreakEnd = Fallback(SoundIdOf(SoundBreakCombo), "builtin:soft");
        _work.SoundPreAlert = Fallback(SoundIdOf(SoundPreCombo), "builtin:notify");
        _work.SoundVolume = ReadInt(VolumeBox, 0, 100, 80);
        _work.NotifyEnabled = NotifyCheck.IsChecked == true;
        _work.PreAlertEnabled = PreAlertCheck.IsChecked == true;
        _work.PreAlertSeconds = ReadInt(PreSecBox, 1, 600, 60);

        _work.Language = TagOf(LangCombo) ?? "zh";
        _work.TrayMode = TagOf(TrayModeCombo) ?? "cat";
        _work.AutoStart = AutoStartCheck.IsChecked == true;
        _work.StartMinimized = StartMinCheck.IsChecked == true;

        _work.HotkeyTogglePause = Clean(HkPause.Text);
        _work.HotkeyReset = Clean(HkReset.Text);
        _work.HotkeySetTimer = Clean(HkTimer.Text);
        _work.HotkeyClock = Clean(HkClock.Text);
        _work.HotkeyShowHide = Clean(HkShow.Text);
        _work.HotkeyTopmost = Clean(HkTop.Text);
        _work.HotkeyPomodoro = Clean(HkPomo.Text);
        _work.HotkeyCycle = Clean(HkCycle.Text);
    }

    private static string Fallback(string? value, string def)
        => string.IsNullOrWhiteSpace(value) ? def : value;

    private static string Clean(string? s) => (s ?? "").Trim();

    private static double Clamp(double v, double min, double max)
    {
        if (double.IsNaN(v)) return min;
        return Math.Min(max, Math.Max(min, v));
    }

    private static string? TagOf(ComboBox cb) => (cb.SelectedItem as ComboBoxItem)?.Tag as string;

    private static void SelectTag(ComboBox cb, string? tag)
    {
        foreach (var item in cb.Items)
        {
            if (item is ComboBoxItem ci && (ci.Tag as string) == tag)
            {
                cb.SelectedItem = ci;
                return;
            }
        }
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
    }

    // ==================== 事件 ====================

    // ==================== 设置面板跟随主题配色 ====================

    private void ApplyThemeResources(Theme t)
    {
        UiTheme.Apply(t);

        if (WinShadow != null) WinShadow.Color = t.Shadow;
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _loading) return;
        BuildPalette();          // "跟随主题"那个圆点要跟着新主题变色
        UpdateThemePreview();
        RequestPreview();
    }

    private void OnFontChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _loading) return;
        RequestPreview();
    }

    private void OnAppearanceChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized || _loading) return;
        RequestPreview();
    }

    private void OnNumberOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    // ==================== 实时预览 ====================

    /// <summary>改动任何外观参数时，既刷新缩略预览，也让主窗口立刻跟着变。</summary>
    private void RequestPreview()
    {
        if (!_initialized || _loading) return;

        if (_previewTimer == null)
        {
            _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) };
            _previewTimer.Tick += (_, _) =>
            {
                _previewTimer!.Stop();
                DoPreview();
            };
        }

        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void DoPreview()
    {
        if (!_initialized) return;

        ReadValues();
        UpdatePreview();

        ApplyThemeResources(ThemeService.Get(_work.Theme));

        ConfigService.CopyInto(_work, _cfg);
        App.MainWin?.ApplyConfig();
    }

    private static FontFamily SafeFont(string? name)
    {
        try { return new FontFamily(string.IsNullOrWhiteSpace(name) ? "Microsoft YaHei UI" : name); }
        catch { return new FontFamily("Microsoft YaHei UI"); }
    }

    private void UpdatePreview()
    {
        if (!_initialized || PreviewCard == null) return;

        var t = ThemeService.Get(_work.Theme);
        bool noBg = _work.BackgroundStyle == "none" || _work.CardOpacity <= 0.001;

        PreviewCard.Background = ThemeService.BuildBackground(_work, t);
        PreviewShadow.Color = t.From;
        PreviewShadow.Opacity = noBg ? 0 : 0.3;

        PreviewTime.Foreground = new SolidColorBrush(string.IsNullOrWhiteSpace(_work.TextColor)
            ? (noBg ? t.From : t.Text)
            : ThemeService.C(_work.TextColor));
        PreviewTime.FontFamily = SafeFont(_work.FontFamily);
        PreviewTime.FontSize = Math.Clamp(_work.FontSize * 0.44, 9, 46);

        PreviewCat.Visibility = _work.ShowCat ? Visibility.Visible : Visibility.Collapsed;
        PreviewBar.Visibility = _work.ShowProgress ? Visibility.Visible : Visibility.Collapsed;

        // 无背景时白猫会融进浅色底，加一层描边阴影
        PreviewCat.Effect = noBg
            ? new DropShadowEffect
            {
                Color = t.Shadow,
                BlurRadius = 6,
                ShadowDepth = 0,
                Opacity = 0.7
            }
            : null;

        if (noBg)
        {
            PreviewTime.Effect = new DropShadowEffect
            {
                Color = t.Shadow,
                BlurRadius = 6,
                ShadowDepth = 0,
                Opacity = 0.85
            };
            PreviewBar.Background = new SolidColorBrush(ThemeService.WithAlpha(Colors.Black, 45));
            PreviewBarFill.Background = new SolidColorBrush(t.From);
        }
        else
        {
            PreviewTime.Effect = null;
            PreviewBar.Background = new SolidColorBrush(ThemeService.WithAlpha(t.Text, 58));
            PreviewBarFill.Background = new SolidColorBrush(ThemeService.WithAlpha(t.Text, 235));
        }
    }

    private void UpdateThemePreview()
    {
        if (ThemeCombo.SelectedItem is not Theme t) return;

        if ((TagOf(BgStyleCombo) ?? "gradient") == "solid")
        {
            var hex = string.IsNullOrWhiteSpace(_work.BackgroundColor) ? t.From.ToString() : _work.BackgroundColor;
            var c = ThemeService.C(hex);
            PrevFrom.Color = c;
            PrevTo.Color = c;
        }
        else
        {
            PrevFrom.Color = t.From;
            PrevTo.Color = t.To;
        }
    }

    /// <summary>字号 / 透明度这类输入框一改就实时预览。</summary>
    private void OnNumberBoxChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized || _loading) return;
        RequestPreview();
    }

    // ==================== 音效 ====================

    private void OnOpenSoundDir(object sender, RoutedEventArgs e)
    {
        var dir = SoundDirEffective();
        try
        {
            Directory.CreateDirectory(dir);

            bool isDefault = string.Equals(dir.TrimEnd('\\'), SoundService.DefaultDir.TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
            if (isDefault) SoundService.EnsureSampleFiles();

            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        }
        catch
        {
            // 资源管理器打不开就算了
        }

        Dispatcher.BeginInvoke(new Action(RefreshSoundCombos), DispatcherPriority.Background);
    }

    /// <summary>选择自己的音效文件夹。</summary>
    private void OnChangeSoundDir(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = L.T("lbl.soundDir"),
                Multiselect = false
            };

            var current = SoundDirEffective();
            if (Directory.Exists(current)) dlg.InitialDirectory = current;

            if (dlg.ShowDialog(this) != true) return;

            _work.SoundDir = dlg.FolderName;
            RefreshSoundCombos();
        }
        catch
        {
            // 某些系统上文件夹选择器不可用，忽略
        }
    }

    private void OnResetSoundDir(object sender, RoutedEventArgs e)
    {
        _work.SoundDir = "";
        SoundService.EnsureSampleFiles();
        RefreshSoundCombos();
    }

    // ==================== 拖拽添加音效 ====================

    private static Color ResColor(string key, Color fallback)
        => Application.Current?.Resources[key] is SolidColorBrush b ? b.Color : fallback;

    private void ResetDropZone()
    {
        DropZone.BorderBrush = new SolidColorBrush(ResColor("LineBrush", ThemeService.C("#EFDCE6")));
        DropZone.Background = new SolidColorBrush(ResColor("CardBrush", Colors.White));
        DropHint.Foreground = new SolidColorBrush(ResColor("SubTextBrush", ThemeService.C("#B08C9C")));
        DropHint.Text = L.T("drop.hint");
    }

    private void OnWindowDragOver(object sender, DragEventArgs e)
    {
        bool hasFiles = e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = hasFiles ? DragDropEffects.Copy : DragDropEffects.None;

        var accent = ResColor("AccentBrush", ThemeService.C("#F2558B"));

        DropZone.BorderBrush = new SolidColorBrush(hasFiles ? accent : ResColor("LineBrush", ThemeService.C("#EFDCE6")));
        DropZone.Background = new SolidColorBrush(hasFiles ? ResColor("AccentSoftBrush", Colors.White) : ResColor("CardBrush", Colors.White));
        DropHint.Foreground = new SolidColorBrush(hasFiles ? accent : ResColor("SubTextBrush", ThemeService.C("#B08C9C")));

        e.Handled = true;
    }

    private void OnWindowDragLeave(object sender, DragEventArgs e)
    {
        ResetDropZone();
    }

    private void OnWindowDrop(object sender, DragEventArgs e)
    {
        ResetDropZone();
        e.Handled = true;

        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (dropped == null || dropped.Length == 0) return;

        var dir = SoundDirEffective();
        try { Directory.CreateDirectory(dir); }
        catch
        {
            DropHint.Text = L.T("drop.fail");
            return;
        }

        int added = 0;
        string? last = null;

        foreach (var item in dropped)
        {
            foreach (var file in ExpandToAudio(item))
            {
                try
                {
                    var target = Path.Combine(dir, Path.GetFileName(file));

                    if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(target),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        // 本来就在音效目录里
                        last = target;
                        added++;
                        continue;
                    }

                    if (File.Exists(target)) target = UniquePath(target);
                    File.Copy(file, target);
                    last = target;
                    added++;
                }
                catch
                {
                    // 单个文件失败就跳过
                }
            }
        }

        RefreshSoundCombos();
        _knownSoundFiles = new HashSet<string>(SoundService.ScanFiles(SoundDirEffective()),
            StringComparer.OrdinalIgnoreCase);

        if (last != null)
        {
            // 自动选上并试听，用户马上知道加成功了
            SelectSound(SoundFinishCombo, "file:" + last);
            Preview(SoundFinishCombo);
        }

        DropHint.Text = added > 0
            ? L.T("drop.ok", added)
            : L.T("drop.fail");
    }

    private static IEnumerable<string> ExpandToAudio(string path)
    {
        if (Directory.Exists(path))
        {
            IEnumerable<string> inner;
            try { inner = Directory.EnumerateFiles(path).ToList(); }
            catch { yield break; }

            foreach (var f in inner)
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                if (SoundService.AllowedExtensions.Contains(ext)) yield return f;
            }
            yield break;
        }

        if (File.Exists(path))
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (SoundService.AllowedExtensions.Contains(ext)) yield return path;
        }
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);

        for (int i = 1; i < 1000; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
        return path;
    }

    private void OnPreviewFinish(object sender, RoutedEventArgs e) => Preview(SoundFinishCombo);
    private void OnPreviewBreak(object sender, RoutedEventArgs e) => Preview(SoundBreakCombo);
    private void OnPreviewPre(object sender, RoutedEventArgs e) => Preview(SoundPreCombo);

    private void Preview(ComboBox cb)
    {
        var id = SoundIdOf(cb);
        if (string.IsNullOrEmpty(id)) return;
        SoundService.Play(id, ReadInt(VolumeBox, 0, 100, 80));
    }

    // ==================== 保存 ====================

    private void OnRestoreDefaults(object sender, RoutedEventArgs e)
    {
        var lang = _work.Language;
        _work = new AppConfig { Language = lang };
        LoadValues();
        RequestPreview();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        ReadValues();
        ConfigService.CopyInto(_work, _cfg);
        L.Lang = _cfg.Language;

        App.SaveConfig();
        StartupService.Set(_cfg.AutoStart);
        App.ReloadHotkeys();
        App.RefreshTray();

        var failed = App.Hotkeys?.LastFailedKeys ?? new List<string>();
        if (failed.Count > 0)
        {
            MessageBox.Show(this,
                (L.Lang == "en"
                    ? "These hotkeys are already taken by another program:\n\n"
                    : "下面这些快捷键被其他程序占用了，换一组试试：\n\n")
                + string.Join("\n", failed),
                L.T("menu.settings").TrimEnd('…', '.'),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    protected override void OnClosed(EventArgs e)
    {
        _soundWatch?.Stop();
        _previewTimer?.Stop();

        // 没点保存就退出：把实时预览改过的设置全部还原
        if (DialogResult != true)
        {
            ConfigService.CopyInto(_original, _cfg);
            App.MainWin?.ApplyConfig();
            App.SaveConfig();
        }

        base.OnClosed(e);
    }

    /// <summary>供截图/自动化使用。</summary>
    public void ScrollTo(double offset) => ContentScroll.ScrollToVerticalOffset(offset);

    private void OnDragArea(object sender, MouseButtonEventArgs e)
    {
        try { DragMove(); } catch { }
    }
}
