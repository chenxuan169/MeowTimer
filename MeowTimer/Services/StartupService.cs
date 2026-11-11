using System.Diagnostics;
using Microsoft.Win32;

namespace MeowTimer.Services;

public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MeowTimer";

    public static string ExePath
    {
        get
        {
            try
            {
                return Process.GetCurrentProcess().MainModule?.FileName ?? "";
            }
            catch
            {
                return "";
            }
        }
    }

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            var v = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(v);
        }
        catch
        {
            return false;
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;

            if (enabled)
            {
                var exe = ExePath;
                if (string.IsNullOrEmpty(exe)) return;
                key.SetValue(ValueName, $"\"{exe}\" --minimized");
            }
            else
            {
                if (key.GetValue(ValueName) != null)
                    key.DeleteValue(ValueName, false);
            }
        }
        catch
        {
            // 权限不足时静默失败
        }
    }
}
