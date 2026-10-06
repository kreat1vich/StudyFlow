using System.Collections.ObjectModel;
using StudyFlow.Models;
using StudyFlow.Services;

namespace StudyFlow.Views;

public partial class TasksPage : ContentPage
{
    public ObservableCollection<TaskItem> Tasks => StudyDataStore.Tasks;

    public TasksPage()
    {
        InitializeComponent();
        TasksCollection.ItemsSource = Tasks;
        SortTasks();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SortTasks();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not TaskItem task)
            return;

        bool confirmed = await DisplayAlert("Видалити завдання?", task.Title, "Видалити", "Скасувати");
        if (!confirmed)
            return;

        Tasks.Remove(task);
        await StudyDataStore.SaveTasksAsync();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        string title = TitleInput.Text?.Trim() ?? string.Empty;
        string subject = SubjectInput.Text?.Trim() ?? string.Empty;
        string deadline = DeadlineInput.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(subject))
        {
            await DisplayAlert("Помилка", "Заповніть назву та опис завдання.", "ОК");
            return;
        }

        if (!string.IsNullOrWhiteSpace(deadline) && !DeadlineParser.TryParse(deadline, out _))
        {
            await DisplayAlert("Некоректний дедлайн", "Використайте формат 06.10 або 06.10.2026.", "ОК");
            return;
        }

        Tasks.Add(new TaskItem
        {
            Title = title,
            Subject = subject,
            Deadline = string.IsNullOrWhiteSpace(deadline) ? string.Empty : deadline,
            IsCompleted = false
        });

        SortTasks();
        await StudyDataStore.SaveTasksAsync();
        ClearInputs();
    }

    private async void OnCheckBoxCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        await StudyDataStore.SaveTasksAsync();
    }

    private void SortTasks()
    {
        var sorted = Tasks
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => GetDeadlineOrMax(t.Deadline))
            .ThenBy(t => t.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (sorted.SequenceEqual(Tasks))
            return;

        Tasks.Clear();
        foreach (var task in sorted)
            Tasks.Add(task);
    }

    private static DateTime GetDeadlineOrMax(string? deadline)
    {
        return DeadlineParser.TryParse(deadline, out var date) ? date.Date : DateTime.MaxValue;
    }

    private void ClearInputs()
    {
        TitleInput.Text = string.Empty;
        SubjectInput.Text = string.Empty;
        DeadlineInput.Text = string.Empty;
    }
}
