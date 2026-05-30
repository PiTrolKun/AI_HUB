using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class AppStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public AppState LoadOrCreate()
    {
        AppDataPaths.EnsureBaseDirectory();

        if (!File.Exists(AppDataPaths.StatePath))
        {
            var state = new AppState();
            Save(state);
            return state;
        }

        try
        {
            var json = File.ReadAllText(AppDataPaths.StatePath);
            return JsonSerializer.Deserialize<AppState>(json, JsonOptions) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public void Save(AppState state)
    {
        AppDataPaths.EnsureBaseDirectory();
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(AppDataPaths.StatePath, json);
    }
}
