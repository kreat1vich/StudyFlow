using System.Collections.ObjectModel;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class HomePage : ContentPage
{
    private bool isTimerRunning = false;
    private int sessionMinutes = 0;

    // Словник для збереження накопиченого часу по предметах (у хвилинах)
    private static Dictionary<string, int> subjectStats = new();

    // Колекція для відображення в інтерфейсі
    public ObservableCollection<StatItem> StatsList { get; set; } = new();

    public HomePage()
    {
        InitializeComponent();

        // Прив'язуємо CollectionView до нашої колекції
        StatsCollection.ItemsSource = StatsList;
        UpdateStatsUI();
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleInput.Text) || string.IsNullOrWhiteSpace(SubjectInput.Text))
        {
            DisplayAlert("Помилка", "Заповніть назву та предмет!", "ОК");
            return;
        }

        TasksPage.Tasks.Add(new TaskItem
        {
            Title = TitleInput.Text,
            Subject = SubjectInput.Text,
            Deadline = string.IsNullOrWhiteSpace(DeadlineInput.Text) ? "Не вказано" : DeadlineInput.Text,
            IsCompleted = false
        });

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

        int totalSeconds = sessionMinutes * 60;

        while (isTimerRunning && totalSeconds > 0)
        {
            int min = totalSeconds / 60;
            int sec = totalSeconds % 60;
            LblTimerDisplay.Text = $"{min:D2}:{sec:D2}";

            await Task.Delay(1000);
            totalSeconds--;
        }

        if (isTimerRunning)
        {
            TimerActiveLayout.IsVisible = false;
            TimerFinishedLayout.IsVisible = true;
        }
    }

    private void OnCancelFocusClicked(object sender, EventArgs e)
    {
        isTimerRunning = false;
        TimerActiveLayout.IsVisible = false;
        TimerFinishedLayout.IsVisible = false;
        BtnStartFocus.IsVisible = true;
        FocusSubjectInput.IsEnabled = true;
        FocusDurationInput.IsEnabled = true;
    }

    private void OnFinishTaskClicked(object sender, EventArgs e)
    {
        string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

        // Додаємо хвилини до словника
        if (subjectStats.ContainsKey(subject))
        {
            subjectStats[subject] += sessionMinutes;
        }
        else
        {
            subjectStats[subject] = sessionMinutes;
        }

        UpdateStatsUI(); // Оновлюємо список на екрані

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

    // Оновлення списку статистики з словника
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

    // Перетворення хвилин у формат "X год Y хв"
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