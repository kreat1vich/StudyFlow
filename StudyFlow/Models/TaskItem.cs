using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudyFlow.Models;

public class TaskItem : INotifyPropertyChanged
{
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Deadline { get; set; } = string.Empty;

    private bool isCompleted;
    public bool IsCompleted
    {
        get => isCompleted;
        set
        {
            if (isCompleted == value)
                return;

            isCompleted = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
