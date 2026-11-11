using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MeowTimer.Services;

namespace MeowTimer.Views;

public partial class StatsWindow : Window
{
    public StatsWindow()
    {
        InitializeComponent();
        Localize();
        Render();
    }

    private void Localize()
    {
        TitleText.Text = L.T("stats.title");
        TodayPomoLabel.Text = L.T("stats.pomodoros");
        TodayFocusLabel.Text = L.T("stats.focusTime");
        TotalFocusLabel.Text = L.T("stats.total");
        TrendLabel.Text = L.T("stats.trend");
        EmptyText.Text = L.T("stats.empty");
        CloseBtn.Content = L.T("btn.close");
    }

    private void Render()
    {
        var today = StatsService.GetDay(DateTime.Now);
        var total = StatsService.Total();

        TodayPomo.Text = today.Pomodoros.ToString();
        TodayFocus.Text = FormatMinutes(today.FocusMinutes);
        TotalFocus.Text = FormatMinutes(total.minutes);

        RenderChart();
    }

    private static string FormatMinutes(int minutes)
    {
        if (minutes <= 0) return "0m";
        if (minutes < 60) return $"{minutes}m";

        double hours = minutes / 60.0;
        if (hours >= 10) return $"{hours:0.#}h";

        int h = minutes / 60, m = minutes % 60;
        return m == 0 ? $"{h}h" : $"{h}h{m}m";
    }

    private void RenderChart()
    {
        ChartGrid.Children.Clear();
        ChartGrid.ColumnDefinitions.Clear();

        var days = StatsService.Last7Days();
        int max = Math.Max(1, days.Max(d => d.minutes));
        bool any = days.Any(d => d.minutes > 0);
        EmptyText.Visibility = any ? Visibility.Collapsed : Visibility.Visible;

        var uiFont = new FontFamily("Microsoft YaHei UI");
        var valueFg = new SolidColorBrush(ThemeService.C("#B08C9C"));
        var labelFg = new SolidColorBrush(ThemeService.C("#B9A6B0"));
        var todayFg = new SolidColorBrush(ThemeService.C("#FF6F9C"));
        var emptyBg = new SolidColorBrush(ThemeService.C("#F6EAF1"));

        for (int i = 0; i < days.Length; i++)
        {
            var (date, minutes) = days[i];
            ChartGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var col = new StackPanel { VerticalAlignment = VerticalAlignment.Bottom };

            col.Children.Add(new TextBlock
            {
                Text = minutes > 0 ? minutes.ToString() : "",
                FontSize = 10,
                Foreground = valueFg,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5),
                FontFamily = uiFont
            });

            var holder = new Grid { Height = 150 };
            var bar = new Border
            {
                Height = minutes > 0 ? Math.Max(8, 150.0 * minutes / max) : 4,
                VerticalAlignment = VerticalAlignment.Bottom,
                CornerRadius = new CornerRadius(8, 8, 5, 5),
                Margin = new Thickness(9, 0, 9, 0),
                Background = minutes > 0
                    ? new LinearGradientBrush(ThemeService.C("#FF7FA8"), ThemeService.C("#FFC2D8"), 90)
                    : emptyBg
            };

            holder.Children.Add(bar);
            col.Children.Add(holder);

            bool isToday = date.Date == DateTime.Today;
            col.Children.Add(new TextBlock
            {
                Text = isToday ? L.T("stats.today") : date.ToString("M/d"),
                FontSize = 10,
                Foreground = isToday ? todayFg : labelFg,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0),
                FontFamily = uiFont,
                FontWeight = isToday ? FontWeights.SemiBold : FontWeights.Normal
            });

            Grid.SetColumn(col, i);
            ChartGrid.Children.Add(col);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnDragArea(object sender, MouseButtonEventArgs e)
    {
        try { DragMove(); } catch { }
    }
}
