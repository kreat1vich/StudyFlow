using System.Text.Json;

namespace StudyFlow.Services;

public static class JsonStorage
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task SaveAsync<T>(string fileName, T data)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, fileName);
        var json = JsonSerializer.Serialize(data, Options);
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task<T?> LoadAsync<T>(string fileName)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, fileName);

        if (!File.Exists(path))
            return default;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return default;
        }
        catch (IOException)
        {
            return default;
        }
    }
}
