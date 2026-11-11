using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MeowTimer.Models;
using MeowTimer.Services;
using MeowTimerMac.Services;

namespace MeowTimerMac.Views;

public partial class MainWindow : Window
{
    private enum Phase { Idle, Running, Paused }

    private readonly DispatcherTimer _ticker;
    private Phase _phase = Phase.Idle;
    private TimerMode _mode = TimerMode.Countdown;

    private int _total;
    private int _remain;
    private DateTime _lastTick;

    private Point _pressPoint;
    private bool _pressed;
    private PointerPressedEventArgs? _pressArgs;

    private PomodoroPhase _pomoPhase = PomodoroPhase.Work;
    private CyclePhase _cyclePhase = CyclePhase.Focus;
    private int _pomoDone;
    private int _cycleRound;

    public MainWindow()
    {
        InitializeComponent();

        _total = Math.Max(1, App.Config.DefaultSeconds);
        _remain = _total;

        _ticker = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _ticker.Tick += OnTick;

        Opened += (_, _) =>
        {
            ApplyTheme();
            PlaceWindow();
            Refresh();
        };

        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel);
        PointerWheelChanged += OnWheel;

        Closing += (_, e) =>
        {
            // 关窗口只是收起来，托盘还在
            if (!_quitting)
            {
                e.Cancel = true;
                Hide();
            }
        };

        SizeChanged += (_, _) => UpdateProgressBar();
        UpdateContextMenu();
    }

    private bool _quitting;

    // ==================== 外观 ====================

    public void ApplyTheme()
    {
        var cfg = App.Config;
        var t = ThemeService.Get(cfg.Theme);

        bool noBg = cfg.BackgroundStyle == "none" || cfg.CardOpacity <= 0.001;

        Card.Background = ThemeService.BuildBackground(cfg, t);
        Card.BoxShadow = noBg
            ? default
            : new BoxShadows(new BoxShadow
            {
                Blur = 24,
                OffsetY = 7,
                Color = ThemeService.WithAlpha(t.Shadow, 56)
            });

        var textColor = ThemeService.Parse(cfg.TextColor, noBg ? t.From : t.Text);

        TimeText.Foreground = new SolidColorBrush(textColor);
        TimeText.FontSize = Math.Clamp(cfg.FontSize, 18, 130);

        StatusText.Foreground = new SolidColorBrush(noBg ? textColor : t.SubText);
        StatusText.IsVisible = cfg.ShowStatus;

        CatBox.IsVisible = cfg.ShowCat;
        CatBox.Width = CatBox.Height = Math.Max(20, cfg.FontSize * 0.95);

        BarBg.IsVisible = cfg.ShowProgress;
        BarFill.Background = new SolidColorBrush(ThemeService.WithAlpha(textColor, 240));
        BarBg.Background = new SolidColorBrush(noBg
            ? ThemeService.WithAlpha(ThemeService.Luminance(textColor) > 0.62 ? t.Shadow : Colors.White, 45)
            : ThemeService.WithAlpha(textColor, 60));

        EyeLeft.Fill = EyeRight.Fill = new SolidColorBrush(Color.Parse("#5C4A56"));
        CheekLeft.Fill = CheekRight.Fill = new SolidColorBrush(ThemeService.WithAlpha(t.From, 130));

        Topmost = cfg.Topmost;
        UpdateProgressBar();
    }

    private void PlaceWindow()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen == null) return;

        var area = screen.WorkingArea;
        bool known = !double.IsNaN(App.Config.WindowLeft) && !double.IsNaN(App.Config.WindowTop);

        double left = known ? App.Config.WindowLeft : area.Right - Width - 40;
        double top = known ? App.Config.WindowTop : area.Y + 40;

        // 只有左上角真的不在任何一块屏幕里（换过显示器 / 改过分辨率）才回默认位置
        bool onScreen = Screens.All.Any(s =>
            s.Bounds.Contains(new PixelPoint((int)left + 24, (int)top + 24)));

        if (!onScreen)
        {
            left = area.Right - Width - 40;
            top = area.Y + 40;
        }

        Position = new PixelPoint((int)Math.Round(left), (int)Math.Round(top));
    }

    // ==================== 计时 ====================

    private void OnTick(object? sender, EventArgs e)
    {
        if (_phase != Phase.Running) return;

        var now = DateTime.Now;
        var elapsed = (now - _lastTick).TotalSeconds;
        _lastTick = now;

        _remain = Math.Max(0, _remain - (int)Math.Round(elapsed));

        if (_remain <= 0)
        {
            _remain = 0;
            _ticker.Stop();
            HandleFinish();
            return;
        }

        Refresh();
    }

    public void Toggle()
    {
        switch (_phase)
        {
            case Phase.Idle:
                if (_remain <= 0) _remain = _total;
                _phase = Phase.Running;
                _lastTick = DateTime.Now;
                _ticker.Start();
                break;

            case Phase.Running:
                _phase = Phase.Paused;
                _ticker.Stop();
                break;

            case Phase.Paused:
                _phase = Phase.Running;
                _lastTick = DateTime.Now;
                _ticker.Start();
                break;
        }

        Refresh();
    }

    private void HandleFinish()
    {
        var cfg = App.Config;

        switch (_mode)
        {
            case TimerMode.Pomodoro:
            {
                bool isWork = _pomoPhase == PomodoroPhase.Work;

                if (isWork)
                {
                    _pomoDone++;
                    if (cfg.SoundEnabled) SoundService.Play(cfg.SoundFinish, cfg.SoundVolume);
                    bool longBreak = _pomoDone % Math.Max(1, cfg.PomodoroRoundsBeforeLong) == 0;
                    BeginPhase(longBreak ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak, cfg.PomodoroAutoNext);
                }
                else
                {
                    if (cfg.SoundEnabled) SoundService.Play(cfg.SoundBreakEnd, cfg.SoundVolume);
                    BeginPhase(PomodoroPhase.Work, cfg.PomodoroAutoNext);
                }
                break;
            }

            case TimerMode.Cycle:
            {
                if (_cyclePhase == CyclePhase.Focus)
                {
                    _cycleRound++;
                    if (cfg.SoundEnabled) SoundService.Play(cfg.SoundFinish, cfg.SoundVolume);
                    BeginCyclePhase(CyclePhase.Break, cfg.CycleAutoNext);
                }
                else
                {
                    if (cfg.SoundEnabled) SoundService.Play(cfg.SoundBreakEnd, cfg.SoundVolume);

                    if (cfg.CycleRounds > 0 && _cycleRound >= cfg.CycleRounds)
                    {
                        _phase = Phase.Idle;
                        _cycleRound = 0;
                        _cyclePhase = CyclePhase.Focus;
                        _total = Math.Max(1, cfg.CycleFocusSeconds);
                        _remain = _total;
                    }
                    else
                    {
                        BeginCyclePhase(CyclePhase.Focus, cfg.CycleAutoNext);
                    }
                }
                break;
            }

            default:
            {
                if (cfg.SoundEnabled) SoundService.Play(cfg.SoundFinish, cfg.SoundVolume);
                _phase = Phase.Idle;
                break;
            }
        }

        Refresh();
    }

    private void BeginPhase(PomodoroPhase phase, bool autoStart)
    {
        _pomoPhase = phase;
        _total = Math.Max(1, phase switch
        {
            PomodoroPhase.Work => App.Config.PomodoroWorkSeconds,
            PomodoroPhase.ShortBreak => App.Config.PomodoroShortBreakSeconds,
            _ => App.Config.PomodoroLongBreakSeconds
        });
        _remain = _total;

        _phase = autoStart ? Phase.Running : Phase.Idle;
        if (autoStart) _lastTick = DateTime.Now;

        Refresh();
    }

    private void BeginCyclePhase(CyclePhase phase, bool autoStart)
    {
        _cyclePhase = phase;
        _total = Math.Max(1, phase == CyclePhase.Focus
            ? App.Config.CycleFocusSeconds
            : App.Config.CycleBreakSeconds);
        _remain = _total;

        _phase = autoStart ? Phase.Running : Phase.Idle;
        if (autoStart) _lastTick = DateTime.Now;

        Refresh();
    }

    public void Reset()
    {
        _ticker.Stop();
        _phase = Phase.Idle;

        switch (_mode)
        {
            case TimerMode.Pomodoro:
                _pomoDone = 0;
                BeginPhase(PomodoroPhase.Work, autoStart: false);
                return;
            case TimerMode.Cycle:
                _cycleRound = 0;
                BeginCyclePhase(CyclePhase.Focus, autoStart: false);
                return;
        }

        _total = Math.Max(1, App.Config.DefaultSeconds);
        _remain = _total;
        Refresh();
    }

    private void SetMode(TimerMode mode)
    {
        if (_mode == mode) return;

        _mode = mode;
        _ticker.Stop();
        _phase = Phase.Idle;

        switch (mode)
        {
            case TimerMode.Pomodoro:
                _pomoDone = 0;
                BeginPhase(PomodoroPhase.Work, autoStart: false);
                break;
            case TimerMode.Cycle:
                _cycleRound = 0;
                BeginCyclePhase(CyclePhase.Focus, autoStart: false);
                break;
            default:
                _total = Math.Max(1, App.Config.DefaultSeconds);
                _remain = _total;
                Refresh();
                break;
        }

        UpdateContextMenu();
    }

    private void SetTotal(int seconds)
    {
        _ticker.Stop();
        _phase = Phase.Idle;
        _mode = TimerMode.Countdown;
        _total = Math.Max(1, seconds);
        _remain = _total;
        UpdateContextMenu();
        Refresh();
    }

    private void Adjust(int delta)
    {
        if (_phase == Phase.Running) return;

        _total = Math.Max(1, _total + delta);
        _remain = _total;
        _phase = Phase.Idle;
        Refresh();
    }

    // ==================== 刷新 ====================

    private void Refresh()
    {
        TimeText.Text = Format(_remain);
        StatusText.Text = StatusLine();
        UpdateProgressBar();
        UpdateTooltip();
    }

    private void UpdateTooltip()
    {
        if (_phase == Phase.Idle) return;
        TrayService.UpdateToolTip($"{StatusLine()} · {Format(_remain)}");
    }

    private string StatusLine()
    {
        string idle = "点一下开始";
        string running = "专注中 · 点一下暂停";
        string paused = "已暂停 · 点一下继续";

        switch (_mode)
        {
            case TimerMode.Pomodoro:
            {
                string name = _pomoPhase switch
                {
                    PomodoroPhase.Work => $"专注 #{_pomoDone + 1}",
                    PomodoroPhase.ShortBreak => "短休息",
                    _ => "长休息"
                };

                return _phase switch
                {
                    Phase.Running => $"{name} · 点一下暂停",
                    Phase.Paused => $"{name} · 已暂停",
                    _ => $"{name} · {idle}"
                };
            }

            case TimerMode.Cycle:
            {
                string name = _cyclePhase == CyclePhase.Focus
                    ? $"专注 第 {_cycleRound + 1} 轮"
                    : "休息";

                return _phase switch
                {
                    Phase.Running => $"{name} · 点一下暂停",
                    Phase.Paused => $"{name} · 已暂停",
                    _ => $"{name} · {idle}"
                };
            }

            default:
                return _phase switch
                {
                    Phase.Running => running,
                    Phase.Paused => paused,
                    _ => idle
                };
        }
    }

    private void UpdateProgressBar()
    {
        var width = BarBg.Bounds.Width;
        if (width <= 0) return;

        double ratio = _total <= 0 ? 0 : (double)_remain / _total;
        BarFill.Width = Math.Max(0, Math.Min(width, width * ratio));
    }

    private static string Format(int totalSeconds)
    {
        totalSeconds = Math.Max(0, totalSeconds);
        int h = totalSeconds / 3600;
        int m = totalSeconds % 3600 / 60;
        int s = totalSeconds % 60;
        return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }

    // ==================== 交互 ====================

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsRightButtonPressed)
        {
            UpdateContextMenu();
            return;
        }

        _pressPoint = point.Position;
        _pressed = true;
        _pressArgs = e;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!_pressed) return;

        var p = e.GetPosition(this);

        if (Math.Abs(p.X - _pressPoint.X) > 6 || Math.Abs(p.Y - _pressPoint.Y) > 6)
        {
            _pressed = false;

            if (App.Config.LockPosition) return;

            try
            {
                if (_pressArgs != null) BeginMoveDrag(_pressArgs);
            }
            catch { }

            App.Config.WindowLeft = Position.X;
            App.Config.WindowTop = Position.Y;
            App.Save();
        }
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_pressed) return;
        _pressed = false;

        if (e.InitialPressMouseButton == MouseButton.Left && !App.Config.LockPosition)
            Toggle();
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (App.Config.LockPosition) return;
        if (_mode != TimerMode.Countdown) return;

        Adjust(e.Delta.Y > 0 ? 60 : -60);
        e.Handled = true;
    }

    // ==================== 右键菜单 ====================

    private void UpdateContextMenu()
    {
        var cfg = App.Config;
        var menu = new ContextMenu();

        menu.Items.Add(Item(_phase == Phase.Running ? "暂停" : "开始", (_, _) => Toggle()));
        menu.Items.Add(Item("重置", (_, _) => Reset()));
        menu.Items.Add(new Separator());

        menu.Items.Add(Item((_mode == TimerMode.Countdown ? "✓ " : "   ") + "倒计时", (_, _) => SetMode(TimerMode.Countdown)));
        menu.Items.Add(Item((_mode == TimerMode.Pomodoro ? "✓ " : "   ") + "番茄钟", (_, _) => SetMode(TimerMode.Pomodoro)));
        menu.Items.Add(Item((_mode == TimerMode.Cycle ? "✓ " : "   ") + "专注 · 休息", (_, _) => SetMode(TimerMode.Cycle)));

        menu.Items.Add(new Separator());
        menu.Items.Add(Item("设置倒计时…", (_, _) => OpenSetTimer()));
        menu.Items.Add(Item("设置…", (_, _) => OpenSettings()));

        menu.Items.Add(new Separator());
        menu.Items.Add(Item((cfg.Topmost ? "✓ " : "   ") + "窗口置顶", (_, _) =>
        {
            cfg.Topmost = !cfg.Topmost;
            Topmost = cfg.Topmost;
            App.Save();
            UpdateContextMenu();
        }));
        menu.Items.Add(Item((cfg.LockPosition ? "✓ " : "   ") + "锁定位置", (_, _) =>
        {
            cfg.LockPosition = !cfg.LockPosition;
            App.Save();
            UpdateContextMenu();
        }));

        menu.Items.Add(new Separator());
        menu.Items.Add(Item("隐藏到托盘", (_, _) => Hide()));
        menu.Items.Add(Item("退出喵时", (_, _) => Quit()));

        ContextMenu = menu;
    }

    private static MenuItem Item(string header, EventHandler<RoutedEventArgs> handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        return item;
    }

    private async void OpenSetTimer()
    {
        var dlg = new SetTimerWindow(_total);
        if (await dlg.ShowDialog<bool>(this)) SetTotal(dlg.Seconds);
    }

    private async void OpenSettings()
    {
        var dlg = new SettingsWindow();
        await dlg.ShowDialog(this);
        ApplyTheme();
        Refresh();
    }

    public void Quit()
    {
        _quitting = true;

        App.Config.WindowLeft = Position.X;
        App.Config.WindowTop = Position.Y;
        App.Save();

        if (Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
