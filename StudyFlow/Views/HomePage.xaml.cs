using System.Collections.ObjectModel;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class HomePage : ContentPage
{
    public ObservableCollection<StatItem> StatsList { get; set; } = new();

    // Зберігаємо обраний період ("День", "Тиждень", "Місяць")
    private string currentPeriod = "Тиждень";

    public HomePage()
    {
        InitializeComponent();
        StatsCollection.ItemsSource = StatsList;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateDashboardData();
    }

    // Обробка перемикача періодів (День / Тиждень / Місяць)
    private void OnPeriodChanged(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            currentPeriod = button.Text;

            // Змінюємо візуальний стиль кнопок (активна підсвічується фіолетовим)
            BtnDay.BackgroundColor = currentPeriod == "День" ? Color.FromArgb("#512BD4") : Color.FromArgb("#2C2C2C");
            BtnWeek.BackgroundColor = currentPeriod == "Тиждень" ? Color.FromArgb("#512BD4") : Color.FromArgb("#2C2C2C");
            BtnMonth.BackgroundColor = currentPeriod == "Місяць" ? Color.FromArgb("#512BD4") : Color.FromArgb("#2C2C2C");

            UpdateDashboardData();
        }
    }

    // Головний метод оновлення всієї статистики на сторінці
    private void UpdateDashboardData()
    {
        StatsList.Clear();

        int totalMinutes = 0;

        // Рахуємо сумарний час з усіх предметів
        foreach (var pair in FocusPage.subjectStats)
        {
            totalMinutes += pair.Value;
            StatsList.Add(new StatItem
            {
                Subject = pair.Key,
                TimeFormatted = FormatMinutes(pair.Value)
            });
        }

        // Кількість сесій (можеш налаштувати під себе, наприклад, кількість записів у словнику або фіксоване число)
        int totalSessions = FocusPage.subjectStats.Count > 0 ? FocusPage.subjectStats.Values.Count * 2 : 0;

        // Заповнюємо UI показників
        LblTotalTime.Text = FormatMinutes(totalMinutes);
        LblTotalSessions.Text = totalSessions.ToString();

        int avgMinutes = totalSessions > 0 ? totalMinutes / totalSessions : 0;
        LblAvgSession.Text = $"{avgMinutes} хв";

        // Генерація графіку з урахуванням реального часу
        GenerateChart(totalMinutes);
    }

    // Генерація блокового графіку на зразок Пн ███████
    private void GenerateChart(int baseMinutes)
    {
        ChartLayout.Children.Clear();

        // Беремо реальні дані з FocusPage.weeklyStats по днях тижня
        var days = FocusPage.weeklyStats;

        foreach (var day in days)
        {
            // Якщо за цей день є хвилини, малюємо блоки (1 блок = 1 хвилина або налаштуй масштаб, наприклад / 5)
            int blocksCount = day.Value > 0 ? Math.Max(1, day.Value / 5) : 0;
            string blocks = blocksCount > 0 ? new string('█', Math.Min(blocksCount, 12)) : "—";
            Color textColor = blocksCount > 0 ? Color.FromArgb("#512BD4") : Color.FromArgb("#555555");

            var rowLayout = new HorizontalStackLayout { Spacing = 10 };

            rowLayout.Children.Add(new Label
            {
                Text = day.Key,
                TextColor = Color.FromArgb("#B0B0B0"),
                WidthRequest = 30,
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center
            });

            rowLayout.Children.Add(new Label
            {
                Text = blocks,
                TextColor = textColor,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center
            });

            ChartLayout.Children.Add(rowLayout);
        }
    }

    private string FormatMinutes(int totalMinutes)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (hours > 0 && minutes > 0) return $"{hours} год {minutes} хв";
        if (hours > 0) return $"{hours} год";
        return $"{minutes} хв";
    }
}