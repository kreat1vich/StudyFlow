using Microsoft.Maui.Storage;

namespace StudyFlow.Views;

public partial class SchedulePage : ContentPage
{
    private bool isEditing = false;
    private Dictionary<string, Editor> dayEditors = new();
    private Dictionary<string, Label> dayLabels = new();

    private readonly string[] daysOfWeek = { "Понеділок", "Вівторок", "Середа", "Четвер", "П'ятниця", "Субота" };

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

        foreach (var day in daysOfWeek)
        {
            string savedText = Preferences.Get($"Schedule_{day}", string.Empty);

            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#1E1E1E"),
                CornerRadius = 16,
                Padding = 16,
                HasShadow = false
            };

            var stack = new VerticalStackLayout { Spacing = 8 };

            // Назва дня тижня (робимо світлішим та виразнішим акцентом)
            stack.Children.Add(new Label
            {
                Text = day,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                TextColor = Color.FromArgb("#8A75F5") // Світліший фіолетовий відтінок
            });

            // Статичний Label для перегляду
            var label = new Label
            {
                Text = string.IsNullOrWhiteSpace(savedText) ? "Розклад не заповнено" : savedText,
                // Для порожнього тексту даємо світліший сірий, для заповненого — майже чистий білий
                TextColor = string.IsNullOrWhiteSpace(savedText) ? Color.FromArgb("#999999") : Color.FromArgb("#E0E0E0"),
                FontSize = 14,
                IsVisible = !isEditing
            };
            dayLabels[day] = label;
            stack.Children.Add(label);

            // Редактор для зміни розкладу
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
            stack.Children.Add(editor);

            frame.Content = stack;
            DaysLayout.Children.Add(frame);
        }
    }

    private void OnEditSaveClicked(object sender, EventArgs e)
    {
        if (isEditing)
        {
            foreach (var day in daysOfWeek)
            {
                if (dayEditors.ContainsKey(day))
                {
                    string newText = dayEditors[day].Text;
                    Preferences.Set($"Schedule_{day}", newText);

                    if (dayLabels.ContainsKey(day))
                    {
                        dayLabels[day].Text = string.IsNullOrWhiteSpace(newText) ? "Розклад не заповнено" : newText;
                        dayLabels[day].TextColor = string.IsNullOrWhiteSpace(newText) ? Color.FromArgb("#777777") : Colors.White;
                    }
                }
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

        foreach (var day in daysOfWeek)
        {
            if (dayLabels.ContainsKey(day)) dayLabels[day].IsVisible = !isEditing;
            if (dayEditors.ContainsKey(day)) dayEditors[day].IsVisible = isEditing;
        }
    }
}