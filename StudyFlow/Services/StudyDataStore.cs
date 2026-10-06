using System.Collections.ObjectModel;
using StudyFlow.Models;

namespace StudyFlow.Services;

public static class StudyDataStore
{
    private static readonly SemaphoreSlim LoadLock = new(1, 1);
    private static bool isLoaded;

    public static ObservableCollection<TaskItem> Tasks { get; } = new();
    public static ObservableCollection<GoalItem> Goals { get; } = new();
    public static List<StudySession> Sessions { get; private set; } = new();

    public static async Task EnsureLoadedAsync()
    {
        if (isLoaded)
            return;

        await LoadLock.WaitAsync();
        try
        {
            if (isLoaded)
                return;

            var tasks = await JsonStorage.LoadAsync<List<TaskItem>>("tasks.json");
            var goals = await JsonStorage.LoadAsync<List<GoalItem>>("goals.json");
            var sessions = await JsonStorage.LoadAsync<List<StudySession>>("sessions.json");

            // Міграція з v1.x, де дані зберігалися в Preferences.
            if (tasks == null)
                tasks = LoadLegacy<List<TaskItem>>("saved_tasks_list");
            if (goals == null)
                goals = LoadLegacy<List<GoalItem>>("saved_goals_list");
            if (sessions == null)
                sessions = LoadLegacy<List<StudySession>>("saved_study_sessions_history");

            Tasks.Clear();
            if (tasks != null)
            {
                foreach (var task in tasks)
                    Tasks.Add(task);
            }

            Goals.Clear();
            if (goals != null)
            {
                foreach (var goal in goals)
                    Goals.Add(goal);
            }

            Sessions = sessions ?? new List<StudySession>();

            await SaveTasksAsync();
            await SaveGoalsAsync();
            await SaveSessionsAsync();

            isLoaded = true;
        }
        finally
        {
            LoadLock.Release();
        }
    }

    private static T? LoadLegacy<T>(string key)
    {
        if (!Preferences.ContainsKey(key))
            return default;

        var json = Preferences.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return default;
        }
    }

    public static Task SaveTasksAsync() => JsonStorage.SaveAsync("tasks.json", Tasks.ToList());
    public static Task SaveGoalsAsync() => JsonStorage.SaveAsync("goals.json", Goals.ToList());
    public static Task SaveSessionsAsync() => JsonStorage.SaveAsync("sessions.json", Sessions);
}
