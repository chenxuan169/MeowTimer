namespace MeowTimer.Models;

/// <summary>所有可持久化的用户设置。字段名即 JSON 键名。</summary>
public sealed class AppConfig
{
    // ---------- 外观 ----------
    public string Theme { get; set; } = "sakura";
    public double FontSize { get; set; } = 46;
    public string FontFamily { get; set; } = "Microsoft YaHei UI";
    public double CardOpacity { get; set; } = 0.62;   // 背景卡片透明度，0 = 全透明
    public string BackgroundStyle { get; set; } = "gradient";  // gradient | solid | none
    public string BackgroundColor { get; set; } = "#FF6F9C";    // 纯色背景用
    public string TextColor { get; set; } = "#FFFFFF";          // 文字颜色，独立于主题
    public bool Topmost { get; set; } = true;
    public bool LockPosition { get; set; } = false;   // 锁定窗口位置，拖不动
    public bool ShowCat { get; set; } = true;
    public bool ShowProgress { get; set; } = true;
    public bool ShowStatus { get; set; } = true;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;

    // ---------- 计时 ----------
    public int DefaultSeconds { get; set; } = 1500;   // 默认倒计时，精确到秒
    public bool FlashOnFinish { get; set; } = true;
    public bool StartTimerOnLaunch { get; set; } = false;

    // ---------- 番茄钟 ----------
    public int PomodoroWorkSeconds { get; set; } = 1500;
    public int PomodoroShortBreakSeconds { get; set; } = 300;
    public int PomodoroLongBreakSeconds { get; set; } = 900;
    public int PomodoroRoundsBeforeLong { get; set; } = 4;
    public bool PomodoroAutoNext { get; set; } = true;

    // ---------- 专注 · 休息循环 ----------
    public int CycleFocusSeconds { get; set; } = 2700;
    public int CycleBreakSeconds { get; set; } = 600;
    public bool CycleAutoNext { get; set; } = true;
    public int CycleRounds { get; set; } = 0;          // 0 = 一直循环

    // ---------- 提醒 ----------
    public bool SoundEnabled { get; set; } = true;
    public string SoundFinish { get; set; } = "builtin:meow";      // 专注 / 倒计时结束
    public string SoundBreakEnd { get; set; } = "builtin:soft";    // 休息结束
    public string SoundPreAlert { get; set; } = "builtin:notify";  // 提前提醒
    public string SoundDir { get; set; } = "";                     // 自定义音效目录，空 = 默认
    public int SoundVolume { get; set; } = 80;
    public bool NotifyEnabled { get; set; } = true;
    public bool PreAlertEnabled { get; set; } = false;
    public int PreAlertSeconds { get; set; } = 60;

    // ---------- 托盘 ----------
    public bool TrayIconEnabled { get; set; } = true;
    public string TrayMode { get; set; } = "cat";       // cat | cpu | mem

    // ---------- 系统 ----------
    public bool AutoStart { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public string Language { get; set; } = "zh";

    // ---------- 全局热键 ----------
    public string HotkeyTogglePause { get; set; } = "Ctrl+Alt+P";
    public string HotkeyReset { get; set; } = "Ctrl+Alt+R";
    public string HotkeySetTimer { get; set; } = "Alt+Shift+T";
    public string HotkeyClock { get; set; } = "Ctrl+Alt+S";
    public string HotkeyShowHide { get; set; } = "Ctrl+Alt+H";
    public string HotkeyTopmost { get; set; } = "Ctrl+Alt+O";
    public string HotkeyPomodoro { get; set; } = "Ctrl+Alt+D";
    public string HotkeyCycle { get; set; } = "Ctrl+Alt+B";

    // ---------- 旧版本遗留字段：只用于把老配置迁移到新的多音效方案 ----------
    public string? SoundMode { get; set; }
    public string? CustomSoundPath { get; set; }

    /// <summary>旧版"默认倒计时（分钟）"，迁移到 DefaultSeconds 后清空。</summary>
    public int? DefaultMinutes { get; set; }

    // 旧版用分钟存的各段时长，迁移成秒后清空
    public int? PomodoroWork { get; set; }
    public int? PomodoroShortBreak { get; set; }
    public int? PomodoroLongBreak { get; set; }
    public int? CycleFocusMinutes { get; set; }
    public int? CycleBreakMinutes { get; set; }
}
