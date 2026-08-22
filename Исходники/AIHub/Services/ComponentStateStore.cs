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
    private readonly string _statePath;

    public ComponentStateStore(string? statePath = null)
    {
        _statePath = string.IsNullOrWhiteSpace(statePath)
            ? AppDataPaths.ComponentStatePath
            : Path.GetFullPath(statePath);
    }

    public ComponentStateDocument Load()
    {
        lock (_sync)
        {
            EnsureStateDirectory();
            if (!File.Exists(_statePath))
            {
                return new ComponentStateDocument();
            }

            try
            {
                return JsonSerializer.Deserialize<ComponentStateDocument>(
                    File.ReadAllText(_statePath),
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
            EnsureStateDirectory();
            var temporaryPath = _statePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
            File.Move(temporaryPath, _statePath, true);
        }
    }

    private void EnsureStateDirectory()
    {
        if (string.Equals(
            _statePath,
            AppDataPaths.ComponentStatePath,
            StringComparison.OrdinalIgnoreCase))
        {
            AppDataPaths.EnsureComponentDirectories();
            return;
        }

        var directory = Path.GetDirectoryName(_statePath)
            ?? throw new InvalidOperationException("Component state path has no parent directory.");
        Directory.CreateDirectory(directory);
    }
}
