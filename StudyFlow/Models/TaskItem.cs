namespace StudyFlow.Models
{
    public class TaskItem
    {
        public string Title { get; set; }      // Назва
        public string Subject { get; set; }    // Предмет
        public string Deadline { get; set; }   // Дедлайн

        private bool isCompleted;
        public bool IsCompleted
        {
            get => isCompleted;
            set => isCompleted = value;
        }
    }
}