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

    private const string SessionsStorageKey = "saved_study_sessions_history";

    // Список усіх збережених сесій
    public static List<StudySession> allSessions = new();

    public FocusPage()
    {
        InitializeComponent();
        LoadSessions();
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

        // Рахуємо чесно цілі хвилини (без секундного округлення вгору)
        int actualMinutes = elapsedSeconds / 60;

        if (actualMinutes > 0)
        {
            string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

            RecordSession(subject, actualMinutes);
            DisplayAlert("Сесію завершено", $"Достроково зараховано {actualMinutes} хв до предмета «{subject}».", "ОК");
        }
        else
        {
            DisplayAlert("Скасовано", "Сесія тривала занадто мало (менше хвилини), час не зараховано.", "ОК");
        }

        ResetFocusUI();
    }

    private void OnFinishTaskClicked(object sender, EventArgs e)
    {
        string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

        RecordSession(subject, sessionMinutes);
        DisplayAlert("Чудово!", $"Сесію на {sessionMinutes} хв зараховано до «{subject}»!", "ОК");
        ResetFocusUI();
    }

    private async void OnContinueFocusClicked(object sender, EventArgs e)
    {
        // 1. Визначаємо предмет і записуємо сесію в історію та JSON
        string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();
        RecordSession(subject, sessionMinutes);

        // 2. Показуємо плашку з успішним зарахуванням часу (await чекає, поки користувач натисне «ОК»)
        await DisplayAlert("Чудово!", $"Сесію на {sessionMinutes} хв зараховано до «{subject}»!", "ОК");

        // 3. Ховаємо блок завершення та скидаємо інтерфейс для наступного старту
        TimerFinishedLayout.IsVisible = false;
        ResetFocusUI();
    }

    // Метод запису нової сесії в історію
    private void RecordSession(string subject, int minutes)
    {
        allSessions.Add(new StudySession
        {
            Subject = subject,
            Minutes = minutes,
            Date = DateTime.Now
        });

        SaveSessions();
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

    public static void SaveSessions()
    {
        try
        {
            var json = JsonSerializer.Serialize(allSessions);
            Preferences.Set(SessionsStorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка збереження сесій: {ex.Message}");
        }
    }

    private void LoadSessions()
    {
        if (Preferences.ContainsKey(SessionsStorageKey))
        {
            var json = Preferences.Get(SessionsStorageKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var loaded = JsonSerializer.Deserialize<List<StudySession>>(json);
                    if (loaded != null) allSessions = loaded;
                }
                catch { }
            }
        }
    }
}