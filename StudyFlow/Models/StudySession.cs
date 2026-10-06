namespace StudyFlow.Models;

public class StudySession
{
    public string Subject { get; set; } = "Інше";
    public int Minutes { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
}
