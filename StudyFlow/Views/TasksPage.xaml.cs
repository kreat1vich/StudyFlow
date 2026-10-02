using System.Collections.ObjectModel;
using System.Text.Json;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class TasksPage : ContentPage
{
    public static ObservableCollection<TaskItem> Tasks { get; set; } = new();
    private const string TasksStorageKey = "saved_tasks_list";

    public TasksPage()
    {
        InitializeComponent();
        LoadTasks(); // Завантажуємо збережені завдання при відкритті
        TasksCollection.ItemsSource = Tasks;
    }

    // Метод збереження завдань
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
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка читання завдань: {ex.Message}");
                }
            }
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
}