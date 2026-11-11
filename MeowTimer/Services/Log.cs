using System;
using System.IO;

namespace MeowTimer.Services;

/// <summary>轻量诊断日志（只在排查问题时写入 %LOCALAPPDATA%\MeowTimer\debug.log）。</summary>
public static class Log
{
    private static readonly object Gate = new();
    private static string? _path;

    private static string Path_
    {
        get
        {
            if (_path != null) return _path;

            var dir = Environment.GetEnvironmentVariable("MEOWTIMER_DATA");
            if (string.IsNullOrWhiteSpace(dir))
                dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeowTimer");

            Directory.CreateDirectory(dir);
            _path = System.IO.Path.Combine(dir, "debug.log");
            return _path;
        }
    }

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                File.AppendAllText(Path_, $"{DateTime.Now:MM-dd HH:mm:ss.fff}  {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // 日志失败不影响使用
        }
    }

    public static void Clear()
    {
        try
        {
            lock (Gate) File.WriteAllText(Path_, "");
        }
        catch { }
    }
}
