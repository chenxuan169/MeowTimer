namespace MeowTimer.Services;

/// <summary>极简双语本地化：中文 / English。</summary>
public static class L
{
    public static string Lang { get; set; } = "zh";

    private static readonly Dictionary<string, (string zh, string en)> Map = new()
    {
        // ---- 托盘菜单 ----
        ["menu.start"] = ("开始", "Start"),
        ["menu.pause"] = ("暂停", "Pause"),
        ["menu.resume"] = ("继续", "Resume"),
        ["menu.reset"] = ("重置", "Reset"),
        ["menu.setTimer"] = ("设置倒计时…", "Set Countdown…"),
        ["menu.pomodoro"] = ("番茄钟", "Pomodoro"),
        ["menu.cycle"] = ("专注 · 休息循环", "Focus–Break Cycle"),
        ["menu.stopwatch"] = ("开始正计时", "Start Stopwatch"),
        ["menu.clock"] = ("显示当前时间", "Show Clock"),
        ["menu.topmost"] = ("窗口置顶", "Always on Top"),
        ["menu.recenter"] = ("窗口移回当前屏幕", "Move window to this screen"),
        ["menu.lock"] = ("锁定窗口位置", "Lock window position"),
        ["menu.unlock"] = ("解锁窗口位置", "Unlock window position"),
        ["menu.editOn"] = ("✎ 进入编辑模式", "✎ Enter edit mode"),
        ["menu.editOff"] = ("✓ 退出编辑模式", "✓ Exit edit mode"),
        ["menu.settings"] = ("设置…", "Settings…"),
        ["menu.stats"] = ("统计…", "Statistics…"),
        ["menu.hide"] = ("隐藏窗口", "Hide Window"),
        ["menu.show"] = ("显示窗口", "Show Window"),
        ["menu.exit"] = ("退出", "Exit"),
        ["menu.mode"] = ("模式", "Mode"),
        ["menu.quick"] = ("快捷时长", "Quick Presets"),

        // ---- 状态文字 ----
        ["status.focus"] = ("专注中", "Focusing"),
        ["status.paused"] = ("已暂停", "Paused"),
        ["status.breakShort"] = ("短休息", "Short Break"),
        ["status.breakLong"] = ("长休息", "Long Break"),
        ["status.ready"] = ("点一下开始", "Tap to start"),
        ["status.done"] = ("时间到啦", "Time's up"),
        ["status.pomodoroCount"] = ("第 {0} 个番茄", "Pomodoro #{0}"),
        ["status.break"] = ("休息中", "On a break"),
        ["status.cycleRound"] = ("第 {0} 轮", "Round {0}"),
        ["status.idle"] = ("已重置", "Reset"),
        ["status.editing"] = ("编辑中 · 滚轮调时间", "EDITING · wheel to adjust"),

        // ---- 通知 ----
        ["notify.finish"] = ("喵～时间到！", "Meow~ Time's up!"),
        ["notify.finishBody"] = ("起来动一动吧", "Stand up and stretch"),
        ["notify.workDone"] = ("专注结束，休息一下", "Focus done, take a break"),
        ["notify.workDoneBody"] = ("休息 {0} 分钟，喵会叫你", "{0} min break, I'll meow for you"),
        ["notify.breakDone"] = ("休息结束啦", "Break is over"),
        ["notify.breakDoneBody"] = ("新一轮专注开始吧！", "Let's focus again!"),
        ["notify.preAlert"] = ("只剩 {0} 秒了", "Only {0}s left"),
        ["notify.preAlertTitle"] = ("倒计时提醒", "Timer alert"),
        ["notify.restTime"] = ("休息时间到！", "Break time!"),
        ["notify.restTimeBody"] = ("休息 {0} 分钟，结束了喵会叫你回来", "Take {0} min — I'll call you back"),
        ["notify.restOver"] = ("休息时间结束", "Break's over"),
        ["notify.restOverBody"] = ("回来啦，开始下一轮专注吧", "Welcome back — next round"),
        ["notify.cycleDone"] = ("循环完成啦", "Cycle complete"),
        ["notify.cycleDoneBody"] = ("已完成 {0} 轮，好好休息一下吧", "{0} rounds done. Take a real break!"),

        // ---- 设置分节 ----
        ["sec.appearance"] = ("外观", "Appearance"),
        ["preview.label"] = ("实时预览", "Live preview"),
        ["sec.timer"] = ("计时", "Timer"),
        ["sec.pomodoro"] = ("番茄钟", "Pomodoro"),
        ["sec.cycle"] = ("专注 · 休息循环", "Focus / Break"),
        ["sec.alert"] = ("提醒", "Alerts"),
        ["sec.system"] = ("系统", "System"),
        ["sec.hotkey"] = ("全局快捷键", "Global Hotkeys"),

        // ---- 设置项 ----
        ["lbl.theme"] = ("配色主题", "Theme"),
        ["lbl.fontSize"] = ("字号", "Font size"),
        ["lbl.font"] = ("字体", "Font"),
        ["lbl.cardOpacity"] = ("背景不透明度", "Background opacity"),
        ["lbl.bgStyle"] = ("背景样式", "Background"),
        ["lbl.bgColor"] = ("背景颜色", "Color"),
        ["lbl.textColor"] = ("文字颜色", "Text color"),
        ["lbl.followTheme"] = ("跟随主题", "Follow theme"),
        ["lbl.bgGradient"] = ("渐变（随主题）", "Gradient"),
        ["lbl.bgSolid"] = ("纯色", "Solid"),
        ["lbl.bgNone"] = ("无背景（只留数字）", "None (text only)"),
        ["lbl.showCat"] = ("显示猫咪", "Show the cat"),
        ["lbl.showProgress"] = ("显示进度条", "Show progress bar"),
        ["lbl.showStatus"] = ("显示状态文字", "Show status text"),
        ["lbl.topmost"] = ("窗口置顶", "Always on top"),
        ["lbl.flash"] = ("结束时闪烁", "Flash when finished"),
        ["lbl.startTimerOnLaunch"] = ("启动后自动开始倒计时", "Auto start countdown on launch"),
        ["lbl.defaultMinutes"] = ("默认倒计时", "Default countdown"),
        ["lbl.work"] = ("工作时长", "Work"),
        ["lbl.shortBreak"] = ("短休息", "Short break"),
        ["lbl.longBreak"] = ("长休息", "Long break"),
        ["lbl.rounds"] = ("长休息前番茄数", "Rounds before long break"),
        ["lbl.autoNext"] = ("自动进入下一阶段", "Auto next phase"),
        ["lbl.sound"] = ("开启提示音", "Enable sound"),
        ["lbl.soundMode"] = ("音效", "Sound"),
        ["lbl.volume"] = ("音量", "Volume"),
        ["lbl.notify"] = ("系统通知气泡", "System notification"),
        ["lbl.preAlert"] = ("提前提醒", "Pre-alert"),
        ["lbl.preSeconds"] = ("提前几秒", "Seconds before"),
        ["lbl.autoStart"] = ("开机自动启动", "Launch at startup"),
        ["lbl.startMin"] = ("启动后最小化到托盘", "Start minimized to tray"),
        ["lbl.language"] = ("界面语言", "Language"),
        ["lbl.trayMode"] = ("托盘图标显示", "Tray icon shows"),
        ["lbl.trayCat"] = ("猫咪", "Cat"),
        ["lbl.trayCpu"] = ("CPU 占用", "CPU usage"),
        ["lbl.trayMem"] = ("内存占用", "Memory usage"),
        ["lbl.soundMeow"] = ("喵叫", "Meow"),
        ["lbl.soundSystem"] = ("系统提示音", "System beep"),
        ["lbl.soundCustom"] = ("自定义音频…", "Custom file…"),
        ["lbl.focusLen"] = ("专注时长", "Focus"),
        ["lbl.breakLen"] = ("休息时长", "Break"),
        ["lbl.cycleRounds"] = ("循环轮数（0 = 不限）", "Rounds (0 = endless)"),
        ["lbl.soundFinish"] = ("专注结束音效", "Focus-end sound"),
        ["lbl.soundBreak"] = ("休息结束音效", "Break-end sound"),
        ["lbl.soundPre"] = ("提前提醒音效", "Pre-alert sound"),
        ["lbl.soundDir"] = ("音效目录", "Sound folder"),
        ["btn.changeDir"] = ("更改", "Change"),
        ["btn.open"] = ("打开", "Open"),
        ["btn.resetDir"] = ("恢复默认目录", "Reset folder"),
        ["drop.hint"] = ("把 mp3 / wav 文件拖到这里，自动加进音效库",
                         "Drop mp3 / wav here to add it"),
        ["drop.ok"] = ("已加入 {0} 个音效 ✓", "Added {0} sound(s) ✓"),
        ["drop.fail"] = ("没认出来，只支持 wav / mp3 / m4a / wma / flac",
                         "Unsupported — use wav / mp3 / m4a / wma / flac"),
        ["lbl.lock"] = ("锁定窗口位置", "Lock position"),

        // ---- 通用按钮 ----
        ["btn.ok"] = ("保存", "Save"),
        ["btn.cancel"] = ("取消", "Cancel"),
        ["btn.close"] = ("关闭", "Close"),
        ["btn.browse"] = ("浏览…", "Browse…"),
        ["btn.openFolder"] = ("打开文件夹", "Open folder"),
        ["btn.preview"] = ("试听", "Try"),
        ["btn.reset"] = ("恢复默认", "Reset to default"),

        // ---- 统计 ----
        ["stats.title"] = ("专注统计", "Statistics"),
        ["stats.today"] = ("今日", "Today"),
        ["stats.week"] = ("本周", "This week"),
        ["stats.month"] = ("本月", "This month"),
        ["stats.pomodoros"] = ("番茄", "Pomodoros"),
        ["stats.focusTime"] = ("专注时长", "Focus time"),
        ["stats.trend"] = ("近 7 天专注（分钟）", "Last 7 days (min)"),
        ["stats.total"] = ("累计专注", "All-time"),
        ["stats.empty"] = ("还没有记录，先去专注一下吧～", "No records yet — go focus!"),

        // ---- 输入窗 ----
        ["input.title"] = ("要专注多久？", "How long?"),
        ["input.hint"] = ("可输入 25、25m、1h30m、90s", "Try 25, 25m, 1h30m, 90s"),
        ["input.start"] = ("开始", "Start"),
        ["input.invalid"] = ("这个时间看不懂呢", "Can't parse that time"),

        // ---- 其它 ----
        ["tray.tip"] = ("喵时 · 左键显示 / 右键菜单", "MeowTimer · click to show, right-click for menu"),
        ["tip.edit"] = ("滚轮 ±1 分钟 · Shift+滚轮 ±10 秒 · Ctrl+滚轮 ±5 分钟 · Alt+滚轮 透明度 · 双击直接输入 · Esc 退出",
                        "Wheel ±1min · Shift ±10s · Ctrl ±5min · Alt opacity · Double-click to type · Esc to exit"),
        ["hint.wheel"] = ("滚轮调时间 · Ctrl+滚轮字号 · Shift+滚轮透明度 · 拖动移动", "Wheel: time · Ctrl+Wheel: size · Shift+Wheel: opacity · drag to move"),
        ["about.title"] = ("喵时 MeowTimer", "MeowTimer"),
        ["about.desc"] = ("一个很轻的桌面倒计时 · 番茄钟", "A tiny desktop timer & pomodoro"),
        ["seconds"] = ("秒", "s"),
        ["unit.hour"] = ("时", "h"),
        ["unit.minute"] = ("分", "min"),
        ["unit.second"] = ("秒", "s"),
        ["unit.count"] = ("个", ""),
        ["unit.round"] = ("轮", ""),
    };

    public static string T(string key)
    {
        if (Map.TryGetValue(key, out var v)) return Lang == "en" ? v.en : v.zh;
        return key;
    }

    public static string T(string key, params object[] args)
    {
        var s = T(key);
        try { return string.Format(s, args); }
        catch { return s; }
    }
}
