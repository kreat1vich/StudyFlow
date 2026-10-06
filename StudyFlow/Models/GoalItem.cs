using System.Text.Json.Serialization;
using StudyFlow.Services;

namespace StudyFlow.Models;

public class GoalItem
{
    public string Subject { get; set; } = string.Empty;
    public double TargetHours { get; set; }
    public string Period { get; set; } = "Тиждень";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public int TotalMinutes => GetTotalMinutesForSubject();

    [JsonIgnore]
    public double CurrentHours => Math.Round(TotalMinutes / 60.0, 1);

    [JsonIgnore]
    public string ProgressText
    {
        get
        {
            int hours = TotalMinutes / 60;
            int minutes = TotalMinutes % 60;
            string current = hours > 0 ? $"{hours} год {minutes} хв" : $"{minutes} хв";
            return $"{current} / {TargetHours:0.##} год";
        }
    }

    [JsonIgnore]
    public string ProgressBarText
    {
        get
        {
            const int totalBlocks = 15;
            double targetMinutes = TargetHours * 60;
            double progress = targetMinutes > 0
                ? Math.Clamp(TotalMinutes / targetMinutes, 0, 1)
                : 0;

            int filledBlocks = (int)Math.Round(progress * totalBlocks);
            return new string('█', filledBlocks) + new string('░', totalBlocks - filledBlocks);
        }
    }

    [JsonIgnore]
    public string RemainingText
    {
        get
        {
            double remainingMinutes = TargetHours * 60 - TotalMinutes;
            if (remainingMinutes <= 0)
                return "🎉 Ціль виконано!";

            int hours = (int)(remainingMinutes / 60);
            int minutes = (int)(remainingMinutes % 60);

            if (hours > 0 && minutes > 0) return $"Ще {hours} год {minutes} хв";
            if (hours > 0) return $"Ще {hours} год";
            return $"Ще {minutes} хв";
        }
    }

    [JsonIgnore]
    public string DaysLeftText
    {
        get
        {
            var startDate = CreatedAt == default ? DateTime.Today : CreatedAt.Date;
            var deadline = GetDeadline(startDate);
            int daysLeft = (deadline - DateTime.Today).Days;

            if (daysLeft < 0) return "Термін вийшов";
            if (daysLeft == 0) return "Останній день";
            if (daysLeft == 1) return "Залишився 1 день";
            return $"Залишилось {daysLeft} дн.";
        }
    }

    private int GetTotalMinutesForSubject()
    {
        var startDate = CreatedAt == default ? DateTime.Today : CreatedAt.Date;
        var endDate = GetDeadline(startDate);

        return StudyDataStore.Sessions
            .Where(s => string.Equals(s.Subject?.Trim(), Subject?.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Date.Date >= startDate && s.Date.Date < endDate)
            .Sum(s => Math.Max(0, s.Minutes));
    }

    private DateTime GetDeadline(DateTime startDate) =>
        string.Equals(Period, "Місяць", StringComparison.OrdinalIgnoreCase)
            ? startDate.AddMonths(1)
            : startDate.AddDays(7);
}
