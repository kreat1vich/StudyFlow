using System.Collections.ObjectModel;
using System.Text.Json;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class HomePage : ContentPage
{
    private bool isTimerRunning = false;
    private bool isPaused = false;
    private int sessionMinutes = 0;
    private int elapsedSeconds = 0; // Скільки секунд вже реально пройшло
    private int totalSeconds = 0;   // Загальна тривалість сесії в секундах

    private const string StatsStorageKey = "saved_study_stats";

    // Словник для збереження часу по предметах
    private static Dictionary<string, int> subjectStats = new();

    public ObservableCollection<StatItem> StatsList { get; set; } = new();

    public HomePage()
    {
        InitializeComponent();
        LoadStats(); // Завантажуємо збережену статистику

        StatsCollection.ItemsSource = StatsList;
        UpdateStatsUI();
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleInput.Text) || string.IsNullOrWhiteSpace(SubjectInput.Text))
        {
            DisplayAlert("Помилка", "Заповніть назву та опис завдання!", "ОК");
            return;
        }

        TasksPage.Tasks.Add(new TaskItem
        {
            Title = TitleInput.Text,
            Subject = SubjectInput.Text,
            Deadline = string.IsNullOrWhiteSpace(DeadlineInput.Text) ? "Не вказано" : DeadlineInput.Text,
            IsCompleted = false
        });

        // Зберігаємо оновлений список завдань у пам'ять
        TasksPage.SaveTasks();

        TitleInput.Text = string.Empty;
        SubjectInput.Text = string.Empty;
        DeadlineInput.Text = string.Empty;

        DisplayAlert("Успіх", "Завдання успішно додано!", "ОК");
    }

    private async void OnStartFocusClicked(object sender, EventArgs e)
    {
        if (!int.TryParse(FocusDurationInput.Text, out sessionMinutes) || sessionMinutes <= 0)
        {
            sessionMinutes = 25;
        }

        BtnStartFocus.IsVisible = false;
        FocusSubjectInput.IsEnabled = false;
        FocusDurationInput.IsEnabled = false;
        TimerActiveLayout.IsVisible = true;

        isTimerRunning = true;
        isPaused = false;
        BtnPauseResume.Text = "Пауза";

        totalSeconds = sessionMinutes * 60;
        elapsedSeconds = 0;

        while (isTimerRunning && elapsedSeconds < totalSeconds)
        {
            if (isPaused)
            {
                await Task.Delay(500);
                continue;
            }

            int remainingSeconds = totalSeconds - elapsedSeconds;
            int min = remainingSeconds / 60;
            int sec = remainingSeconds % 60;
            LblTimerDisplay.Text = $"{min:D2}:{sec:D2}";

            await Task.Delay(1000);

            if (isTimerRunning && !isPaused)
            {
                elapsedSeconds++;
            }
        }

        // Якщо таймер дійшов до кінця сам (без дострокового завершення)
        if (isTimerRunning && elapsedSeconds >= totalSeconds)
        {
            TimerActiveLayout.IsVisible = false;
            TimerFinishedLayout.IsVisible = true;
        }
    }

    private void OnPauseResumeClicked(object sender, EventArgs e)
    {
        isPaused = !isPaused;
        BtnPauseResume.Text = isPaused ? "Продовжити" : "Пауза";
    }

    private void OnEndEarlyClicked(object sender, EventArgs e)
    {
        isTimerRunning = false; // Зупиняємо цикл таймера
        TimerActiveLayout.IsVisible = false;

        // Вираховуємо скільки хвилин реально пройшло (округлюємо у меншу або більшу сторону)
        int actualMinutes = (int)Math.Ceiling(elapsedSeconds / 60.0);

        if (actualMinutes > 0)
        {
            string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

            // Зараховуємо до статистики лише фактично проведений час
            if (subjectStats.ContainsKey(subject))
            {
                subjectStats[subject] += actualMinutes;
            }
            else
            {
                subjectStats[subject] = actualMinutes;
            }

            SaveStats();
            UpdateStatsUI();

            DisplayAlert("Сесію завершено", $"Достроково завершено. Зараховано {actualMinutes} хв до предмета «{subject}».", "ОК");
        }
        else
        {
            DisplayAlert("Скасовано", "Сесія тривала занадто мало, час не зараховано.", "ОК");
        }

        ResetFocusUI();
    }

    private void OnCancelFocusClicked(object sender, EventArgs e)
    {
        isTimerRunning = false;
        ResetFocusUI();
    }

    private void ResetFocusUI()
    {
        isTimerRunning = false;
        isPaused = false;
        TimerActiveLayout.IsVisible = false;
        TimerFinishedLayout.IsVisible = false;
        BtnStartFocus.IsVisible = true;
        FocusSubjectInput.IsEnabled = true;
        FocusDurationInput.IsEnabled = true;
        BtnPauseResume.Text = "Пауза";
    }

    private void OnFinishTaskClicked(object sender, EventArgs e)
    {
        string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

        if (subjectStats.ContainsKey(subject))
        {
            subjectStats[subject] += sessionMinutes;
        }
        else
        {
            subjectStats[subject] = sessionMinutes;
        }

        SaveStats();
        UpdateStatsUI();

        DisplayAlert("Чудово!", $"Сесію на {sessionMinutes} хв завершено й зараховано до предмета «{subject}»!", "ОК");
        OnCancelFocusClicked(sender, e);
    }

    private void OnContinueFocusClicked(object sender, EventArgs e)
    {
        TimerFinishedLayout.IsVisible = false;
        BtnStartFocus.IsVisible = true;
        FocusSubjectInput.IsEnabled = true;
        FocusDurationInput.IsEnabled = true;
    }

    // Збереження статистики у Preferences через JSON
    private void SaveStats()
    {
        try
        {
            var json = JsonSerializer.Serialize(subjectStats);
            Preferences.Set(StatsStorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка збереження статистики: {ex.Message}");
        }
    }

    // Завантаження статистики з Preferences
    private void LoadStats()
    {
        if (Preferences.ContainsKey(StatsStorageKey))
        {
            var json = Preferences.Get(StatsStorageKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                    if (loaded != null)
                    {
                        subjectStats = loaded;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка читання статистики: {ex.Message}");
                }
            }
        }
    }

    private void UpdateStatsUI()
    {
        StatsList.Clear();
        foreach (var pair in subjectStats)
        {
            StatsList.Add(new StatItem
            {
                Subject = pair.Key,
                TimeFormatted = FormatMinutes(pair.Value)
            });
        }
    }

    private string FormatMinutes(int totalMinutes)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (hours > 0 && minutes > 0)
            return $"{hours} год {minutes} хв";
        if (hours > 0)
            return $"{hours} год";

        return $"{minutes} хв";
    }
}