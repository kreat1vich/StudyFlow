using System.Text.Json;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class FocusPage : ContentPage
{
    private bool isTimerRunning = false;
    private bool isPaused = false;
    private int sessionMinutes = 0;
    private int elapsedSeconds = 0;
    private int totalSeconds = 0;

    private const string StatsStorageKey = "saved_study_stats";
    public static Dictionary<string, int> subjectStats = new();

    public FocusPage()
    {
        InitializeComponent();
        LoadStats();
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
        isTimerRunning = false;
        TimerActiveLayout.IsVisible = false;

        int actualMinutes = (int)Math.Ceiling(elapsedSeconds / 60.0);

        if (actualMinutes > 0)
        {
            string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

            if (subjectStats.ContainsKey(subject))
                subjectStats[subject] += actualMinutes;
            else
                subjectStats[subject] = actualMinutes;

            SaveStats();
            DisplayAlert("Сесію завершено", $"Достроково зараховано {actualMinutes} хв до предмета «{subject}».", "ОК");
        }
        else
        {
            DisplayAlert("Скасовано", "Сесія тривала занадто мало, час не зараховано.", "ОК");
        }

        ResetFocusUI();
    }

    private void OnFinishTaskClicked(object sender, EventArgs e)
    {
        string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

        if (subjectStats.ContainsKey(subject))
            subjectStats[subject] += sessionMinutes;
        else
            subjectStats[subject] = sessionMinutes;

        SaveStats();
        DisplayAlert("Чудово!", $"Сесію на {sessionMinutes} хв зараховано до «{subject}»!", "ОК");
        ResetFocusUI();
    }

    private void OnContinueFocusClicked(object sender, EventArgs e)
    {
        TimerFinishedLayout.IsVisible = false;
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

    public static void SaveStats()
    {
        try
        {
            var json = JsonSerializer.Serialize(subjectStats);
            Preferences.Set(StatsStorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
        }
    }

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
                    if (loaded != null) subjectStats = loaded;
                }
                catch { }
            }
        }
    }
}