using System.Collections.ObjectModel;
using System.Globalization;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class HomePage : ContentPage
{
    public ObservableCollection<StatItem> StatsList { get; set; } = new();
    public ObservableCollection<DeadlineViewModel> DeadlinesList { get; set; } = new();

    private string currentPeriod = "Тиждень";

    public HomePage()
    {
        InitializeComponent();
        StatsCollection.ItemsSource = StatsList;
        DeadlinesCollection.ItemsSource = DeadlinesList;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateDashboardData();
        UpdateDeadlinesData();
        UpdateStreakData(); // Оновлюємо блок стріку при кожному появі сторінки
    }

    private void OnPeriodChanged(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            currentPeriod = button.Text;
            BtnDay.BackgroundColor = currentPeriod == "День" ? Color.FromArgb("#512BD4") : Color.FromArgb("#2C2C2C");
            BtnWeek.BackgroundColor = currentPeriod == "Тиждень" ? Color.FromArgb("#512BD4") : Color.FromArgb("#2C2C2C");
            BtnMonth.BackgroundColor = currentPeriod == "Місяць" ? Color.FromArgb("#512BD4") : Color.FromArgb("#2C2C2C");

            UpdateDashboardData();
        }
    }

    private void UpdateDashboardData()
    {
        StatsList.Clear();

        // 1. Фільтруємо сесії залежно від обраного періоду
        var filteredSessions = FilterSessionsByPeriod(FocusPage.allSessions, currentPeriod);

        int totalMinutes = 0;
        var subjectGrouped = filteredSessions
            .GroupBy(s => s.Subject)
            .Select(g => new { Subject = g.Key, TotalMinutes = g.Sum(s => s.Minutes) })
            .OrderByDescending(x => x.TotalMinutes);

        foreach (var item in subjectGrouped)
        {
            totalMinutes += item.TotalMinutes;
            StatsList.Add(new StatItem
            {
                Subject = item.Subject,
                TimeFormatted = FormatMinutes(item.TotalMinutes)
            });
        }

        int totalSessionsCount = filteredSessions.Count;

        // Заповнюємо UI показників
        LblTotalTime.Text = FormatMinutes(totalMinutes);
        LblTotalSessions.Text = totalSessionsCount.ToString();

        int avgMinutes = totalSessionsCount > 0 ? totalMinutes / totalSessionsCount : 0;
        LblAvgSession.Text = $"{avgMinutes} хв";

        // Генерація графіку тижня (графік незмінно показує тижневу активність)
        GenerateChart();
    }

    // Метод фільтрації сесій за кнопками періоду
    private List<StudySession> FilterSessionsByPeriod(List<StudySession> sessions, string period)
    {
        var today = DateTime.Today;

        return period switch
        {
            "День" => sessions.Where(s => s.Date.Date == today).ToList(),

            "Тиждень" => sessions.Where(s => s.Date.Date >= today.AddDays(-6) && s.Date.Date <= today).ToList(),

            "Місяць" => sessions.Where(s => s.Date.Month == today.Month && s.Date.Year == today.Year).ToList(),

            _ => sessions
        };
    }

    private void UpdateDeadlinesData()
    {
        DeadlinesList.Clear();

        var activeTasks = TasksPage.Tasks
            .Where(t => !t.IsCompleted)
            .ToList();

        foreach (var task in activeTasks)
        {
            string daysLeftStr = "Без дедлайну";
            string icon = "📌";
            Color color = Color.FromArgb("#B0B0B0");
            int sortPriority = 0;
            DateTime targetDate = DateTime.MaxValue;

            if (!string.IsNullOrWhiteSpace(task.Deadline) && task.Deadline != "Не вказано")
            {
                if (DateTime.TryParseExact(task.Deadline.Trim() + ".2026", "dd.MM.yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out targetDate))
                {
                    var today = DateTime.Today;
                    int daysDiff = (targetDate.Date - today).Days;

                    if (daysDiff < 0)
                    {
                        daysLeftStr = $"Прострочено на {Math.Abs(daysDiff)} дн.";
                        icon = "❌";
                        color = Color.FromArgb("#FF5252");
                        sortPriority = -1;
                    }
                    else if (daysDiff == 0)
                    {
                        daysLeftStr = "Сьогодні";
                        icon = "⚠";
                        color = Color.FromArgb("#FF5252");
                        sortPriority = 0;
                    }
                    else if (daysDiff == 1)
                    {
                        daysLeftStr = "Завтра";
                        icon = "🟡";
                        color = Color.FromArgb("#FFB74D");
                        sortPriority = 1;
                    }
                    else
                    {
                        daysLeftStr = $"Через {daysDiff} дн.";
                        icon = "🟢";
                        color = Color.FromArgb("#81C784");
                        sortPriority = 2;
                    }
                }
            }
            else
            {
                sortPriority = -2;
            }

            DeadlinesList.Add(new DeadlineViewModel
            {
                DisplayTitle = $"{task.Subject} — {task.Title}",
                DeadlineText = task.Deadline,
                DaysLeftString = daysLeftStr,
                IndicatorIcon = icon,
                IndicatorColor = color,
                TargetDate = targetDate,
                SortPriority = sortPriority
            });
        }

        var sorted = DeadlinesList
            .OrderBy(d => d.SortPriority)
            .ThenBy(d => d.TargetDate)
            .ToList();

        DeadlinesList.Clear();
        foreach (var item in sorted)
        {
            DeadlinesList.Add(item);
        }
    }

    // МЕХАНІКА СТРІКУ ТА ДНІВ ТИЖНЯ
    private void UpdateStreakData()
    {
        if (WeekDaysLayout == null || LblStreakCount == null) return;

        WeekDaysLayout.Children.Clear();

        var today = DateTime.Today;
        int currentStreak = 0;

        const int minMinutesForStreak = 5;

        var dailyTotals = FocusPage.allSessions
            .GroupBy(s => s.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Minutes));

        var checkDate = dailyTotals.ContainsKey(today) && dailyTotals[today] >= minMinutesForStreak
            ? today
            : today.AddDays(-1);

        while (dailyTotals.ContainsKey(checkDate) && dailyTotals[checkDate] >= minMinutesForStreak)
        {
            currentStreak++;
            checkDate = checkDate.AddDays(-1);
        }

        // Виводимо число з правильним відмінюванням слова «день»
        string dayWord = GetDayWord(currentStreak);
        LblStreakCount.Text = $"Серія: {currentStreak} {dayWord} поспіль";

        // Малюємо дні поточного тижня (Пн - Нд)
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        var monday = today.AddDays(-diff);

        string[] dayNames = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Нд" };

        for (int i = 0; i < 7; i++)
        {
            var targetDay = monday.AddDays(i);
            bool isDone = dailyTotals.ContainsKey(targetDay) && dailyTotals[targetDay] >= minMinutesForStreak;

            string statusIcon = isDone ? "✅" : "—";

            var dayStack = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center };

            dayStack.Children.Add(new Label
            {
                Text = dayNames[i],
                TextColor = Color.FromArgb("#B0B0B0"),
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Center
            });

            dayStack.Children.Add(new Label
            {
                Text = statusIcon,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center
            });

            WeekDaysLayout.Children.Add(dayStack);
        }
    }

    // Допоміжний метод для правильного відмінювання слова «день»
    private string GetDayWord(int count)
    {
        int mod10 = count % 10;
        int mod100 = count % 100;

        if (mod100 >= 11 && mod100 <= 14)
            return "днів";
        if (mod10 == 1)
            return "день";
        if (mod10 >= 2 && mod10 <= 4)
            return "дні";
        return "днів";
    }

    private void GenerateChart()
    {
        ChartLayout.Children.Clear();

        var today = DateTime.Today;
        Dictionary<string, int> weeklyStats = new()
        {
            { "Пн", 0 }, { "Вт", 0 }, { "Ср", 0 }, { "Чт", 0 }, { "Пт", 0 }, { "Сб", 0 }, { "Нд", 0 }
        };

        var recentSessions = FocusPage.allSessions
            .Where(s => s.Date.Date >= today.AddDays(-6) && s.Date.Date <= today);

        foreach (var session in recentSessions)
        {
            string dayKey = session.Date.DayOfWeek switch
            {
                DayOfWeek.Monday => "Пн",
                DayOfWeek.Tuesday => "Вт",
                DayOfWeek.Wednesday => "Ср",
                DayOfWeek.Thursday => "Чт",
                DayOfWeek.Friday => "Пт",
                DayOfWeek.Saturday => "Сб",
                DayOfWeek.Sunday => "Нд",
                _ => "Пн"
            };

            if (weeklyStats.ContainsKey(dayKey))
                weeklyStats[dayKey] += session.Minutes;
        }

        // Знаходимо максимальне значення за тиждень для пропорційного масштабування
        int maxMinutes = weeklyStats.Values.Max();

        foreach (var day in weeklyStats)
        {
            int blocksCount = 0;
            if (day.Value > 0)
            {
                if (maxMinutes > 0)
                {
                    // Пропорція відносно найпродуктивнішого дня (максимум 10 блоків)
                    blocksCount = (int)Math.Round((double)day.Value / maxMinutes * 10);
                    // Навіть за невеликої активності показуємо мінімум 1 блок
                    blocksCount = Math.Max(1, blocksCount);
                }
                else
                {
                    blocksCount = 1;
                }
            }

            string blocks = blocksCount > 0 ? new string('█', blocksCount) : "—";
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

public class DeadlineViewModel
{
    public string DisplayTitle { get; set; }
    public string DeadlineText { get; set; }
    public string DaysLeftString { get; set; }
    public string IndicatorIcon { get; set; }
    public Color IndicatorColor { get; set; }
    public DateTime TargetDate { get; set; }
    public int SortPriority { get; set; }
}