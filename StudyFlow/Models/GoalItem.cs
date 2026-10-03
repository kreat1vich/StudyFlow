using System.Text.Json.Serialization;
using StudyFlow.Views;

namespace StudyFlow.Models
{
    public class GoalItem
    {
        public string Subject { get; set; }
        public double TargetHours { get; set; }
        public string Period { get; set; }
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

        [JsonIgnore]
        public string DaysLeftText
        {
            get
            {
                // Якщо CreatedAt чомусь скинувся, ставимо сьогодні
                if (CreatedAt == default) CreatedAt = DateTime.Today;

                var today = DateTime.Today;
                DateTime deadline = Period == "Тиждень" ? CreatedAt.Date.AddDays(7) : CreatedAt.Date.AddMonths(1);

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

                if (CreatedAt == default) CreatedAt = DateTime.Today;

                // Фільтруємо за назвою предмета
                var filtered = FocusPage.allSessions.Where(s =>
                    string.Equals(s.Subject?.Trim(), Subject?.Trim(), StringComparison.OrdinalIgnoreCase));

                var startDate = CreatedAt.Date;
                DateTime endDate;

                if (Period == "Тиждень")
                {
                    // Робимо інтервал рівно 7 днів від моменту створення цілі
                    endDate = startDate.AddDays(7);
                }
                else // "Місяць"
                {
                    // Робимо інтервал рівно 1 місяць (30 днів) від моменту створення цілі
                    endDate = startDate.AddMonths(1);
                }

                // Рахуємо сесії, які відбулися в діапазоні від створення цілі до дедлайну
                filtered = filtered.Where(s => s.Date.Date >= startDate && s.Date.Date < endDate);

                return filtered.Sum(s => s.Minutes);
            }
            catch
            {
                return 0;
            }
        }
    }
}