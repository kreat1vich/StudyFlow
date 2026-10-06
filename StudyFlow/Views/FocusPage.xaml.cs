using StudyFlow.Models;
using StudyFlow.Services;

namespace StudyFlow.Views;

public partial class FocusPage : ContentPage
{
    private bool isTimerRunning;
    private bool isPaused;
    private int sessionMinutes;
    private DateTime sessionStartedAt;
    private TimeSpan totalPausedDuration;
    private DateTime? pauseStartedAt;
    private CancellationTokenSource? timerCts;

    public FocusPage()
    {
        InitializeComponent();
        _ = InitializeAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RenderNotes();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Не скидаємо таймер: він має продовжувати працювати при переході між вкладками.
    }

    private async Task InitializeAsync()
    {
        await StudyDataStore.EnsureLoadedAsync();
        RenderNotes();
    }

    private async void OnStartFocusClicked(object sender, EventArgs e)
    {
        await StartFocusAsync();
    }

    private async Task StartFocusAsync()
    {
        if (!int.TryParse(FocusDurationInput.Text, out sessionMinutes) || sessionMinutes <= 0)
            sessionMinutes = 25;

        sessionMinutes = Math.Clamp(sessionMinutes, 1, 24 * 60);
        FocusDurationInput.Text = sessionMinutes.ToString();
        sessionStartedAt = DateTime.Now;
        totalPausedDuration = TimeSpan.Zero;
        pauseStartedAt = null;

        SetInputsEnabled(false);
        BtnStartFocus.IsVisible = false;
        TimerActiveLayout.IsVisible = true;
        TimerFinishedLayout.IsVisible = false;

        isTimerRunning = true;
        isPaused = false;
        BtnPauseResume.Text = "Пауза";

        timerCts?.Cancel();
        timerCts = new CancellationTokenSource();

        try
        {
            await RunTimerAsync(timerCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Нормальне завершення/перезапуск таймера.
        }
    }

    private async Task RunTimerAsync(CancellationToken token)
    {
        var endAt = sessionStartedAt.AddMinutes(sessionMinutes);

        while (isTimerRunning)
        {
            token.ThrowIfCancellationRequested();

            if (isPaused)
            {
                pauseStartedAt ??= DateTime.Now;
                await Task.Delay(200, token);
                continue;
            }

            if (pauseStartedAt.HasValue)
            {
                totalPausedDuration += DateTime.Now - pauseStartedAt.Value;
                pauseStartedAt = null;
                endAt = sessionStartedAt.AddMinutes(sessionMinutes) + totalPausedDuration;
            }

            var remaining = endAt - DateTime.Now;
            if (remaining <= TimeSpan.Zero)
                break;

            UpdateTimerDisplay(remaining);
            await Task.Delay(250, token);
        }

        if (!isTimerRunning)
            return;

        isTimerRunning = false;
        UpdateTimerDisplay(TimeSpan.Zero);
        TimerActiveLayout.IsVisible = false;
        TimerFinishedLayout.IsVisible = true;
    }

    private void UpdateTimerDisplay(TimeSpan remaining)
    {
        int totalSeconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        LblTimerDisplay.Text = $"{minutes:D2}:{seconds:D2}";
    }

    private void OnPauseResumeClicked(object sender, EventArgs e)
    {
        if (!isTimerRunning)
            return;

        isPaused = !isPaused;

        if (isPaused)
            pauseStartedAt = DateTime.Now;
        else if (pauseStartedAt.HasValue)
        {
            totalPausedDuration += DateTime.Now - pauseStartedAt.Value;
            pauseStartedAt = null;
        }

        BtnPauseResume.Text = isPaused ? "Продовжити" : "Пауза";
    }

    private async void OnEndEarlyClicked(object sender, EventArgs e)
    {
        if (!isTimerRunning)
            return;

        isTimerRunning = false;
        timerCts?.Cancel();

        var activeDuration = DateTime.Now - sessionStartedAt - totalPausedDuration;
        if (pauseStartedAt.HasValue)
            activeDuration -= DateTime.Now - pauseStartedAt.Value;

        int actualMinutes = Math.Max(0, (int)activeDuration.TotalMinutes);
        if (actualMinutes > sessionMinutes)
            actualMinutes = sessionMinutes;

        string subject = GetSubject();

        if (actualMinutes > 0)
        {
            await RecordSessionAsync(subject, actualMinutes);
            await DisplayAlert("Сесію завершено", $"Зараховано {actualMinutes} хв до «{subject}».", "ОК");
        }
        else
        {
            await DisplayAlert("Скасовано", "Сесія тривала менше хвилини, тому час не зараховано.", "ОК");
        }

        ResetFocusUI();
    }

    private async void OnFinishTaskClicked(object sender, EventArgs e)
    {
        await SaveCompletedSessionAndResetAsync(startAnotherSession: false);
    }

    private async void OnContinueFocusClicked(object sender, EventArgs e)
    {
        await SaveCompletedSessionAndResetAsync(startAnotherSession: true);
    }

    private async Task SaveCompletedSessionAndResetAsync(bool startAnotherSession)
    {
        string subject = GetSubject();
        string? note = await DisplayPromptAsync(
            "Сесію завершено! 🎉",
            $"Сесію на {sessionMinutes} хв завершено. Додати нотатку?",
            accept: "Зберегти",
            cancel: "Пропустити",
            placeholder: "Наприклад: вивчив LINQ Where та Select",
            maxLength: 120);

        await RecordSessionAsync(subject, sessionMinutes, note);
        await DisplayAlert("Чудово!", $"{sessionMinutes} хв зараховано до «{subject}».", "ОК");

        ResetFocusUI();

        if (startAnotherSession)
        {
            FocusDurationInput.Text = sessionMinutes.ToString();
            await StartFocusAsync();
        }
    }

    private async Task RecordSessionAsync(string subject, int minutes, string? note = null)
    {
        if (minutes <= 0)
            return;

        StudyDataStore.Sessions.Add(new StudySession
        {
            Subject = string.IsNullOrWhiteSpace(subject) ? "Інше" : subject.Trim(),
            Minutes = minutes,
            Date = DateTime.Now,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        });

        await StudyDataStore.SaveSessionsAsync();
        RenderNotes();
    }

    private string GetSubject() =>
        string.IsNullOrWhiteSpace(FocusSubjectInput.Text) ? "Інше" : FocusSubjectInput.Text.Trim();

    private void SetInputsEnabled(bool enabled)
    {
        FocusSubjectInput.IsEnabled = enabled;
        FocusDurationInput.IsEnabled = enabled;
    }

    private void ResetFocusUI()
    {
        isTimerRunning = false;
        isPaused = false;
        timerCts?.Cancel();
        timerCts = null;
        pauseStartedAt = null;
        totalPausedDuration = TimeSpan.Zero;

        TimerActiveLayout.IsVisible = false;
        TimerFinishedLayout.IsVisible = false;
        BtnStartFocus.IsVisible = true;
        SetInputsEnabled(true);
        BtnPauseResume.Text = "Пауза";
        LblTimerDisplay.Text = "25:00";
    }

    private void RenderNotes()
    {
        if (NotesContainer == null)
            return;

        NotesContainer.Children.Clear();

        var sessionsWithNotes = StudyDataStore.Sessions
            .Where(s => !string.IsNullOrWhiteSpace(s.Note))
            .OrderByDescending(s => s.Date)
            .Take(10)
            .ToList();

        if (sessionsWithNotes.Count == 0)
            return;

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
                BackgroundColor = Color.FromArgb("#1E1E1E"),
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
                ColumnSpacing = 10
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

            btnEdit.Clicked += async (_, _) =>
            {
                string? newNote = await DisplayPromptAsync(
                    "Редагувати нотатку",
                    "Змініть текст нотатки:",
                    accept: "Зберегти",
                    cancel: "Скасувати",
                    initialValue: session.Note,
                    maxLength: 120);

                if (newNote == null)
                    return;

                session.Note = string.IsNullOrWhiteSpace(newNote) ? null : newNote.Trim();
                await StudyDataStore.SaveSessionsAsync();
                RenderNotes();
            };

            Grid.SetColumn(btnEdit, 1);
            grid.Add(btnEdit);

            frame.Content = grid;
            NotesContainer.Children.Add(frame);
        }
    }
}
