using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using MeowTimer.Models;
using MeowTimer.Services;
using MeowTimerMac.Services;
using MeowTimerMac.Views;

namespace MeowTimerMac;

public partial class App : Application
{
    public static AppConfig Config { get; private set; } = new();
    public static MainWindow? MainWin { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Config = ConfigService.Load();
        MainWin = new MainWindow();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = MainWin;
            // 关窗口只是收进托盘，真正退出走菜单里的「退出喵时」
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();

        TrayService.Init(
            MainWin,
            toggleTimer: () => MainWin?.Toggle(),
            reset: () => MainWin?.Reset(),
            quit: () => MainWin?.Quit());

        HandleScreenshotMode();
    }

    /// <summary>--shot &lt;路径&gt;：渲染一张窗口截图后自动退出，用来生成 README 配图。</summary>
    private void HandleScreenshotMode()
    {
        var args = Environment.GetCommandLineArgs();
        int idx = Array.IndexOf(args, "--shot");
        if (idx < 0 || idx + 1 >= args.Length) return;

        var path = args[idx + 1];

        DispatcherTimer.RunOnce(() =>
        {
            try
            {
                if (MainWin != null)
                {
                    var size = new PixelSize(
                        Math.Max(1, (int)Math.Ceiling(MainWin.Bounds.Width)),
                        Math.Max(1, (int)Math.Ceiling(MainWin.Bounds.Height)));

                    using var bmp = new RenderTargetBitmap(size, new Vector(96, 96));
                    bmp.Render(MainWin);
                    bmp.Save(path);
                }
            }
            catch { }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d) d.Shutdown();
        }, TimeSpan.FromMilliseconds(900));
    }

    public static void Save() => ConfigService.Save(Config);
}
