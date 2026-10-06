namespace StudyFlow.Views;

public partial class SchedulePage : ContentPage
{
    private bool isEditing;
    private readonly Dictionary<string, Editor> dayEditors = new();
    private readonly Dictionary<string, Label> dayLabels = new();

    private static readonly string[] DaysOfWeek =
    {
        "Понеділок", "Вівторок", "Середа", "Четвер", "П'ятниця", "Субота", "Неділя"
    };

    public SchedulePage()
    {
        InitializeComponent();
        BuildScheduleUI();
    }

    private void BuildScheduleUI()
    {
        DaysLayout.Children.Clear();
        dayEditors.Clear();
        dayLabels.Clear();

        foreach (var day in DaysOfWeek)
        {
            string savedText = Preferences.Get(GetStorageKey(day), string.Empty);
            bool hasSchedule = !string.IsNullOrWhiteSpace(savedText);

            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                CornerRadius = 16,
                Padding = 16,
                HasShadow = false
            };

            var stack = new VerticalStackLayout { Spacing = 8 };
            stack.Add(new Label
            {
                Text = day,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                TextColor = Color.FromArgb("#8A75F5")
            });

            var label = new Label
            {
                Text = hasSchedule ? savedText : "Розклад не заповнено",
                TextColor = hasSchedule ? Color.FromArgb("#E0E0E0") : Color.FromArgb("#888888"),
                FontSize = 14,
                IsVisible = !isEditing
            };
            dayLabels[day] = label;
            stack.Add(label);

            var editor = new Editor
            {
                Text = savedText,
                Placeholder = "Введіть предмети, час або кабінети...",
                TextColor = Colors.White,
                PlaceholderColor = Color.FromArgb("#888888"),
                BackgroundColor = Color.FromArgb("#2C2C2C"),
                AutoSize = EditorAutoSizeOption.TextChanges,
                MinimumHeightRequest = 60,
                IsVisible = isEditing
            };
            dayEditors[day] = editor;
            stack.Add(editor);

            frame.Content = stack;
            DaysLayout.Children.Add(frame);
        }
    }

    private void OnEditSaveClicked(object sender, EventArgs e)
    {
        if (isEditing)
        {
            foreach (var day in DaysOfWeek)
            {
                var text = dayEditors[day].Text?.Trim() ?? string.Empty;
                Preferences.Set(GetStorageKey(day), text);

                var label = dayLabels[day];
                label.Text = string.IsNullOrWhiteSpace(text) ? "Розклад не заповнено" : text;
                label.TextColor = string.IsNullOrWhiteSpace(text)
                    ? Color.FromArgb("#777777")
                    : Colors.White;
            }

            BtnEditSave.Text = "✏️ Редагувати";
            BtnEditSave.BackgroundColor = Color.FromArgb("#2C2C2C");
            isEditing = false;
        }
        else
        {
            BtnEditSave.Text = "💾 Зберегти";
            BtnEditSave.BackgroundColor = Color.FromArgb("#512BD4");
            isEditing = true;
        }

        foreach (var day in DaysOfWeek)
        {
            dayLabels[day].IsVisible = !isEditing;
            dayEditors[day].IsVisible = isEditing;
        }
    }

    private static string GetStorageKey(string day) => $"StudyFlow.Schedule.{day}";
}
