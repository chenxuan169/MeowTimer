using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MeowTimerMac.Views;

public partial class SetTimerWindow : Window
{
    public int Seconds { get; private set; }

    public SetTimerWindow() : this(1500) { }

    public SetTimerWindow(int totalSeconds)
    {
        InitializeComponent();
        SetFrom(totalSeconds);
    }

    private void SetFrom(int total)
    {
        total = Math.Max(0, total);
        HourBox.Value = (decimal)(total / 3600);
        MinBox.Value = (decimal)(total % 3600 / 60);
        SecBox.Value = (decimal)(total % 60);
    }

    private int Read()
    {
        int h = (int)(HourBox.Value ?? 0);
        int m = (int)(MinBox.Value ?? 0);
        int s = (int)(SecBox.Value ?? 0);
        return Math.Max(0, h * 3600 + m * 60 + s);
    }

    private void OnPreset(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && int.TryParse(b.Tag as string, out var sec)) SetFrom(sec);
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        int total = Read();
        if (total <= 0) return;

        Seconds = total;
        Close(true);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
