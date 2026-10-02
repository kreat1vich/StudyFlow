using System.Collections.ObjectModel;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class TasksPage : ContentPage
{
    public static ObservableCollection<TaskItem> Tasks { get; set; } = new();

    public TasksPage()
    {
        InitializeComponent();
        TasksCollection.ItemsSource = Tasks;
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TaskItem task)
        {
            Tasks.Remove(task);
        }
    }
}