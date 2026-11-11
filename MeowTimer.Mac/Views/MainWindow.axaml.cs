using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MeowTimer.Models;
using MeowTimer.Services;

namespace MeowTimerMac.Views;

public partial class MainWindow : Window
{
    private enum Phase { Idle, Running, Paused, Done }

    private readonly DispatcherTimer _ticker;
    private Phase _phase = Phase.Idle;
    private int _total;
    private int _remain;
    private DateTime _lastTick;
    private Point _pressPoint;
    private bool _pressed;
    private PointerPressedEventArgs? _pressArgs;

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

        SizeChanged += (_, _) => UpdateProgressBar();
        UpdateContextMenu();
    }

    // ==================== 外观 ====================

    public void ApplyTheme()
    {
        var cfg = App.Config;
        var t = ThemeService.Get(cfg.Theme);

        bool noBg = cfg.BackgroundStyle == "none" || cfg.CardOpacity <= 0.001;

        Card.Background = ThemeService.BuildBackground(cfg, t);
        Card.BoxShadow = noBg ? default : new BoxShadows(new BoxShadow
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

        double left = double.IsNaN(App.Config.WindowLeft) ? screen.WorkingArea.Right - Width - 40 : App.Config.WindowLeft;
        double top = double.IsNaN(App.Config.WindowTop) ? screen.WorkingArea.Y + 40 : App.Config.WindowTop;

        Position = new PixelPoint((int)Math.Max(0, left), (int)Math.Max(0, top));
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
            _phase = Phase.Done;

            if (App.Config.SoundEnabled)
                SoundService.Play(App.Config.SoundFinish, App.Config.SoundVolume);

            Refresh();
            return;
        }

        Refresh();
    }

    private void Toggle()
    {
        switch (_phase)
        {
            case Phase.Idle:
            case Phase.Done:
                if (_phase == Phase.Done) _remain = _total;
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

    public void Reset()
    {
        _ticker.Stop();
        _phase = Phase.Idle;
        _total = Math.Max(1, App.Config.DefaultSeconds);
        _remain = _total;
        Refresh();
    }

    private void SetTotal(int seconds)
    {
        _ticker.Stop();
        _phase = Phase.Idle;
        _total = Math.Max(1, seconds);
        _remain = _total;
        Refresh();
    }

    private void Adjust(int deltaSeconds)
    {
        if (_phase == Phase.Running) return;

        _total = Math.Max(1, _total + deltaSeconds);
        _remain = _total;
        _phase = Phase.Idle;
        Refresh();
    }

    // ==================== 刷新界面 ====================

    private void Refresh()
    {
        TimeText.Text = Format(_remain);

        StatusText.Text = _phase switch
        {
            Phase.Running => "专注中 · 点一下暂停",
            Phase.Paused => "已暂停 · 点一下继续",
            Phase.Done => "时间到啦 🎉",
            _ => "点一下开始"
        };

        UpdateProgressBar();
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
        if (!IsPositionLocked())
        {
            Adjust(e.Delta.Y > 0 ? 60 : -60);
            e.Handled = true;
        }
    }

    private bool IsPositionLocked() => App.Config.LockPosition;

    // ==================== 右键菜单 ====================

    private void UpdateContextMenu()
    {
        var cfg = App.Config;

        var menu = new ContextMenu();

        menu.Items.Add(Item(_phase == Phase.Running ? "暂停" : "开始", (_, _) => Toggle()));
        menu.Items.Add(Item("重置", (_, _) => Reset()));
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
        var ok = await dlg.ShowDialog<bool>(this);
        if (ok) SetTotal(dlg.Seconds);
    }

    private async void OpenSettings()
    {
        var dlg = new SettingsWindow();
        await dlg.ShowDialog(this);
        ApplyTheme();
        Refresh();
    }

    private void Quit()
    {
        App.Config.WindowLeft = Position.X;
        App.Config.WindowTop = Position.Y;
        App.Save();

        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}
