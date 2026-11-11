using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MeowTimer.Services;

namespace MeowTimer.Views;

public partial class QuickInputWindow : Window
{
    private const int MaxSeconds = 99 * 3600 + 59 * 60 + 59;

    public double Seconds { get; private set; }

    public QuickInputWindow(int defaultSeconds)
    {
        InitializeComponent();

        TitleText.Text = L.T("input.title");
        UnitHour.Text = L.T("unit.hour");
        UnitMin.Text = L.T("unit.minute");
        UnitSec.Text = L.T("unit.second");
        CancelBtn.Content = L.T("btn.cancel");
        StartBtn.Content = L.T("input.start");

        SetFromSeconds(defaultSeconds > 0 ? defaultSeconds : 1500);

        HourBox.TextChanged += (_, _) => UpdateTotal();
        MinBox.TextChanged += (_, _) => UpdateTotal();
        SecBox.TextChanged += (_, _) => UpdateTotal();

        Loaded += (_, _) =>
        {
            MinBox.Focus();
            MinBox.SelectAll();
        };
    }

    private void SetFromSeconds(int total)
    {
        total = Math.Clamp(total, 0, MaxSeconds);
        HourBox.Text = (total / 3600).ToString();
        MinBox.Text = (total % 3600 / 60).ToString();
        SecBox.Text = (total % 60).ToString();
        UpdateTotal();
    }

    private static int BoxValue(TextBox box, int max)
    {
        if (!int.TryParse(box.Text, out var v) || v < 0) v = 0;
        if (v > max) v = max;
        return v;
    }

    private int ReadSeconds()
    {
        int h = BoxValue(HourBox, 99);
        int m = BoxValue(MinBox, 59);
        int s = BoxValue(SecBox, 59);
        return Math.Clamp(h * 3600 + m * 60 + s, 0, MaxSeconds);
    }

    private void UpdateTotal()
    {
        if (TotalText == null) return;

        int total = ReadSeconds();
        int h = total / 3600, m = total % 3600 / 60, s = total % 60;

        string text;
        if (h > 0) text = $"{h} {L.T("unit.hour")} {m} {L.T("unit.minute")} {s} {L.T("unit.second")}";
        else if (m > 0) text = $"{m} {L.T("unit.minute")} {s} {L.T("unit.second")}";
        else text = $"{s} {L.T("unit.second")}";

        TotalText.Text = "= " + text;
    }

    // ==================== 事件 ====================

    private void OnDragArea(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is TextBox) return;
        try { DragMove(); } catch { }
    }

    private void OnNumberInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void OnBoxFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box) box.SelectAll();
    }

    private void OnPreset(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && int.TryParse(b.Tag as string, out var sec))
        {
            SetFromSeconds(sec);
            MinBox.Focus();
        }
    }

    private void OnStart(object sender, RoutedEventArgs e)
    {
        int total = ReadSeconds();
        if (total <= 0)
        {
            TotalText.Text = L.T("input.invalid");
            return;
        }

        Seconds = total;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
