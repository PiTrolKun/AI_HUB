using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class DebugModelDiscoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IReadOnlyList<DebugModelInfo> Discover(StorageSettings storageSettings)
    {
        var roots = storageSettings.Models.Locations
            .Select(location => location.Path)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var models = new List<DebugModelInfo>();
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var path in Directory.EnumerateFiles(root, "*.gguf", SearchOption.AllDirectories))
            {
                models.Add(CreateModelInfo(path));
            }
        }

        return models
            .GroupBy(model => model.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(model => model.IsCoreModel)
            .ThenBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DebugModelInfo CreateModelInfo(string path)
    {
        var file = new FileInfo(path);
        var manifest = LoadManifest(file.DirectoryName);
        var isCore = string.Equals(file.Name, CoreModelManager.CoreModelFileName, StringComparison.OrdinalIgnoreCase);

        return new DebugModelInfo
        {
            Name = file.Name,
            Path = file.FullName,
            SizeBytes = file.Length,
            Role = manifest?.Role ?? string.Empty,
            Status = manifest?.Status ?? string.Empty,
            IsCoreModel = isCore
        };
    }

    private static CoreModelManifest? LoadManifest(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var path = Path.Combine(directory, "core-model.json");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CoreModelManifest>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
