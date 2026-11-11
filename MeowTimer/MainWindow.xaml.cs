using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using MeowTimer.Models;
using MeowTimer.Services;
using MeowTimer.Views;

namespace MeowTimer;

public partial class MainWindow : Window
{
    private readonly AppConfig _cfg;
    private readonly DispatcherTimer _tick;
    private readonly DispatcherTimer _saveTimer;
    private readonly Stopwatch _sw = new();

    private double _baseRemaining;   // 本轮启动瞬间的剩余秒
    private double _baseElapsed;     // 正计时累计秒
    private double _totalSeconds;    // 当前阶段总秒
    private TimerMode _mode = TimerMode.Countdown;
    private PomodoroPhase _phase = PomodoroPhase.Work;
    private CyclePhase _cyclePhase = CyclePhase.Focus;
    private int _cycleRound;
    private int _pomodoroDone;
    private bool _running;
    private bool _ready;
    private bool _suppressPosSave;
    private bool _preAlerted;
    private bool _editMode;

    private Point _dragStart;
    private bool _pressed;
    private bool _dragging;

    public event Action<string, string>? NotifyRequested;
    public event Action? TimerStateChanged;

    public bool IsRunning => _running;
    public TimerMode Mode => _mode;
    public bool IsEditMode => _editMode;

    /// <summary>倒计时 / 番茄 / 专注-休息循环：这三种都是"从某个时长往下数"。</summary>
    private bool IsCountdownLike => _mode is TimerMode.Countdown or TimerMode.Pomodoro or TimerMode.Cycle;
    public AppConfig Config => _cfg;

    public MainWindow(AppConfig cfg)
    {
        _cfg = cfg;
        InitializeComponent();

        _tick = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(100) };
        _tick.Tick += (_, _) => Refresh();

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SavePosition();
        };

        Loaded += OnLoaded;
        ContextMenu = new ContextMenu();
        ContextMenu.Opened += (_, _) => RebuildMenu(ContextMenu);
        RebuildMenu(ContextMenu);
    }

    // ==================== 初始化 ====================

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_ready) return;
        _ready = true;

        ApplyConfig();
        ApplyTheme();
        RestorePosition();

        _totalSeconds = Math.Max(1, _cfg.DefaultSeconds);
        _baseRemaining = _totalSeconds;
        SetMode(TimerMode.Countdown, _totalSeconds, start: false);

        StartBreathing();
        _tick.Start();

        if (_cfg.StartTimerOnLaunch)
        {
            StartRunning();
            Refresh();
        }

        // 轻轻淡入一下，免得窗口悄悄出现没人注意到
        Opacity = 0;
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fade);
    }

    public void ApplyConfig()
    {
        Topmost = _cfg.Topmost;
        TimeText.FontFamily = SafeFont(_cfg.FontFamily);
        TimeText.FontSize = Math.Clamp(_cfg.FontSize, 18, 130);
        TimeText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        TimeText.LineHeight = Math.Round(TimeText.FontSize * 1.12);
        CatHost.Visibility = _cfg.ShowCat ? Visibility.Visible : Visibility.Collapsed;
        CatHost.Width = Math.Clamp(_cfg.FontSize * 1.0, 20, 120);
        CatHost.Height = Math.Clamp(_cfg.FontSize * 1.0, 20, 120);
        StatusText.Visibility = _cfg.ShowStatus ? Visibility.Visible : Visibility.Collapsed;
        ProgressBg.Visibility = _cfg.ShowProgress ? Visibility.Visible : Visibility.Collapsed;
        ApplyTheme();
        FixTimeWidth();
        Refresh();
    }

    private static FontFamily SafeFont(string name)
    {
        try { return new FontFamily(string.IsNullOrWhiteSpace(name) ? "Microsoft YaHei UI" : name); }
        catch { return new FontFamily("Microsoft YaHei UI"); }
    }

    private void FixTimeWidth()
    {
        try
        {
            var tf = new Typeface(TimeText.FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var ft = new FormattedText("00:00", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                tf, TimeText.FontSize, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            TimeText.MinWidth = Math.Ceiling(ft.WidthIncludingTrailingWhitespace);
        }
        catch
        {
            TimeText.MinWidth = 0;
        }
    }

    public void ApplyTheme()
    {
        var t = ThemeService.Get(_cfg.Theme);
        bool noBg = _cfg.BackgroundStyle == "none" || _cfg.CardOpacity <= 0.001;

        Card.Background = ThemeService.BuildBackground(_cfg, t);
        CardGlow.Color = t.From;
        CardGlow.Opacity = noBg ? 0 : 0.32;

        // 文字颜色可以单独指定，不填就跟主题走
        Color textColor = string.IsNullOrWhiteSpace(_cfg.TextColor)
            ? (noBg ? t.From : t.Text)
            : ThemeService.C(_cfg.TextColor);

        // 字亮就用深描边，字深就用白描边，保证任何组合都看得清
        bool lightText = ThemeService.Luminance(textColor) > 0.62;
        Color outline = lightText ? t.Shadow : Colors.White;

        TimeText.Foreground = new SolidColorBrush(textColor);

        // 没有背景的时候：靠一圈紧贴的描边保证可读，不做外发光
        if (noBg)
        {
            TextGlow.Color = outline;
            TextGlow.BlurRadius = 4;
            TextGlow.Opacity = 1;
            TextGlow.ShadowDepth = 0;

            StatusText.Foreground = new SolidColorBrush(textColor);
            StatusText.Effect = new DropShadowEffect
            {
                Color = outline,
                BlurRadius = 7,
                ShadowDepth = 0,
                Opacity = 0.95
            };

            CatHost.Effect = new DropShadowEffect
            {
                Color = outline,
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.5
            };

            CheekLeft.Fill = CheekRight.Fill =
                new SolidColorBrush(ThemeService.WithAlpha(t.From, 130));

            // 虚线框换成主题色，不然白线在浅色桌面上看不见
            EditFrame.Stroke = new SolidColorBrush(t.From);
            EditFrame.Effect = new DropShadowEffect
            {
                Color = Colors.White,
                BlurRadius = 6,
                ShadowDepth = 0,
                Opacity = 0.9
            };

            ProgressFill.Background = new SolidColorBrush(t.From);
            ProgressBg.Background = new SolidColorBrush(ThemeService.WithAlpha(Colors.Black, 45));
        }
        else
        {
            // 有背景时不需要任何光晕，字干净就行
            TextGlow.Opacity = 0;
            TextGlow.BlurRadius = 0;
            TextGlow.ShadowDepth = 0;
            TextGlow.Color = Colors.Transparent;

            StatusText.Effect = null;
            CatHost.Effect = null;

            Card.BorderThickness = new Thickness(0);
            CheekLeft.Fill = CheekRight.Fill =
                new SolidColorBrush(ThemeService.WithAlpha(t.From, 120));

            EditFrame.Stroke = Brushes.White;
            EditFrame.Effect = null;

            ProgressFill.Background = new SolidColorBrush(ThemeService.WithAlpha(t.Text, 235));
            ProgressBg.Background = new SolidColorBrush(ThemeService.WithAlpha(t.Text, 58));
        }

        StatusText.Foreground = _editMode || noBg
            ? new SolidColorBrush(textColor)
            : new SolidColorBrush(t.SubText);
        StatusText.FontWeight = _editMode ? FontWeights.SemiBold : FontWeights.Medium;
        EyeLeft.Fill = EyeRight.Fill = new SolidColorBrush(ThemeService.C("5C4A56"));

        // 让设置面板等界面也同步换色
        UiTheme.Apply(t);
    }

    /// <summary>取鼠标所在那块屏幕的可用区域（换算成 WPF 的 DIP 坐标）。</summary>
    private static Rect ActiveWorkArea(Visual visual)
    {
        try
        {
            var screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
            var b = screen.WorkingArea;
            var dpi = VisualTreeHelper.GetDpi(visual);
            return new Rect(
                b.Left / dpi.DpiScaleX,
                b.Top / dpi.DpiScaleY,
                b.Width / dpi.DpiScaleX,
                b.Height / dpi.DpiScaleY);
        }
        catch
        {
            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }
    }

    private static Rect VirtualScreenRect()
        => new(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
               SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

    private static double IntersectArea(Rect a, Rect b)
    {
        var r = Rect.Intersect(a, b);
        return r.IsEmpty ? 0 : r.Width * r.Height;
    }

    private void RestorePosition()
    {
        var wa = ActiveWorkArea(this);
        var vs = VirtualScreenRect();

        double w = ActualWidth > 10 ? ActualWidth : 300;
        double h = ActualHeight > 10 ? ActualHeight : 160;

        double left = double.IsNaN(_cfg.WindowLeft) ? wa.Right - w - 40 : _cfg.WindowLeft;
        double top = double.IsNaN(_cfg.WindowTop) ? wa.Top + 60 : _cfg.WindowTop;

        // 记住的位置要是一大半都在屏幕外（换过显示器 / 改过分辨率），就挪回当前屏幕
        var rect = new Rect(left, top, w, h);
        if (IntersectArea(rect, vs) < rect.Width * rect.Height * 0.5)
        {
            left = wa.Right - w - 40;
            top = wa.Top + 60;
        }

        // 再夹一次，保证至少贴着当前屏幕边缘
        left = Math.Clamp(left, vs.Left, Math.Max(vs.Left, vs.Right - w));
        top = Math.Clamp(top, vs.Top, Math.Max(vs.Top, vs.Bottom - h));

        _suppressPosSave = true;
        Left = left;
        Top = top;
        _suppressPosSave = false;
    }

    /// <summary>把窗口挪到当前屏幕中央（万一跑到别的显示器上了）。</summary>
    public void Recenter()
    {
        var wa = ActiveWorkArea(this);
        double w = ActualWidth > 10 ? ActualWidth : 300;
        double h = ActualHeight > 10 ? ActualHeight : 160;

        _suppressPosSave = true;
        Left = wa.Left + (wa.Width - w) / 2;
        Top = wa.Top + (wa.Height - h) / 3;
        _suppressPosSave = false;

        if (!IsVisible) ShowFromTray();
        else Activate();

        SavePositionNow();
    }

    private void SavePositionNow()
    {
        if (double.IsNaN(Left) || double.IsNaN(Top)) return;
        _cfg.WindowLeft = Left;
        _cfg.WindowTop = Top;
        App.SaveConfig();
    }

    private void SavePosition()
    {
        if (!_ready) return;
        if (double.IsNaN(Left) || double.IsNaN(Top)) return;
        _cfg.WindowLeft = Left;
        _cfg.WindowTop = Top;
        App.SaveConfig();
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (_suppressPosSave || !_ready) return;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    // ==================== 模式控制 ====================

    public void SetMode(TimerMode mode, double seconds, bool start)
    {
        _mode = mode;
        _running = false;
        _sw.Reset();
        _baseElapsed = 0;

        switch (mode)
        {
            case TimerMode.Clock:
                _tick.Start();
                SetEyes(true);
                break;

            case TimerMode.Stopwatch:
                _totalSeconds = 0;
                _baseRemaining = 0;
                if (start) StartRunning();
                break;

            case TimerMode.Pomodoro:
                BeginPhase(PomodoroPhase.Work, start);
                break;

            case TimerMode.Cycle:
                BeginCyclePhase(CyclePhase.Focus, start);
                break;

            default:
                _totalSeconds = Math.Max(60, seconds);
                _baseRemaining = _totalSeconds;
                if (start) StartRunning();
                break;
        }

        Refresh();
        TimerStateChanged?.Invoke();
    }

    public void StartCountdown(double seconds, bool start = true)
    {
        _mode = TimerMode.Countdown;
        _totalSeconds = Math.Max(1, seconds);
        _baseRemaining = _totalSeconds;
        _sw.Reset();
        _running = false;
        if (start) StartRunning();
        Refresh();
        TimerStateChanged?.Invoke();
    }

    public void StartStopwatch()
    {
        _mode = TimerMode.Stopwatch;
        _baseElapsed = 0;
        _sw.Reset();
        _running = false;
        StartRunning();
        TimerStateChanged?.Invoke();
    }

    public void ShowClock()
    {
        SetMode(TimerMode.Clock, 0, false);
    }

    public void StartPomodoro()
    {
        _pomodoroDone = 0;
        SetMode(TimerMode.Pomodoro, 0, true);
    }

    /// <summary>专注 → 休息 → 专注 …… 自动循环。</summary>
    public void StartCycle()
    {
        _cycleRound = 0;
        SetMode(TimerMode.Cycle, 0, true);
    }

    public void BeginCyclePhase(CyclePhase phase, bool autoStart)
    {
        _cyclePhase = phase;
        _totalSeconds = Math.Max(1, phase == CyclePhase.Focus
            ? _cfg.CycleFocusSeconds
            : _cfg.CycleBreakSeconds);

        _baseRemaining = _totalSeconds;
        _sw.Reset();
        _running = false;
        if (autoStart) StartRunning();
        Refresh();
    }

    public void BeginPhase(PomodoroPhase phase, bool autoStart)
    {
        _phase = phase;
        _totalSeconds = Math.Max(1, phase switch
        {
            PomodoroPhase.Work => _cfg.PomodoroWorkSeconds,
            PomodoroPhase.ShortBreak => _cfg.PomodoroShortBreakSeconds,
            _ => _cfg.PomodoroLongBreakSeconds
        });

        _baseRemaining = _totalSeconds;
        _sw.Reset();
        _running = false;
        if (autoStart) StartRunning();
        Refresh();
    }

    private void StartRunning()
    {
        if (IsCountdownLike)
        {
            if (_baseRemaining <= 0) _baseRemaining = _totalSeconds;
        }
        _sw.Restart();
        _running = true;
        _preAlerted = false;
        SetEyes(true);
    }

    private void PauseRunning()
    {
        if (IsCountdownLike)
            _baseRemaining = Math.Max(0, _baseRemaining - _sw.Elapsed.TotalSeconds);
        else
            _baseElapsed += _sw.Elapsed.TotalSeconds;

        _sw.Reset();
        _running = false;
        SetEyes(false);
    }

    public void TogglePause()
    {
        if (_mode == TimerMode.Clock)
        {
            // 时钟模式下点击切回倒计时
            StartCountdown(Math.Max(1, _cfg.DefaultSeconds), start: false);
            return;
        }

        if (_running) PauseRunning();
        else StartRunning();

        Refresh();
        TimerStateChanged?.Invoke();
    }

    public void Reset()
    {
        switch (_mode)
        {
            case TimerMode.Stopwatch:
                _baseElapsed = 0;
                _sw.Reset();
                _running = false;
                break;

            case TimerMode.Pomodoro:
                BeginPhase(_phase, autoStart: false);
                break;

            case TimerMode.Cycle:
                BeginCyclePhase(_cyclePhase, autoStart: false);
                break;

            case TimerMode.Clock:
                break;

            default:
                _sw.Reset();
                _running = false;
                _baseRemaining = _totalSeconds;
                break;
        }

        SetEyes(false);
        Refresh();
        TimerStateChanged?.Invoke();
    }

    public void ToggleTopmost()
    {
        _cfg.Topmost = !_cfg.Topmost;
        Topmost = _cfg.Topmost;
        App.SaveConfig();
        TimerStateChanged?.Invoke();
    }

    /// <summary>锁定后窗口拖不动，防止不小心挪位置。</summary>
    public void ToggleLock()
    {
        _cfg.LockPosition = !_cfg.LockPosition;
        App.SaveConfig();
        TimerStateChanged?.Invoke();
    }

    public void ToggleShow()
    {
        if (IsVisible) Hide();
        else ShowFromTray();
    }

    public void ShowFromTray()
    {
        if (!IsVisible) Show();

        // 如果窗口跑到别的显示器上去了，点托盘就把它拉回鼠标所在的屏幕
        var wa = ActiveWorkArea(this);
        double w = ActualWidth > 10 ? ActualWidth : 300;
        double h = ActualHeight > 10 ? ActualHeight : 160;
        var rect = new Rect(Left, Top, w, h);

        if (IntersectArea(rect, wa) <= 0)
        {
            _suppressPosSave = true;
            Left = wa.Right - w - 40;
            Top = wa.Top + 60;
            _suppressPosSave = false;
            SavePositionNow();
        }

        Activate();
        Topmost = _cfg.Topmost;
    }

    // ==================== 渲染 ====================

    private void Refresh()
    {
        if (!_ready) return;

        switch (_mode)
        {
            case TimerMode.Clock:
                TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
                StatusText.Text = WeekdayText();
                SetProgress(0);
                break;

            case TimerMode.Stopwatch:
            {
                double el = _baseElapsed + (_running ? _sw.Elapsed.TotalSeconds : 0);
                TimeText.Text = Format(el);
                StatusText.Text = _running ? L.T("status.focus") : L.T("status.paused");
                SetProgress(0);
                break;
            }

            default:
            {
                double rem = _running ? _baseRemaining - _sw.Elapsed.TotalSeconds : _baseRemaining;
                if (rem < 0) rem = 0;
                TimeText.Text = Format(rem);
                StatusText.Text = StatusLine();

                double ratio = _totalSeconds > 0 ? 1.0 - rem / _totalSeconds : 0;
                SetProgress(Math.Clamp(ratio, 0, 1));

                if (_running && rem <= 0.001)
                {
                    HandleFinish();
                    break;
                }

                // 提前提醒：只在跨过阈值时响一次
                if (_running && !_preAlerted && _cfg.PreAlertEnabled &&
                    rem > 0 && rem <= _cfg.PreAlertSeconds)
                {
                    _preAlerted = true;
                    NotifyRequested?.Invoke(
                        L.T("notify.preAlertTitle"),
                        L.T("notify.preAlert", (int)Math.Ceiling(rem)));
                    PlaySound(_cfg.SoundPreAlert);
                }
                break;
            }
        }
    }

    private string StatusLine()
    {
        if (_editMode) return L.T("status.editing");

        if (_mode == TimerMode.Cycle)
        {
            string phaseText = _cyclePhase == CyclePhase.Focus ? L.T("status.focus") : L.T("status.break");
            string body = $"{phaseText} · {L.T("status.cycleRound", _cycleRound + 1)}";
            if (_cfg.CycleRounds > 0)
                body += $" / {_cfg.CycleRounds}";
            return _running ? body : $"{body} · {L.T("status.paused")}";
        }

        if (_mode == TimerMode.Pomodoro)
        {
            string phaseText = _phase switch
            {
                PomodoroPhase.Work => L.T("status.focus"),
                PomodoroPhase.ShortBreak => L.T("status.breakShort"),
                _ => L.T("status.breakLong")
            };
            string extra = _phase == PomodoroPhase.Work
                ? L.T("status.pomodoroCount", _pomodoroDone + 1)
                : "";
            string body = string.IsNullOrEmpty(extra) ? phaseText : $"{phaseText} · {extra}";
            return _running ? body : $"{body} · {L.T("status.paused")}";
        }

        if (_baseRemaining <= 0 && _totalSeconds > 0) return L.T("status.done");
        return _running ? L.T("status.focus") : (_baseRemaining >= _totalSeconds ? L.T("status.ready") : L.T("status.paused"));
    }

    private string WeekdayText()
    {
        var now = DateTime.Now;
        if (L.Lang == "en")
            return now.ToString("MMM d, dddd", CultureInfo.GetCultureInfo("en-US"));
        var zh = CultureInfo.GetCultureInfo("zh-CN");
        return now.ToString("M月d日", zh) + "  " + now.ToString("dddd", zh);
    }

    private void SetProgress(double ratio)
    {
        double w = ProgressBg.ActualWidth;
        if (w <= 0) return;
        ProgressFill.Width = Math.Max(0, w * ratio);
    }

    private static string Format(double seconds)
    {
        int total = (int)Math.Ceiling(Math.Max(0, seconds));
        if (total >= 3600)
        {
            int h = total / 3600, m = total % 3600 / 60, s = total % 60;
            return $"{h}:{m:00}:{s:00}";
        }
        return $"{total / 60:00}:{total % 60:00}";
    }

    private void SetEyes(bool open)
    {
        double sy = open ? 1.0 : 0.18;
        var anim = new DoubleAnimation(sy, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        EyeLeftScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        EyeRightScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    private void StartBreathing()
    {
        var tt = new TranslateTransform();
        CatHost.RenderTransform = tt;
        var anim = new DoubleAnimation(0, -2.2, TimeSpan.FromSeconds(1.6))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        tt.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    // ==================== 完成处理 ====================

    private void HandleFinish()
    {
        _running = false;
        _sw.Reset();

        var finishedMode = _mode;
        var finishedPhase = _phase;
        int minutes = (int)Math.Round(_totalSeconds / 60.0);

        switch (finishedMode)
        {
            case TimerMode.Pomodoro:
            {
                bool isWork = finishedPhase == PomodoroPhase.Work;
                if (isWork)
                {
                    _pomodoroDone++;
                    StatsService.Add(minutes, true);
                    PlaySound(_cfg.SoundFinish);
                }
                else
                {
                    PlaySound(_cfg.SoundBreakEnd);
                }

                Flash();

                if (isWork)
                {
                    bool longBreak = _pomodoroDone % Math.Max(1, _cfg.PomodoroRoundsBeforeLong) == 0;
                    var next = longBreak ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak;
                    int nextSec = longBreak ? _cfg.PomodoroLongBreakSeconds : _cfg.PomodoroShortBreakSeconds;
                    int nextMin = Math.Max(1, (int)Math.Round(nextSec / 60.0));
                    NotifyRequested?.Invoke(L.T("notify.restTime"), L.T("notify.restTimeBody", nextMin));
                    BeginPhase(next, _cfg.PomodoroAutoNext);
                }
                else
                {
                    NotifyRequested?.Invoke(L.T("notify.restOver"), L.T("notify.restOverBody"));
                    BeginPhase(PomodoroPhase.Work, _cfg.PomodoroAutoNext);
                }
                break;
            }

            case TimerMode.Cycle:
            {
                if (_cyclePhase == CyclePhase.Focus)
                {
                    _cycleRound++;
                    StatsService.Add(minutes, true);
                    PlaySound(_cfg.SoundFinish);
                    Flash();
                    NotifyRequested?.Invoke(L.T("notify.restTime"),
                        L.T("notify.restTimeBody", Math.Max(1, (int)Math.Round(_cfg.CycleBreakSeconds / 60.0))));
                    BeginCyclePhase(CyclePhase.Break, _cfg.CycleAutoNext);
                }
                else
                {
                    PlaySound(_cfg.SoundBreakEnd);
                    Flash();

                    // 达到设定轮数就收工
                    if (_cfg.CycleRounds > 0 && _cycleRound >= _cfg.CycleRounds)
                    {
                        NotifyRequested?.Invoke(L.T("notify.cycleDone"),
                            L.T("notify.cycleDoneBody", _cycleRound));
                        _baseRemaining = 0;
                        SetEyes(false);
                        Refresh();
                        TimerStateChanged?.Invoke();
                        return;
                    }

                    NotifyRequested?.Invoke(L.T("notify.restOver"), L.T("notify.restOverBody"));
                    BeginCyclePhase(CyclePhase.Focus, _cfg.CycleAutoNext);
                }
                break;
            }

            default:
                if (minutes >= 1) StatsService.Add(minutes, false);
                PlaySound(_cfg.SoundFinish);
                Flash();
                NotifyRequested?.Invoke(L.T("notify.finish"), L.T("notify.finishBody"));
                _baseRemaining = 0;
                break;
        }

        // 自动进入下一阶段时保持睁眼，真正停下来才眯眼
        SetEyes(_running);
        Refresh();
        TimerStateChanged?.Invoke();
    }

    private void PlaySound(string? id)
    {
        if (!_cfg.SoundEnabled) return;
        SoundService.Play(id, _cfg.SoundVolume);
    }

    private void Flash()
    {
        if (!_cfg.FlashOnFinish) return;
        var anim = new DoubleAnimation(1.0, 0.22, TimeSpan.FromMilliseconds(280))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(3)
        };
        Card.BeginAnimation(OpacityProperty, anim);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 关闭按钮 / Alt+F4 都只是收进托盘，真正退出走托盘菜单
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }

    /// <summary>编辑模式：只有进入这里，滚轮才会改动时间。</summary>
    public void ToggleEditMode(bool? force = null)
    {
        _editMode = force ?? !_editMode;

        EditFrame.Visibility = _editMode ? Visibility.Visible : Visibility.Collapsed;
        ResizeHandle.Visibility = _editMode ? Visibility.Visible : Visibility.Collapsed;

        var theme = ThemeService.Get(_cfg.Theme);
        StatusText.Foreground = new SolidColorBrush(_editMode ? theme.Text : theme.SubText);
        StatusText.FontWeight = _editMode ? FontWeights.SemiBold : FontWeights.Medium;

        if (_editMode)
        {
            try { Activate(); } catch { }
            ToolTip = L.T("tip.edit");
        }
        else
        {
            ToolTip = null;
        }

        Refresh();
        TimerStateChanged?.Invoke();
        if (_editMode) App.SaveConfig();
    }

    // ==================== 缩放手柄 ====================

    private bool _resizing;
    private Point _resizeStartScreen;
    private double _resizeStartFont;

    private Point ScreenPoint()
    {
        var p = System.Windows.Forms.Cursor.Position;
        var dpi = VisualTreeHelper.GetDpi(this);
        return new Point(p.X / dpi.DpiScaleX, p.Y / dpi.DpiScaleY);
    }

    private void OnResizeStart(object sender, MouseButtonEventArgs e)
    {
        if (!_editMode) return;
        _resizing = true;
        _resizeStartScreen = ScreenPoint();
        _resizeStartFont = _cfg.FontSize;
        if (sender is UIElement el) el.CaptureMouse();
        e.Handled = true;
    }

    private void OnResizeMove(object sender, MouseEventArgs e)
    {
        if (!_resizing) return;

        var now = ScreenPoint();
        double delta = (now.X - _resizeStartScreen.X) + (now.Y - _resizeStartScreen.Y);
        double target = Math.Clamp(_resizeStartFont + delta * 0.30, 18, 130);

        if (Math.Abs(target - _cfg.FontSize) < 0.8) return;

        _cfg.FontSize = Math.Round(target);
        ApplyConfig();
        e.Handled = true;
    }

    private void OnResizeEnd(object sender, MouseButtonEventArgs e)
    {
        if (!_resizing) return;
        _resizing = false;
        if (sender is UIElement el) el.ReleaseMouseCapture();
        App.SaveConfig();
        e.Handled = true;
    }

    // ==================== 鼠标交互 ====================

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _dragStart = e.GetPosition(this);
        _pressed = true;
        _dragging = false;
        try { CaptureMouse(); } catch { }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_pressed || _dragging) return;

        var p = e.GetPosition(this);
        // 阈值放宽到 8px：手抖一下不会被误判成拖动，点击才开始计时
        if (Math.Abs(p.X - _dragStart.X) > 8 || Math.Abs(p.Y - _dragStart.Y) > 8)
        {
            _dragging = true;
            _pressed = false;
            if (IsMouseCaptured) ReleaseMouseCapture();

            if (_cfg.LockPosition) return;   // 锁定了就不让拖

            try { DragMove(); } catch { }
            SavePosition();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (IsMouseCaptured) ReleaseMouseCapture();

        if (_pressed && !_dragging && !_editMode)
        {
            // 编辑模式下单击不启动计时，避免调时间时误触
            TogglePause();
        }

        _pressed = false;
        _dragging = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _editMode)
        {
            ToggleEditMode(false);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        e.Handled = true;

        // 平时滚轮完全不生效：鼠标掠过窗口不会误改时间
        if (!_editMode) return;

        var mods = Keyboard.Modifiers;
        bool up = e.Delta > 0;

        // Alt + 滚轮：背景不透明度
        if ((mods & ModifierKeys.Alt) != 0)
        {
            _cfg.CardOpacity = Math.Clamp(Math.Round(_cfg.CardOpacity + (up ? 0.05 : -0.05), 2), 0, 1);
            ApplyTheme();
            App.SaveConfig();
        }
        // 滚轮 ±1 分钟 / Shift ±10 秒（精确）/ Ctrl ±5 分钟（大步）
        else if (IsCountdownLike)
        {
            int step = (mods & ModifierKeys.Shift) != 0 ? 10
                     : (mods & ModifierKeys.Control) != 0 ? 300
                     : 60;
            AdjustTime(up ? step : -step);
        }
    }

    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        // 编辑模式下双击窗口 → 直接输入时长
        if (_editMode)
        {
            OpenSetTimer();
            e.Handled = true;
        }
    }

    public void AdjustTime(double deltaSeconds)
    {
        if (_mode == TimerMode.Clock || _mode == TimerMode.Stopwatch) return;

        if (_running)
        {
            _baseRemaining = Math.Max(1, _baseRemaining + deltaSeconds);
            if (_baseRemaining > _totalSeconds) _totalSeconds = _baseRemaining;
        }
        else
        {
            _totalSeconds = Math.Max(60, _totalSeconds + deltaSeconds);
            _baseRemaining = _totalSeconds;
        }

        Refresh();
        TimerStateChanged?.Invoke();
    }

    // ==================== 右键菜单 ====================

    private void RebuildMenu(ContextMenu cm)
    {
        cm.Items.Clear();

        cm.Items.Add(MenuItem(_running ? L.T("menu.pause") : L.T("menu.start"),
            () => TogglePause(), _cfg.HotkeyTogglePause));
        cm.Items.Add(MenuItem(L.T("menu.reset"), Reset, _cfg.HotkeyReset));
        cm.Items.Add(MenuItem(L.T("menu.setTimer"), OpenSetTimer, _cfg.HotkeySetTimer));
        cm.Items.Add(MenuItem(_editMode ? L.T("menu.editOff") : L.T("menu.editOn"),
            () => ToggleEditMode(), ""));

        cm.Items.Add(new Separator());

        cm.Items.Add(MenuItem(L.T("menu.pomodoro"), StartPomodoro, _cfg.HotkeyPomodoro));
        cm.Items.Add(MenuItem(L.T("menu.cycle"), StartCycle, _cfg.HotkeyCycle));
        cm.Items.Add(MenuItem(L.T("menu.stopwatch"), StartStopwatch, ""));
        cm.Items.Add(MenuItem(L.T("menu.clock"), ShowClock, _cfg.HotkeyClock));

        cm.Items.Add(new Separator());

        cm.Items.Add(MenuItem((_cfg.Topmost ? "✓ " : "   ") + L.T("menu.topmost"),
            ToggleTopmost, _cfg.HotkeyTopmost));
        cm.Items.Add(MenuItem((_cfg.LockPosition ? "✓ " : "   ") +
            (_cfg.LockPosition ? L.T("menu.unlock") : L.T("menu.lock")), ToggleLock, ""));
        cm.Items.Add(MenuItem(L.T("menu.recenter"), Recenter, ""));

        cm.Items.Add(new Separator());

        cm.Items.Add(MenuItem(L.T("menu.settings"), OpenSettings, ""));
        cm.Items.Add(MenuItem(L.T("menu.stats"), OpenStats, ""));
        cm.Items.Add(MenuItem(L.T("menu.hide"), Hide, _cfg.HotkeyShowHide));

        cm.Items.Add(new Separator());

        cm.Items.Add(MenuItem(L.T("menu.exit"), () =>
        {
            App.IsExiting = true;
            Application.Current.Shutdown();
        }, ""));
    }

    private static MenuItem MenuItem(string header, Action action, string gesture)
    {
        var mi = new MenuItem { Header = header, InputGestureText = gesture };
        mi.Click += (_, _) => action();
        return mi;
    }

    public void OpenSetTimer()
    {
        var dlg = new QuickInputWindow(_cfg.DefaultSeconds) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Seconds > 0)
            StartCountdown(dlg.Seconds);
    }

    private void OpenSettings()
    {
        var w = new SettingsWindow(_cfg) { Owner = this };
        w.ShowDialog();
        ApplyConfig();
        TimerStateChanged?.Invoke();
    }

    private void OpenStats()
    {
        var w = new StatsWindow { Owner = this };
        w.ShowDialog();
    }
}
