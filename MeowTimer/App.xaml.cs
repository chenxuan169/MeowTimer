using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using MeowTimer.Models;
using MeowTimer.Services;

namespace MeowTimer;

public partial class App : Application
{
    // 互斥名带上数据目录的哈希：同一套配置只允许一个实例，但不同数据目录可以并行（方便调试）
    private static string MutexName => InstanceKey("SingleInstance");
    private static string ShowEventName => InstanceKey("ShowWindow");

    private static string InstanceKey(string kind)
    {
        var dir = (ConfigService.DataDir ?? "").ToLowerInvariant();
        unchecked
        {
            int h = 17;
            foreach (var ch in dir) h = h * 31 + ch;
            return $"MeowTimer_{kind}_{h:X8}";
        }
    }

    private static Mutex? _mutex;
    private static EventWaitHandle? _showEvent;

    public static AppConfig Config { get; private set; } = new();
    public static HotkeyService? Hotkeys { get; private set; }
    public static TrayIcon? Tray { get; private set; }
    public static MainWindow? MainWin { get; private set; }
    public static bool IsExiting { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) => LogCrash(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => LogCrash(args.ExceptionObject as Exception);

        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // 已经有一个实例在跑：直接让它把窗口叫出来，而不是傻站着弹个框
            bool notified = false;
            try
            {
                if (EventWaitHandle.TryOpenExisting(ShowEventName, out var ev))
                {
                    using (ev) ev.Set();
                    notified = true;
                }
            }
            catch { }

            if (!notified)
            {
                MessageBox.Show("喵时已经在运行啦～\n看看屏幕右下角的托盘图标，左键点一下就能叫出窗口。",
                    "喵时 MeowTimer", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            Shutdown();
            return;
        }

        base.OnStartup(e);

        Config = ConfigService.Load();
        L.Lang = string.IsNullOrWhiteSpace(Config.Language) ? "zh" : Config.Language;
        StatsService.Load();
        SoundService.EnsureSampleFiles();

        MainWin = new MainWindow(Config);
        MainWindow = MainWin;
        MainWin.NotifyRequested += (title, body) => Tray?.Notify(title, body);

        Hotkeys = new HotkeyService();
        Hotkeys.Pressed += OnHotkey;

        Tray = new TrayIcon(MainWin);
        StartShowListener();

        MainWin.Show();
        Hotkeys.Attach(MainWin);
        ReloadHotkeys();

        // 命令行：MeowTimer.exe 15m  → 直接开始倒计时
        var arg = e.Args.FirstOrDefault(a => !a.StartsWith("-"));
        if (arg != null)
        {
            var sec = DurationParser.Parse(arg);
            if (sec is > 0) MainWin.StartCountdown((int)sec.Value);
        }

        bool minimized = Config.StartMinimized ||
                         e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase));
        if (minimized) MainWin.Hide();

        PreloadSounds();

        // 开发自检：--shot <文件> / --shot-settings <文件> / --shot-stats <文件>
        int shotIdx = Array.FindIndex(e.Args, a => a.Equals("--shot", StringComparison.OrdinalIgnoreCase));
        int setIdx = Array.FindIndex(e.Args, a => a.Equals("--shot-settings", StringComparison.OrdinalIgnoreCase));
        int statIdx = Array.FindIndex(e.Args, a => a.Equals("--shot-stats", StringComparison.OrdinalIgnoreCase));
        int selfIdx = Array.FindIndex(e.Args, a => a.Equals("--selftest", StringComparison.OrdinalIgnoreCase));

        if (e.Args.Any(a => a.Equals("--run", StringComparison.OrdinalIgnoreCase)))
            MainWin.StartCountdown(600);

        if (e.Args.Any(a => a.Equals("--cycle", StringComparison.OrdinalIgnoreCase)))
            MainWin.StartCycle();

        if (e.Args.Any(a => a.Equals("--edit", StringComparison.OrdinalIgnoreCase)))
            MainWin.ToggleEditMode(true);

        int delayIdx = Array.FindIndex(e.Args, a => a.Equals("--shot-delay", StringComparison.OrdinalIgnoreCase));
        int shotDelay = 1100;
        if (delayIdx >= 0 && delayIdx + 1 < e.Args.Length && int.TryParse(e.Args[delayIdx + 1], out var dms))
            shotDelay = Math.Clamp(dms, 300, 120000);

        int scrollIdx = Array.FindIndex(e.Args, a => a.Equals("--scroll", StringComparison.OrdinalIgnoreCase));
        double scrollY = 0;
        if (scrollIdx >= 0 && scrollIdx + 1 < e.Args.Length && double.TryParse(e.Args[scrollIdx + 1], out var sy))
            scrollY = sy;

        if (selfIdx >= 0 && selfIdx + 1 < e.Args.Length)
        {
            RunSelfTest(e.Args[selfIdx + 1]);
        }
        else if (e.Args.Any(a => a.Equals("--dump-window", StringComparison.OrdinalIgnoreCase)))
        {
            int di = Array.FindIndex(e.Args, a => a.Equals("--dump-window", StringComparison.OrdinalIgnoreCase));
            DumpWindowState(MainWin, di + 1 < e.Args.Length ? e.Args[di + 1] : "window-dump.txt");
        }
        else if (shotIdx >= 0 && shotIdx + 1 < e.Args.Length)
        {
            CaptureVisual(MainWin, e.Args[shotIdx + 1], null, shotDelay);
        }
        else if (setIdx >= 0 && setIdx + 1 < e.Args.Length)
        {
            var win = new Views.SettingsWindow(Config) { Owner = MainWin };
            win.Show();
            if (scrollY > 0) win.ScrollTo(scrollY);
            CaptureVisual(win, e.Args[setIdx + 1], win, shotDelay);
        }
        else if (statIdx >= 0 && statIdx + 1 < e.Args.Length)
        {
            var win = new Views.StatsWindow { Owner = MainWin };
            win.Show();
            CaptureVisual(win, e.Args[statIdx + 1], win, shotDelay);
        }
        else if (Array.FindIndex(e.Args, a => a.Equals("--shot-input", StringComparison.OrdinalIgnoreCase)) is int ii && ii >= 0 && ii + 1 < e.Args.Length)
        {
            var win = new Views.QuickInputWindow(Config.DefaultSeconds) { Owner = MainWin };
            win.Show();
            CaptureVisual(win, e.Args[ii + 1], win, shotDelay);
        }
    }

    private void RunSelfTest(string path)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("== MeowTimer self test ==");
        sb.AppendLine($"time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"os: {Environment.OSVersion}, 64bit={Environment.Is64BitProcess}");

        var cases = new (string input, double? expect)[]
        {
            ("25", 1500), ("25m", 1500), ("90s", 90), ("1h30m", 5400),
            ("1:30", 90), ("25分钟", 1500), ("abc", null), ("", null)
        };
        int pass = 0;
        foreach (var (input, expect) in cases)
        {
            var got = DurationParser.Parse(input);
            bool ok = expect is null
                ? got is null
                : got is not null && Math.Abs(expect.Value - got!.Value) < 0.01;
            if (ok) pass++;
            sb.AppendLine($"parse '{input}' -> {got?.ToString() ?? "null"} {(ok ? "OK" : "FAIL")}");
        }
        sb.AppendLine($"parse score: {pass}/{cases.Length}");

        ConfigService.Save(ConfigService.Clone(Config));
        var loaded = ConfigService.Load();
        sb.AppendLine($"config: theme={loaded.Theme} font={loaded.FontFamily} size={loaded.FontSize} OK");

        var today = StatsService.GetDay(DateTime.Now);
        var total = StatsService.Total();
        sb.AppendLine($"stats: today {today.Pomodoros} pomo / {today.FocusMinutes} min, total {total.minutes} min, activeDays={StatsService.ActiveDays()}");

        sb.AppendLine($"hotkeys: {Hotkeys?.LastRegisteredCount ?? 0} registered, failed=[{string.Join(",", Hotkeys?.LastFailedKeys ?? new List<string>())}]");

        try
        {
            using var ic = CatIconFactory.CatIcon(16);
            using var ic2 = CatIconFactory.PercentIcon(16, 42, System.Drawing.Color.HotPink, System.Drawing.Color.White);
            sb.AppendLine($"tray icons: cat {ic.Width}x{ic.Height} OK, percent {ic2.Width}x{ic2.Height} OK");
        }
        catch (Exception ex) { sb.AppendLine($"tray icons: FAIL {ex.Message}"); }

        try
        {
            var wav = SoundService.BuildMeowWav(80);
            var head = System.Text.Encoding.ASCII.GetString(wav, 0, 4) + "/" + System.Text.Encoding.ASCII.GetString(wav, 8, 4);
            sb.AppendLine($"meow wav: {wav.Length} bytes, header={head}");
        }
        catch (Exception ex) { sb.AppendLine($"meow wav: FAIL {ex.Message}"); }

        SystemMonitor.CpuPercent();
        Thread.Sleep(150);
        sb.AppendLine($"system: cpu={SystemMonitor.CpuPercent()}% mem={SystemMonitor.MemoryPercent()}%");

        sb.AppendLine($"autostart: {StartupService.IsEnabled()}");
        sb.AppendLine($"themes: {string.Join(", ", ThemeService.All.Select(t => t.Id))}");

        int soundOk = 0;
        foreach (var s in SoundService.Builtins)
        {
            try { if (SoundService.Synth(s.Id, 80).Length > 1000) soundOk++; } catch { }
        }
        sb.AppendLine($"sounds: {soundOk}/{SoundService.Builtins.Count} synthesized ok");

        var soundDir = string.IsNullOrWhiteSpace(Config.SoundDir) ? SoundService.DefaultDir : Config.SoundDir;
        var soundFiles = SoundService.ScanFiles(soundDir).ToList();
        sb.AppendLine($"sound folder: {soundDir} ({soundFiles.Count} files)");
        sb.AppendLine($"sound files: {string.Join(", ", soundFiles.Select(Path.GetFileName))}");
        sb.AppendLine($"sound binding: finish={Config.SoundFinish}, break={Config.SoundBreakEnd}, pre={Config.SoundPreAlert}");
        sb.AppendLine($"cycle: focus={Config.CycleFocusSeconds}s, break={Config.CycleBreakSeconds}s, rounds={Config.CycleRounds}, auto={Config.CycleAutoNext}");
        sb.AppendLine($"pomodoro: work={Config.PomodoroWorkSeconds}s, short={Config.PomodoroShortBreakSeconds}s, long={Config.PomodoroLongBreakSeconds}s, rounds={Config.PomodoroRoundsBeforeLong}");
        sb.AppendLine($"modes: countdown / stopwatch / clock / pomodoro");
        sb.AppendLine($"config path: {ConfigService.ConfigPath}");
        sb.AppendLine("== done ==");

        try { File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8); }
        catch (Exception ex) { LogCrash(ex); }

        Shutdown();
    }

    /// <summary>把窗口实际位置 / DPI / 工作区写出来，用于排查"窗口看不见"。</summary>
    private void DumpWindowState(Window w, string path)
    {
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var sb = new System.Text.StringBuilder();
            try
            {
                var wa = SystemParameters.WorkArea;
                var dpi = VisualTreeHelper.GetDpi(w);

                sb.AppendLine("== window state ==");
                sb.AppendLine($"Left={w.Left}  Top={w.Top}  Width={w.Width}  Height={w.Height}");
                sb.AppendLine($"ActualWidth={w.ActualWidth}  ActualHeight={w.ActualHeight}");
                sb.AppendLine($"IsVisible={w.IsVisible}  WindowState={w.WindowState}  Topmost={w.Topmost}  Opacity={w.Opacity}");
                sb.AppendLine($"RestoreBounds={w.RestoreBounds}");
                sb.AppendLine();
                sb.AppendLine($"WorkArea: {wa.Left},{wa.Top} {wa.Width}x{wa.Height}");
                sb.AppendLine($"PrimaryScreen: {SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}");
                sb.AppendLine($"VirtualScreen: {SystemParameters.VirtualScreenLeft},{SystemParameters.VirtualScreenTop} {SystemParameters.VirtualScreenWidth}x{SystemParameters.VirtualScreenHeight}");
                sb.AppendLine($"DPI: {dpi.DpiScaleX}x{dpi.DpiScaleY}  ({dpi.PixelsPerInchX} dpi)");
                sb.AppendLine();
                sb.AppendLine($"config: WindowLeft={Config.WindowLeft} WindowTop={Config.WindowTop} StartMinimized={Config.StartMinimized}");

                // 判断窗口矩形是否和工作区相交
                double right = w.Left + w.ActualWidth;
                double bottom = w.Top + w.ActualHeight;
                bool intersects = w.Left < wa.Right && right > wa.Left && w.Top < wa.Bottom && bottom > wa.Top;
                sb.AppendLine($"window rect: {w.Left},{w.Top} -> {right},{bottom}");
                sb.AppendLine($"intersects workarea: {intersects}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"dump failed: {ex}");
            }

            try { File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8); } catch { }
            Shutdown();
        };

        timer.Start();
    }

    private void CaptureVisual(Window window, string path, Window? closeAfter, int delayMs = 1100)
    {
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(delayMs)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                window.UpdateLayout();
                int w = (int)Math.Max(1, window.ActualWidth);
                int h = (int)Math.Max(1, window.ActualHeight);
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);

                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var fs = File.Create(path);
                enc.Save(fs);
            }
            catch (Exception ex)
            {
                LogCrash(ex);
            }

            closeAfter?.Close();
            Shutdown();
        };

        timer.Start();
    }

    /// <summary>启动几秒后把提醒音预加载好，响的时候就不会慢半拍。</summary>
    private void PreloadSounds()
    {
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.5)
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                SoundService.Preload(Config.SoundFinish, Config.SoundVolume);
                SoundService.Preload(Config.SoundBreakEnd, Config.SoundVolume);
                SoundService.Preload(Config.SoundPreAlert, Config.SoundVolume);
            }
            catch { }
        };

        timer.Start();
    }

    /// <summary>监听"再启动一次"的信号，把窗口叫到前台。</summary>
    private void StartShowListener()
    {
        try
        {
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);

            var thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        _showEvent.WaitOne();
                        Dispatcher.BeginInvoke(new Action(() => MainWin?.ShowFromTray()));
                    }
                    catch
                    {
                        break;
                    }
                }
            })
            {
                IsBackground = true,
                Name = "MeowTimer-ShowListener"
            };
            thread.Start();
        }
        catch
        {
            // 拿不到事件就只靠托盘/热键
        }
    }

    private void OnHotkey(HotkeyAction action)
    {
        if (MainWin == null) return;

        switch (action)
        {
            case HotkeyAction.TogglePause: MainWin.TogglePause(); break;
            case HotkeyAction.Reset: MainWin.Reset(); break;
            case HotkeyAction.SetTimer: MainWin.OpenSetTimer(); break;
            case HotkeyAction.Clock: MainWin.ShowClock(); break;
            case HotkeyAction.ShowHide: MainWin.ToggleShow(); break;
            case HotkeyAction.Topmost: MainWin.ToggleTopmost(); break;
            case HotkeyAction.Pomodoro: MainWin.StartPomodoro(); break;
            case HotkeyAction.Cycle: MainWin.StartCycle(); break;
        }
    }

    public static void SaveConfig() => ConfigService.Save(Config);

    public static void ReloadHotkeys() => Hotkeys?.RegisterAll(Config);

    public static void RefreshTray() => Tray?.Refresh();

    private static void LogCrash(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            Directory.CreateDirectory(ConfigService.DataDir);
            File.AppendAllText(Path.Combine(ConfigService.DataDir, "error.log"),
                $"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====\r\n{ex}\r\n\r\n");
        }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        IsExiting = true;
        SaveConfig();
        Hotkeys?.Dispose();
        Tray?.Dispose();

        try { _mutex?.ReleaseMutex(); } catch { }
        _mutex?.Dispose();
        _mutex = null;

        try { _showEvent?.Dispose(); } catch { }
        _showEvent = null;

        base.OnExit(e);
    }
}
