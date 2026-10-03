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
        RenderNotes(); // Завантажуємо нотатки при старті
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RenderNotes(); // Оновлюємо список при переході на вкладку
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

    private async void OnFinishTaskClicked(object sender, EventArgs e)
    {
        string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

        // Запитуємо нотатку (необов'язково)
        string note = await DisplayPromptAsync(
            "Сесію завершено! 🎉",
            $"Сесію на {sessionMinutes} хв завершено. Бажаєте додати нотатку про те, що зроблено?",
            accept: "Зберегти",
            cancel: "Пропустити",
            placeholder: "Наприклад: вивчив LINQ Where та Select",
            maxLength: 120);

        RecordSession(subject, sessionMinutes, note);
        DisplayAlert("Чудово!", $"Сесію на {sessionMinutes} хв зараховано до «{subject}»!", "ОК");
        ResetFocusUI();
    }

    private async void OnContinueFocusClicked(object sender, EventArgs e)
    {
        string subject = string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

        // Запитуємо нотатку також і при продовженні
        string note = await DisplayPromptAsync(
            "Сесію завершено! 🎉",
            $"Сесію на {sessionMinutes} хв завершено. Бажаєте додати нотатку про те, що зроблено?",
            accept: "Зберегти",
            cancel: "Пропустити",
            maxLength: 120);

        RecordSession(subject, sessionMinutes, note);

        await DisplayAlert("Чудово!", $"Сесію на {sessionMinutes} хв зараховано до «{subject}»!", "ОК");

        TimerFinishedLayout.IsVisible = false;
        ResetFocusUI();
    }

    // Метод запису нової сесії в історію
    private void RecordSession(string subject, int minutes, string note = null)
    {
        allSessions.Add(new StudySession
        {
            Subject = subject,
            Minutes = minutes,
            Date = DateTime.Now,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });

        SaveSessions();
        RenderNotes(); // Оновлюємо список нотаток нижче на сторінці Фокусу
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

    private void RenderNotes()
    {
        if (NotesContainer == null) return;
        NotesContainer.Children.Clear();

        var sessionsWithNotes = allSessions
            .Where(s => !string.IsNullOrEmpty(s.Note))
            .OrderByDescending(s => s.Date)
            .Take(10)
            .ToList();

        if (!sessionsWithNotes.Any()) return;

        NotesContainer.Children.Add(new Label
        {
            Text = "📝 Останні нотатки до фокусів",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            Margin = new Thickness(0, 10, 0, 5)
        });

        foreach (var session in sessionsWithNotes)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"), // Темний фон картки
                CornerRadius = 12,
                Padding = 16,
                HasShadow = false
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                VerticalOptions = LayoutOptions.Center
            };

            var stack = new VerticalStackLayout { Spacing = 4 };

            stack.Add(new Label
            {
                Text = $"{session.Subject} • {session.Minutes} хв ({session.Date:dd.MM HH:mm})",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#8A75F5")
            });

            stack.Add(new Label
            {
                Text = $"\"{session.Note}\"",
                FontSize = 14,
                FontAttributes = FontAttributes.Italic,
                TextColor = Colors.White
            });

            Grid.SetColumn(stack, 0);
            grid.Add(stack);

            var btnEdit = new Button
            {
                Text = "✏️ Редагувати",
                FontSize = 12,
                TextColor = Colors.White, 
                BackgroundColor = Color.FromArgb("#2A2A2A"),
                CornerRadius = 8,
                Padding = new Thickness(12, 0),
                HeightRequest = 36,
                VerticalOptions = LayoutOptions.Center
            };

            btnEdit.Clicked += async (s, e) =>
            {
                string newNote = await DisplayPromptAsync(
                    "Редагувати нотатку",
                    "Змініть текст нотатки:",
                    accept: "Зберегти",
                    cancel: "Скасувати",
                    initialValue: session.Note,
                    maxLength: 120);

                if (newNote != null)
                {
                    session.Note = string.IsNullOrWhiteSpace(newNote) ? null : newNote.Trim();
                    SaveSessions();
                    RenderNotes();
                }
            };

            Grid.SetColumn(btnEdit, 1);
            grid.Add(btnEdit);

            frame.Content = grid;
            NotesContainer.Children.Add(frame);
        }
    }
}