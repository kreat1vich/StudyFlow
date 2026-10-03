using System.Collections.ObjectModel;
using System.Text.Json;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class GoalsPage : ContentPage
{
    public static ObservableCollection<GoalItem> Goals { get; set; } = new();
    private const string GoalsStorageKey = "saved_goals_list";

    private string selectedPeriod = "Тиждень";

    public GoalsPage()
    {
        InitializeComponent();
        LoadGoals();

        Goals.CollectionChanged += (s, e) => RenderGoals();
    }

    // Додаємо цей метод, щоб цілі перераховувалися щоразу при переході на сторінку
    protected override void OnAppearing()
    {
        base.OnAppearing();
        RenderGoals();
    }

    private void OnPeriodButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            selectedPeriod = button.Text;

            if (selectedPeriod == "Тиждень")
            {
                BtnWeek.BackgroundColor = Color.FromArgb("#512BD4");
                BtnMonth.BackgroundColor = Color.FromArgb("#2C2C2C");
            }
            else
            {
                BtnMonth.BackgroundColor = Color.FromArgb("#512BD4");
                BtnWeek.BackgroundColor = Color.FromArgb("#2C2C2C");
            }
        }
    }

    private void OnAddGoalClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GoalSubjectInput.Text) ||
            !double.TryParse(GoalHoursInput.Text, out double hours) || hours <= 0)
        {
            DisplayAlert("Помилка", "Введіть назву та коректну кількість годин!", "ОК");
            return;
        }

        Goals.Add(new GoalItem
        {
            Subject = GoalSubjectInput.Text.Trim(),
            TargetHours = hours,
            Period = selectedPeriod
        });

        SaveGoals();

        GoalSubjectInput.Text = string.Empty;
        GoalHoursInput.Text = string.Empty;
    }

    private void RenderGoals()
    {
        GoalsContainer.Children.Clear();

        foreach (var goal in Goals)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                CornerRadius = 12,
                Padding = 16,
                HasShadow = false
            };

            var stack = new VerticalStackLayout { Spacing = 10 };

            // Рядок: Назва та кількість днів до кінця
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var lblSubject = new Label { Text = goal.Subject, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            var lblDaysLeft = new Label { Text = goal.DaysLeftText, FontSize = 12, TextColor = Color.FromArgb("#8A75F5"), VerticalOptions = LayoutOptions.Center };

            Grid.SetColumn(lblSubject, 0);
            Grid.SetColumn(lblDaysLeft, 1);
            grid.Add(lblSubject);
            grid.Add(lblDaysLeft);
            stack.Add(grid);

            // Шкала та прогрес
            var hStack = new HorizontalStackLayout { Spacing = 12, VerticalOptions = LayoutOptions.Center };
            var lblBar = new Label { Text = goal.ProgressBarText, FontSize = 14, TextColor = Color.FromArgb("#512BD4"), FontAttributes = FontAttributes.Bold, FontFamily = "Monospace" };
            var lblProgress = new Label { Text = goal.ProgressText, FontSize = 13, TextColor = Color.FromArgb("#CCCCCC"), VerticalOptions = LayoutOptions.Center };
            hStack.Add(lblBar);
            hStack.Add(lblProgress);
            stack.Add(hStack);

            // Залишок
            var lblRemaining = new Label { Text = goal.RemainingText, FontSize = 12, TextColor = Color.FromArgb("#888888") };
            stack.Add(lblRemaining);

            // Кнопка видалення
            var btnDelete = new Button
            {
                Text = "🗑️ Видалити",
                BackgroundColor = Color.FromArgb("#C62828"),
                TextColor = Colors.White,
                CornerRadius = 6,
                HeightRequest = 35,
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 4, 0, 0)
            };
            btnDelete.Clicked += (s, e) =>
            {
                Goals.Remove(goal);
                SaveGoals();
            };
            stack.Add(btnDelete);

            frame.Content = stack;
            GoalsContainer.Children.Add(frame);
        }
    }

    public static void SaveGoals()
    {
        try
        {
            var json = JsonSerializer.Serialize(Goals);
            Preferences.Set(GoalsStorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка збереження цілей: {ex.Message}");
        }
    }

    public static void LoadGoals()
    {
        if (Preferences.ContainsKey(GoalsStorageKey))
        {
            var json = Preferences.Get(GoalsStorageKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<GoalItem>>(json);
                    if (items != null)
                    {
                        Goals.Clear();
                        foreach (var item in items)
                        {
                            Goals.Add(item);
                        }
                    }
                }
                catch { }
            }
        }
    }
}