using System.Text.Json.Serialization;
using StudyFlow.Views;

namespace StudyFlow.Models
{
    public class GoalItem
    {
        public string Subject { get; set; }
        public double TargetHours { get; set; }
        public string Period { get; set; }         // "Тиждень" або "Місяць"
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        public int TotalMinutes
        {
            get => GetTotalMinutesForSubject();
        }

        [JsonIgnore]
        public double CurrentHours
        {
            get => Math.Round(TotalMinutes / 60.0, 1);
        }

        [JsonIgnore]
        public string ProgressText
        {
            get
            {
                int hours = TotalMinutes / 60;
                int minutes = TotalMinutes % 60;

                string currentStr = hours > 0 ? $"{hours} год {minutes} хв" : $"{minutes} хв";
                return $"{currentStr} / {TargetHours} год";
            }
        }

        [JsonIgnore]
        public string ProgressBarText
        {
            get
            {
                int totalBlocks = 15;
                double targetMinutes = TargetHours * 60;
                double progress = targetMinutes > 0 ? Math.Clamp((double)TotalMinutes / targetMinutes, 0, 1) : 0;
                int filledBlocks = (int)Math.Round(progress * totalBlocks);
                int emptyBlocks = totalBlocks - filledBlocks;

                return new string('█', filledBlocks) + new string('░', emptyBlocks);
            }
        }

        [JsonIgnore]
        public string RemainingText
        {
            get
            {
                double targetMinutes = TargetHours * 60;
                double remainingMinutes = targetMinutes - TotalMinutes;

                if (remainingMinutes <= 0) return "🎉 Ціль виконано!";

                int hours = (int)(remainingMinutes / 60);
                int minutes = (int)(remainingMinutes % 60);

                if (hours > 0 && minutes > 0)
                    return $"Ще {hours} год {minutes} хв";
                else if (hours > 0)
                    return $"Ще {hours} год";
                else
                    return $"Ще {minutes} хв";
            }
        }

        // Нова властивість: обчислює скільки днів залишилось до кінця тижня чи місяця
        [JsonIgnore]
        public string DaysLeftText
        {
            get
            {
                var today = DateTime.Today;
                DateTime deadline;

                if (Period == "Тиждень")
                {
                    // Робимо дедлайн рівно через 7 днів після створення цілі
                    deadline = CreatedAt.Date.AddDays(7);
                }
                else // "Місяць"
                {
                    // Робимо дедлайн через 1 місяць після створення цілі
                    deadline = CreatedAt.Date.AddMonths(1);
                }

                int daysLeft = (deadline - today).Days;

                if (daysLeft < 0) return "Термін вийшов";
                if (daysLeft == 0) return "Останній день";
                if (daysLeft == 1) return "Залишився 1 день";

                return $"Залишилось {daysLeft} дн.";
            }
        }

        private int GetTotalMinutesForSubject()
        {
            try
            {
                if (FocusPage.allSessions == null || !FocusPage.allSessions.Any())
                    return 0;

                var now = DateTime.Now;

                var filtered = FocusPage.allSessions.Where(s =>
                    string.Equals(s.Subject?.Trim(), Subject?.Trim(), StringComparison.OrdinalIgnoreCase));

                if (Period == "Тиждень")
                {
                    var today = now.Date;
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    var startOfWeek = today.AddDays(-diff);
                    var endOfWeek = startOfWeek.AddDays(7);

                    filtered = filtered.Where(s => s.Date >= startOfWeek && s.Date < endOfWeek);
                }
                else if (Period == "Місяць")
                {
                    filtered = filtered.Where(s => s.Date.Month == now.Month && s.Date.Year == now.Year);
                }

                return filtered.Sum(s => s.Minutes);
            }
            catch
            {
                return 0;
            }
        }
    }
}