using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace MeowTimerMac.Services;

/// <summary>菜单栏（macOS）／系统托盘（Windows）图标。</summary>
public static class TrayService
{
    private static TrayIcon? _tray;

    public static void Init(Window main, Action toggleTimer, Action reset, Action quit)
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://MeowTimer/Assets/meow.png"));

            _tray = new TrayIcon
            {
                Icon = new WindowIcon(stream),
                ToolTipText = "喵时 MeowTimer",
                IsVisible = true
            };

            _tray.Clicked += (_, _) => ShowMain(main);

            var menu = new NativeMenu();

            var show = new NativeMenuItem("显示喵时");
            show.Click += (_, _) => ShowMain(main);
            menu.Add(show);

            var toggle = new NativeMenuItem("开始 / 暂停");
            toggle.Click += (_, _) => toggleTimer();
            menu.Add(toggle);

            var res = new NativeMenuItem("重置");
            res.Click += (_, _) => reset();
            menu.Add(res);

            menu.Add(new NativeMenuItemSeparator());

            var exit = new NativeMenuItem("退出喵时");
            exit.Click += (_, _) => quit();
            menu.Add(exit);

            _tray.Menu = menu;

            if (Application.Current != null)
                TrayIcon.SetIcons(Application.Current, new TrayIcons { _tray });
        }
        catch
        {
            // 托盘起不来不影响正常使用
        }
    }

    private static void ShowMain(Window main)
    {
        main.Show();
        main.WindowState = WindowState.Normal;
        main.Activate();
    }

    public static void UpdateToolTip(string text)
    {
        if (_tray != null) _tray.ToolTipText = text;
    }
}
