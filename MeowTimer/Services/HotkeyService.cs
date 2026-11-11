using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace MeowTimer.Services;

public enum HotkeyAction
{
    TogglePause,
    Reset,
    SetTimer,
    Clock,
    ShowHide,
    Topmost,
    Pomodoro,
    Cycle
}

public sealed class HotkeyService : IDisposable
{
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    private HwndSource? _source;
    private IntPtr _hwnd = IntPtr.Zero;
    private int _nextId = 0x4D00;
    private readonly Dictionary<int, HotkeyAction> _map = new();
    private readonly List<int> _ids = new();

    public event Action<HotkeyAction>? Pressed;

    /// <summary>上一次 RegisterAll 成功注册的数量。</summary>
    public int LastRegisteredCount { get; private set; }

    /// <summary>上一次 RegisterAll 注册失败的快捷键。</summary>
    public List<string> LastFailedKeys { get; } = new();

    public void Attach(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.EnsureHandle();
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_map.TryGetValue(id, out var action))
            {
                Pressed?.Invoke(action);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public bool TryRegister(string? spec, HotkeyAction action)
    {
        if (string.IsNullOrWhiteSpace(spec)) return false;
        if (!TryParse(spec, out uint mods, out uint vk)) return false;

        int id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_hwnd, id, mods | MOD_NOREPEAT, vk))
            return false;

        _map[id] = action;
        _ids.Add(id);
        return true;
    }

    /// <summary>注册全部快捷键，返回成功注册的数量。</summary>
    public int RegisterAll(MeowTimer.Models.AppConfig cfg)
    {
        UnregisterAll();
        LastFailedKeys.Clear();
        int ok = 0;

        void Try(string? spec, HotkeyAction action)
        {
            if (string.IsNullOrWhiteSpace(spec)) return;
            if (TryRegister(spec, action)) ok++;
            else LastFailedKeys.Add(spec);
        }

        Try(cfg.HotkeyTogglePause, HotkeyAction.TogglePause);
        Try(cfg.HotkeyReset, HotkeyAction.Reset);
        Try(cfg.HotkeySetTimer, HotkeyAction.SetTimer);
        Try(cfg.HotkeyClock, HotkeyAction.Clock);
        Try(cfg.HotkeyShowHide, HotkeyAction.ShowHide);
        Try(cfg.HotkeyTopmost, HotkeyAction.Topmost);
        Try(cfg.HotkeyPomodoro, HotkeyAction.Pomodoro);
        Try(cfg.HotkeyCycle, HotkeyAction.Cycle);

        LastRegisteredCount = ok;
        return ok;
    }

    private static bool TryParse(string spec, out uint mods, out uint vk)
    {
        mods = 0;
        vk = 0;
        var parts = spec.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        string keyPart = "";
        foreach (var p in parts)
        {
            switch (p.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    mods |= MOD_CONTROL; break;
                case "alt":
                    mods |= MOD_ALT; break;
                case "shift":
                    mods |= MOD_SHIFT; break;
                case "win":
                case "windows":
                    mods |= MOD_WIN; break;
                default:
                    keyPart = p; break;
            }
        }

        if (string.IsNullOrEmpty(keyPart)) return false;
        if (!Enum.TryParse<Key>(keyPart, true, out var key)) return false;
        var k = key == Key.None ? Key.None : key;
        vk = (uint)KeyInterop.VirtualKeyFromKey(k);
        return vk != 0;
    }

    public void UnregisterAll()
    {
        if (_hwnd != IntPtr.Zero)
        {
            foreach (var id in _ids)
                NativeMethods.UnregisterHotKey(_hwnd, id);
        }
        _ids.Clear();
        _map.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
        _source?.RemoveHook(WndProc);
        _source = null;
    }
}
