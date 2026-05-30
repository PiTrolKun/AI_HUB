using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class StorageSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public StorageSettings LoadOrCreate()
    {
        AppDataPaths.EnsureBaseDirectory();

        if (!File.Exists(AppDataPaths.StorageSettingsPath))
        {
            var settings = new StorageSettings();
            Save(settings);
            return settings;
        }

        try
        {
            var json = File.ReadAllText(AppDataPaths.StorageSettingsPath);
            return JsonSerializer.Deserialize<StorageSettings>(json, JsonOptions) ?? new StorageSettings();
        }
        catch
        {
            return new StorageSettings();
        }
    }

    public void Save(StorageSettings settings)
    {
        AppDataPaths.EnsureBaseDirectory();
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(AppDataPaths.StorageSettingsPath, json);
    }
}
