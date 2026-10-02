using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudyFlow.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        public string Title { get; set; }      // Назва
        public string Subject { get; set; }    // Предмет
        public string Deadline { get; set; }   // Дедлайн

        private bool isCompleted;
        public bool IsCompleted
        {
            get => isCompleted;
            set
            {
                if (isCompleted != value)
                {
                    isCompleted = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}