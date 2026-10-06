using StudyFlow.Models;
using StudyFlow.Services;

namespace StudyFlow.Views;

public partial class GoalsPage : ContentPage
{
    private string selectedPeriod = "Тиждень";

    public GoalsPage()
    {
        InitializeComponent();
        StudyDataStore.Goals.CollectionChanged += (_, _) => RenderGoals();
        _ = InitializeAsync();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RenderGoals();
    }

    private async Task InitializeAsync()
    {
        await StudyDataStore.EnsureLoadedAsync();
        RenderGoals();
    }

    private void OnPeriodButtonClicked(object sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        bool isWeek = button == BtnWeek;
        selectedPeriod = isWeek ? "Тиждень" : "Місяць";
        BtnWeek.BackgroundColor = isWeek ? Color.FromArgb("#512BD4") : Color.FromArgb("#2C2C2C");
        BtnMonth.BackgroundColor = isWeek ? Color.FromArgb("#2C2C2C") : Color.FromArgb("#512BD4");
    }

    private async void OnAddGoalClicked(object sender, EventArgs e)
    {
        string subject = GoalSubjectInput.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(subject) ||
            !double.TryParse(GoalHoursInput.Text, out double hours) ||
            hours <= 0 || hours > 1000)
        {
            await DisplayAlert("Помилка", "Введіть назву та кількість годин від 0 до 1000.", "ОК");
            return;
        }

        StudyDataStore.Goals.Add(new GoalItem
        {
            Subject = subject,
            TargetHours = hours,
            Period = selectedPeriod,
            CreatedAt = DateTime.Now
        });

        await StudyDataStore.SaveGoalsAsync();

        GoalSubjectInput.Text = string.Empty;
        GoalHoursInput.Text = string.Empty;
    }

    private async void OnDeleteGoalClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not GoalItem goal)
            return;

        bool confirmed = await DisplayAlert("Видалити ціль?", goal.Subject, "Видалити", "Скасувати");
        if (!confirmed)
            return;

        StudyDataStore.Goals.Remove(goal);
        await StudyDataStore.SaveGoalsAsync();
    }

    private void RenderGoals()
    {
        if (GoalsContainer == null)
            return;

        GoalsContainer.Children.Clear();

        foreach (var goal in StudyDataStore.Goals)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                CornerRadius = 12,
                Padding = 16,
                HasShadow = false
            };

            var stack = new VerticalStackLayout { Spacing = 10 };
            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var subject = new Label
            {
                Text = goal.Subject,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };
            var days = new Label
            {
                Text = goal.DaysLeftText,
                FontSize = 12,
                TextColor = Color.FromArgb("#8A75F5"),
                VerticalOptions = LayoutOptions.Center
            };

            header.Add(subject);
            Grid.SetColumn(days, 1);
            header.Add(days);
            stack.Add(header);

            var progress = new HorizontalStackLayout { Spacing = 12 };
            progress.Add(new Label
            {
                Text = goal.ProgressBarText,
                FontSize = 14,
                TextColor = Color.FromArgb("#512BD4"),
                FontAttributes = FontAttributes.Bold,
                FontFamily = "Monospace"
            });
            progress.Add(new Label
            {
                Text = goal.ProgressText,
                FontSize = 13,
                TextColor = Color.FromArgb("#CCCCCC"),
                VerticalOptions = LayoutOptions.Center
            });
            stack.Add(progress);

            stack.Add(new Label
            {
                Text = goal.RemainingText,
                FontSize = 12,
                TextColor = goal.TotalMinutes >= goal.TargetHours * 60
                    ? Color.FromArgb("#81C784")
                    : Color.FromArgb("#888888")
            });

            var deleteButton = new Button
            {
                Text = "🗑️ Видалити",
                BackgroundColor = Color.FromArgb("#C62828"),
                TextColor = Colors.White,
                CornerRadius = 6,
                HeightRequest = 35,
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 4, 0, 0),
                CommandParameter = goal
            };
            deleteButton.Clicked += OnDeleteGoalClicked;
            stack.Add(deleteButton);

            frame.Content = stack;
            GoalsContainer.Children.Add(frame);
        }
    }
}
