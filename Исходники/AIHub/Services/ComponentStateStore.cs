using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ComponentStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly object _sync = new();

    public ComponentStateDocument Load()
    {
        lock (_sync)
        {
            AppDataPaths.EnsureComponentDirectories();
            if (!File.Exists(AppDataPaths.ComponentStatePath))
            {
                return new ComponentStateDocument();
            }

            try
            {
                return JsonSerializer.Deserialize<ComponentStateDocument>(
                    File.ReadAllText(AppDataPaths.ComponentStatePath),
                    JsonOptions) ?? new ComponentStateDocument();
            }
            catch
            {
                return new ComponentStateDocument();
            }
        }
    }

    public void Save(ComponentStateDocument state)
    {
        ArgumentNullException.ThrowIfNull(state);
        lock (_sync)
        {
            AppDataPaths.EnsureComponentDirectories();
            var temporaryPath = AppDataPaths.ComponentStatePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
            File.Move(temporaryPath, AppDataPaths.ComponentStatePath, true);
        }
    }
}
