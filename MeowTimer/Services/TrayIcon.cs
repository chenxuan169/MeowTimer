using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using MeowTimer.Models;
using Draw = System.Drawing;
using Forms = System.Windows.Forms;

namespace MeowTimer.Services;

/// <summary>系统托盘：猫咪图标 / CPU / 内存百分比 + 右键菜单 + 气泡通知。</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly MainWindow _main;
    private readonly DispatcherTimer _monitor;

    private Draw.Icon? _current;
    private Draw.Icon? _previous;
    private int _lastValue = -1;
    private string _lastMode = "";

    public TrayIcon(MainWindow main)
    {
        _main = main;

        _icon = new Forms.NotifyIcon
        {
            Visible = true,
            Text = L.T("tray.tip"),
            Icon = CatIconFactory.CatIcon(IconSize())
        };
        _icon.MouseUp += OnMouseUp;
        _icon.MouseDoubleClick += (_, _) => _main.ShowFromTray();

        _monitor = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _monitor.Tick += (_, _) => UpdateIcon();
        _monitor.Start();

        UpdateIcon();
    }

    private static int IconSize() => Math.Max(16, Forms.SystemInformation.SmallIconSize.Width);

    private void OnMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            // 左键点托盘 = 我要看到窗口（不再切换隐藏，避免点了反而消失）
            _main.ShowFromTray();
        }
        else if (e.Button == Forms.MouseButtons.Right)
        {
            ShowMenu();
        }
    }

    private void ShowMenu()
    {
        try
        {
            if (!_main.IsVisible) _main.ShowFromTray();

            var cm = _main.ContextMenu;
            if (cm == null) return;

            cm.PlacementTarget = _main;
            cm.Placement = PlacementMode.MousePoint;
            cm.HorizontalOffset = 0;
            cm.VerticalOffset = 0;
            cm.IsOpen = true;
        }
        catch
        {
            // 菜单弹出失败不影响使用
        }
    }

    public void Notify(string title, string body)
    {
        try
        {
            if (!_main.Config.NotifyEnabled) return;
            _icon.ShowBalloonTip(4000, title, body, Forms.ToolTipIcon.None);
        }
        catch { }
    }

    private void UpdateIcon()
    {
        try
        {
            var cfg = _main.Config;
            string mode = string.IsNullOrWhiteSpace(cfg.TrayMode) ? "cat" : cfg.TrayMode;

            int value = mode switch
            {
                "cpu" => SystemMonitor.CpuPercent(),
                "mem" => SystemMonitor.MemoryPercent(),
                _ => -1
            };

            if (mode == _lastMode && value == _lastValue && _current != null) return;
            _lastMode = mode;
            _lastValue = value;

            int size = IconSize();

            if (mode == "cat")
            {
                SetIcon(CatIconFactory.CatIcon(size));
            }
            else
            {
                var t = ThemeService.Get(cfg.Theme);
                var bg = Draw.Color.FromArgb(t.From.R, t.From.G, t.From.B);
                SetIcon(CatIconFactory.PercentIcon(size, value, bg, Draw.Color.White));
            }
        }
        catch { }
    }

    private void SetIcon(Draw.Icon icon)
    {
        // 延迟一轮释放，避免 NotifyIcon 仍在引用旧句柄
        _previous?.Dispose();
        _previous = _current;
        _current = icon;
        _icon.Icon = icon;
    }

    public void Refresh()
    {
        _lastMode = "";
        _lastValue = -1;
        try { _icon.Text = L.T("tray.tip"); } catch { }
        UpdateIcon();
    }

    public void Dispose()
    {
        try
        {
            _monitor.Stop();
            _icon.Visible = false;
            _icon.Dispose();
        }
        catch { }

        _current?.Dispose();
        _previous?.Dispose();
        _current = null;
        _previous = null;
    }
}
