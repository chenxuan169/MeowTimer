namespace MeowTimer.Models;

public enum TimerMode
{
    Countdown,
    Stopwatch,
    Clock,
    Pomodoro,
    Cycle
}

public enum PomodoroPhase
{
    Work,
    ShortBreak,
    LongBreak
}

/// <summary>专注 · 休息循环的两个阶段。</summary>
public enum CyclePhase
{
    Focus,
    Break
}
