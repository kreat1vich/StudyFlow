using StudyFlow.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Globalization;

namespace StudyFlow.Views;

public partial class TasksPage : ContentPage
{
    public static ObservableCollection<TaskItem> Tasks { get; set; } = new();
    private const string TasksStorageKey = "saved_tasks_list";

    public TasksPage()
    {
        InitializeComponent();
        LoadTasks(); // Завантажуємо та сортуємо завдання при відкритті
        TasksCollection.ItemsSource = Tasks;
    }

    // Метод збереження завдань
    public static void SaveStats() // Залишаємо сумісність, якщо потрібно
    {
        SaveTasks();
    }

    public static void SaveTasks()
    {
        try
        {
            var json = JsonSerializer.Serialize(Tasks);
            Preferences.Set(TasksStorageKey, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка збереження завдань: {ex.Message}");
        }
    }

    // Метод завантаження завдань
    private void LoadTasks()
    {
        if (Preferences.ContainsKey(TasksStorageKey))
        {
            var json = Preferences.Get(TasksStorageKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<TaskItem>>(json);
                    if (items != null)
                    {
                        Tasks.Clear();
                        foreach (var item in items)
                        {
                            Tasks.Add(item);
                        }
                        SortTasks(); // Сортуємо одразу після завантаження
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка читання завдань: {ex.Message}");
                }
            }
        }
    }

    // Метод сортування за дедлайнами
    private void SortTasks()
    {
        var sortedList = Tasks
            .OrderBy(t => string.IsNullOrWhiteSpace(t.Deadline) || t.Deadline == "Не вказано" ? 0 : 1) // 0 — спочатку без дедлайну
            .ThenBy(t =>
            {
                if (string.IsNullOrWhiteSpace(t.Deadline) || t.Deadline == "Не вказано")
                    return DateTime.MinValue;

                // Парсимо формат "04.10" з урахуванням поточного року (2026)
                if (DateTime.TryParseExact(t.Deadline.Trim() + ".2026", "dd.MM.yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    return parsedDate;
                }

                return DateTime.MaxValue; // Якщо введений незрозумілий текст — кидаємо в кінець
            })
            .ToList();

        Tasks.Clear();
        foreach (var item in sortedList)
        {
            Tasks.Add(item);
        }
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TaskItem task)
        {
            Tasks.Remove(task);
            SaveTasks(); // Зберігаємо після видалення
        }
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleInput.Text) || string.IsNullOrWhiteSpace(SubjectInput.Text))
        {
            DisplayAlert("Помилка", "Заповніть назву та опис!", "ОК");
            return;
        }

        Tasks.Add(new TaskItem
        {
            Title = TitleInput.Text,
            Subject = SubjectInput.Text,
            Deadline = string.IsNullOrWhiteSpace(DeadlineInput.Text) ? "Не вказано" : DeadlineInput.Text.Trim(),
            IsCompleted = false
        });

        SortTasks(); // Сортуємо список одразу після додавання нової задачі
        SaveTasks(); // Зберігаємо актуальний відсортований стан

        TitleInput.Text = string.Empty;
        SubjectInput.Text = string.Empty;
        DeadlineInput.Text = string.Empty;
    }

    private void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        // Коли статус чекбокса змінюється, одразу зберігаємо оновлений список у JSON
        SaveTasks();
    }
}