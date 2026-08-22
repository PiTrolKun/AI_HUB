using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class WorkPatternCatalogService
{
    internal const int MinimumExecutionMatchPercent = 40;

    public const string RelativeCatalogPath = "Catalogs/WorkPatterns/work-patterns.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _catalogPath;
    private WorkPatternCatalogDocument? _cached;

    public WorkPatternCatalogService(string? catalogPath = null)
    {
        _catalogPath = catalogPath ?? ResolveDefaultPath();
    }

    public WorkPatternCatalogDocument Load()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        if (!File.Exists(_catalogPath))
        {
            throw new FileNotFoundException(
                "The Sandbox work-pattern catalog was not found.",
                _catalogPath);
        }

        var document = JsonSerializer.Deserialize<WorkPatternCatalogDocument>(
            File.ReadAllText(_catalogPath),
            JsonOptions) ?? throw new InvalidDataException(
                "The Sandbox work-pattern catalog is empty.");
        ValidateCatalog(document);
        _cached = document;
        return document;
    }

    public WorkPatternSelectionResult ValidateSelection(
        WorkPatternSelectionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var catalog = Load();
        var known = catalog.Patterns.ToDictionary(
            pattern => pattern.Id,
            StringComparer.OrdinalIgnoreCase);
        var validated = new WorkPatternSelectionResult
        {
            MissingData = result.MissingData
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList(),
            Source = string.IsNullOrWhiteSpace(result.Source)
                ? "core"
                : result.Source.Trim(),
            UsedFallback = result.UsedFallback
        };

        foreach (var selection in result.Selections)
        {
            var id = selection.PatternId?.Trim() ?? string.Empty;
            if (!known.ContainsKey(id)
                || validated.Selections.Any(existing =>
                    string.Equals(existing.PatternId, id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            validated.Selections.Add(new WorkPatternSelection
            {
                PatternId = known[id].Id,
                MatchPercent = Math.Clamp(selection.MatchPercent, 0, 100),
                Reason = (selection.Reason ?? string.Empty).Trim()
            });
        }

        if (validated.Selections.Count == 0)
        {
            validated.Selections.Add(new WorkPatternSelection
            {
                PatternId = "other.custom",
                MatchPercent = 100,
                Reason = "No catalog pattern could be validated."
            });
            validated.UsedFallback = true;
            validated.Source = "program_fallback";
        }

        validated.Selections = validated.Selections
            .OrderByDescending(selection => selection.MatchPercent)
            .ThenBy(selection => selection.PatternId, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
        return validated;
    }

    public IReadOnlyList<SandboxWorkPattern> ResolveSelected(
        WorkPatternSelectionResult selection)
    {
        var catalog = Load();
        var rankedSelections = selection.Selections
            .OrderByDescending(item => item.MatchPercent)
            .ThenBy(item => item.PatternId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var executableSelections = rankedSelections
            .Where(item => item.MatchPercent >= MinimumExecutionMatchPercent)
            .ToList();
        if (executableSelections.Count == 0 && rankedSelections.Count > 0)
        {
            executableSelections.Add(rankedSelections[0]);
        }

        var ids = executableSelections
            .Select(item => item.PatternId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return catalog.Patterns
            .Where(pattern => ids.Contains(pattern.Id))
            .ToList();
    }

    private static void ValidateCatalog(WorkPatternCatalogDocument document)
    {
        if (document.SchemaVersion != 1)
        {
            throw new InvalidDataException(
                $"Unsupported work-pattern schema: {document.SchemaVersion}.");
        }

        if (document.Patterns.Count == 0)
        {
            throw new InvalidDataException("The work-pattern catalog has no entries.");
        }

        var duplicate = document.Patterns
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern.Id))
            .GroupBy(pattern => pattern.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"Duplicate work-pattern ID: {duplicate.Key}.");
        }

        foreach (var pattern in document.Patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern.Id)
                || string.IsNullOrWhiteSpace(pattern.NameRu)
                || string.IsNullOrWhiteSpace(pattern.NameEn)
                || pattern.ArtifactTypes.Count == 0)
            {
                throw new InvalidDataException(
                    $"Work-pattern entry '{pattern.Id}' is incomplete.");
            }
        }

        if (!document.Patterns.Any(pattern =>
                string.Equals(pattern.Id, "other.custom", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException(
                "The work-pattern catalog must contain other.custom.");
        }
    }

    private static string ResolveDefaultPath()
    {
        var deployed = Path.Combine(
            AppContext.BaseDirectory,
            "Catalogs",
            "WorkPatterns",
            "work-patterns.json");
        if (File.Exists(deployed))
        {
            return deployed;
        }

        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Каталоги",
            "Рабочие_паттерны",
            "work-patterns.json"));
    }
}
