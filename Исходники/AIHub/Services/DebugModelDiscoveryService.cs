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

            foreach (var path in Directory.EnumerateFiles(root, "tool-model.json", SearchOption.AllDirectories))
            {
                var toolModel = CreateToolModelInfo(path);
                if (toolModel is not null)
                {
                    models.Add(toolModel);
                }
            }

            foreach (var path in Directory.EnumerateFiles(root, "executor-model.json", SearchOption.AllDirectories))
            {
                var executorModel = CreateExecutorModelInfo(path);
                if (executorModel is not null)
                {
                    models.Add(executorModel);
                }
            }
        }

        return models
            .GroupBy(model => model.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(model => string.Equals(model.Role, "executor", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(model => model.IsCoreModel)
                .First())
            .OrderByDescending(model => model.IsCoreModel)
            .ThenBy(model => model.IsRunnable ? 0 : 1)
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
            Format = "gguf",
            IsCoreModel = isCore,
            IsRunnable = true
        };
    }

    private static DebugModelInfo? CreateToolModelInfo(string manifestPath)
    {
        ToolModelManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ToolModelManifest>(File.ReadAllText(manifestPath), JsonOptions);
        }
        catch
        {
            return null;
        }

        if (manifest is null)
        {
            return null;
        }

        var directory = Path.GetDirectoryName(manifestPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var mainFile = manifest.Files
            .FirstOrDefault(file => string.Equals(Path.GetFileName(file.File), "model.safetensors", StringComparison.OrdinalIgnoreCase))
            ?? manifest.Files.FirstOrDefault();
        var displayPath = mainFile is null ? manifestPath : Path.Combine(directory, mainFile.File);
        var sizeBytes = manifest.TotalBytes > 0
            ? manifest.TotalBytes
            : manifest.Files.Sum(file => File.Exists(Path.Combine(directory, file.File)) ? new FileInfo(Path.Combine(directory, file.File)).Length : file.SizeBytes);

        return new DebugModelInfo
        {
            Name = string.IsNullOrWhiteSpace(manifest.Name) ? Path.GetFileName(directory) : manifest.Name,
            Path = displayPath,
            SizeBytes = sizeBytes,
            Role = string.IsNullOrWhiteSpace(manifest.ToolKind) ? manifest.Role : manifest.ToolKind,
            Status = manifest.Status,
            Format = manifest.Format,
            IsCoreModel = false,
            IsRunnable = false
        };
    }

    private static DebugModelInfo? CreateExecutorModelInfo(string manifestPath)
    {
        try
        {
            var manifest = ExecutorModelManifestStore.Load(manifestPath);
            var directory = Path.GetDirectoryName(manifestPath);
            var path = directory is null ? null : Path.Combine(directory, manifest?.File ?? string.Empty);
            if (manifest is null
                || path is null
                || manifest.Status != "installed"
                || manifest.RuntimeVerifiedAt is null
                || !File.Exists(path))
            {
                return null;
            }

            var passport = ExecutorModelManifestStore.ResolvePassport(manifest);
            if (passport.Source == "manual_catalog"
                && manifest.SemanticPassport.Source != "manual_catalog")
            {
                manifest.SemanticPassport = passport;
                ExecutorModelManifestStore.Save(manifestPath, manifest);
            }

            return new DebugModelInfo
            {
                Name = string.IsNullOrWhiteSpace(manifest.RepoId) ? manifest.RequestedModel : manifest.RepoId,
                Path = path,
                SizeBytes = new FileInfo(path).Length,
                Role = "executor",
                Status = manifest.Status,
                Format = "gguf",
                IsRunnable = true,
                SemanticDescriptionRu = passport.DescriptionRu,
                SemanticDescriptionEn = passport.DescriptionEn
            };
        }
        catch
        {
            return null;
        }
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
