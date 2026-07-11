using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class LocalModelCatalogTool
{
    private static readonly HashSet<string> LoadLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "any", "light", "optimal", "extreme"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _catalogPath;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly ComputerPassport? _computerPassport;

    public LocalModelCatalogTool(
        string? catalogPath = null,
        Func<DateTimeOffset>? utcNow = null,
        ComputerPassport? computerPassport = null)
    {
        _catalogPath = catalogPath ?? AppDataPaths.HuggingFaceCatalogPath;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _computerPassport = computerPassport;
    }

    public ModelCatalogSearchResponse Search(string requestJson, string currentCoreName = "")
    {
        var request = ParseRequest(requestJson);
        var response = new ModelCatalogSearchResponse { CatalogPath = _catalogPath };
        if (!File.Exists(_catalogPath))
        {
            response.Status = "missing";
            response.RequiresLiveSearch = true;
            response.Warnings.Add("Local model catalog was not found. Run catalog synchronization or use live search.");
            return response;
        }

        HuggingFaceCatalogDatabase database;
        try
        {
            database = new HuggingFaceCatalogStore().Load(_catalogPath, _utcNow());
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            response.Status = "invalid";
            response.RequiresLiveSearch = true;
            response.Warnings.Add($"Local model catalog could not be read: {ex.Message}");
            return response;
        }

        response.LastSuccessfulSyncUtc = database.LastSuccessfulSyncUtc;
        response.LiveVerificationSuggested = database.LastSuccessfulSyncUtc is null
            || _utcNow() - database.LastSuccessfulSyncUtc.Value > TimeSpan.FromDays(30);

        var eligible = database.Records
            .Where(record => record.IsAvailable
                && !record.Entry.IsPrivate
                && !record.Entry.IsDisabled
                && !string.IsNullOrWhiteSpace(record.Entry.RepoId))
            .ToList();
        response.RecordsConsidered = eligible.Count;

        var passport = _computerPassport ?? new ComputerPassportService().EnsurePassport();

        var scored = eligible
            .Select(record => new
            {
                Scored = Score(record, request),
                Hardware = ModelHardwareCompatibilityService.Assess(record.Entry.ParameterCount, passport, request.LoadLevel)
            })
            .Where(item => item.Scored.IsEligible)
            .ToList();
        response.HardwareRejectedCount = scored.Count(item => item.Hardware.IsCompatible == false);
        var compatible = scored
            .Where(item => item.Hardware.IsCompatible != false)
            .Select(item => new
            {
                item.Scored,
                item.Hardware,
                Candidate = CreateCandidate(
                    item.Scored.Record,
                    item.Scored.Reasons,
                    request.LoadLevel,
                    item.Hardware)
            })
            .ToList();
        response.LineageRejectedCount = compatible.Count(item =>
            !ChoiceExecutorPolicy.IsCandidateLineageAllowed(item.Candidate, currentCoreName));
        var ranked = compatible
            .Where(item => ChoiceExecutorPolicy.IsCandidateLineageAllowed(item.Candidate, currentCoreName))
            .OrderByDescending(item => item.Scored.Score)
            .ThenByDescending(item => item.Scored.Record.LastSuccessfulCheckUtc ?? DateTimeOffset.MinValue)
            .ThenByDescending(item => item.Scored.Record.Entry.Likes ?? 0)
            .ThenByDescending(item => item.Scored.Record.Entry.Downloads ?? 0)
            .ThenBy(item => item.Scored.Record.Entry.RepoId, StringComparer.OrdinalIgnoreCase)
            .Take(request.Limit)
            .Select(item => item.Candidate)
            .ToList();

        response.Candidates = ranked;
        response.RequiresLiveSearch = ranked.Count == 0;
        response.Status = ranked.Count == 0 ? "empty" : "ready";
        if (ranked.Count == 0)
        {
            response.Warnings.Add("No local catalog entry matched the requested direction and load constraints.");
        }
        if (response.HardwareRejectedCount > 0)
        {
            response.Warnings.Add($"Hardware filter rejected {response.HardwareRejectedCount} candidate(s) that do not fit the current PC under the requested load mode.");
        }
        if (response.LineageRejectedCount > 0)
        {
            response.Warnings.Add($"Lineage policy rejected {response.LineageRejectedCount} candidate(s) from the current core family without a significant generation advance.");
        }

        return response;
    }

    private static ModelCatalogSearchRequest ParseRequest(string requestJson)
    {
        ModelCatalogSearchRequest request;
        try
        {
            request = JsonSerializer.Deserialize<ModelCatalogSearchRequest>(requestJson, JsonOptions)
                ?? new ModelCatalogSearchRequest();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"model_catalog_search arguments are invalid JSON: {ex.Message}", ex);
        }

        request.Directions = Normalize(request.Directions);
        request.RequiredCapabilities = Normalize(request.RequiredCapabilities);
        request.TaskType = request.TaskType?.Trim() ?? string.Empty;
        request.LoadLevel = request.LoadLevel?.Trim().ToLowerInvariant() ?? "any";
        if (!LoadLevels.Contains(request.LoadLevel))
        {
            throw new InvalidOperationException("loadLevel must be one of: any, light, optimal, extreme.");
        }

        request.Limit = Math.Clamp(request.Limit, 1, 6);
        return request;
    }

    private static ScoredRecord Score(HuggingFaceCatalogRecord record, ModelCatalogSearchRequest request)
    {
        var reasons = new List<string>();
        var score = 0d;
        var directions = record.CatalogDirections
            .Concat(record.SeedSlots.Select(slot => slot.Direction))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var directionMatches = request.Directions
            .Where(direction => directions.Contains(direction, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (request.Directions.Count > 0 && directionMatches.Count == 0)
        {
            return new ScoredRecord(record, false, 0, reasons);
        }

        if (directionMatches.Count > 0)
        {
            score += 100 + (directionMatches.Count - 1) * 10;
            reasons.Add("direction: " + string.Join(", ", directionMatches));
        }

        var loadLevels = ResolveLoadLevels(record, out var inferred);
        if (!string.Equals(request.LoadLevel, "any", StringComparison.OrdinalIgnoreCase))
        {
            if (!loadLevels.Contains(request.LoadLevel, StringComparer.OrdinalIgnoreCase))
            {
                return new ScoredRecord(record, false, 0, reasons);
            }

            score += inferred ? 18 : 30;
            reasons.Add($"load: {request.LoadLevel}" + (inferred ? " (inferred)" : string.Empty));
        }

        var searchable = string.Join(' ', new[]
        {
            record.Entry.RepoId,
            record.Entry.PipelineTag,
            record.Entry.ModelType,
            record.Entry.AuthorDescription,
            string.Join(' ', record.Entry.Tags),
            string.Join(' ', record.Entry.Architectures),
            string.Join(' ', record.SeedSlots.Select(slot => slot.Role))
        }).ToLowerInvariant();
        foreach (var term in Tokenize(request.RequiredCapabilities.Append(request.TaskType)))
        {
            if (searchable.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 8;
                if (reasons.Count(reason => reason.StartsWith("capability:", StringComparison.Ordinal)) < 3)
                {
                    reasons.Add("capability: " + term);
                }
            }
        }

        if (record.SeedSlots.Count > 0)
        {
            score += 12;
            reasons.Add("curated catalog slot");
        }
        if (record.IsRadarDiscovery)
        {
            score += Math.Min(record.DiscoveryScore, 10);
            reasons.Add("popular new model radar");
        }
        if (!string.IsNullOrWhiteSpace(record.Entry.License))
        {
            score += 3;
        }
        if (record.LastSuccessfulCheckUtc is not null)
        {
            score += 3;
        }

        return new ScoredRecord(record, true, score, reasons);
    }

    private static ModelCatalogCandidate CreateCandidate(
        HuggingFaceCatalogRecord record,
        List<string> reasons,
        string requestedLoadLevel,
        ModelHardwareCompatibility hardware)
    {
        var loadLevels = ResolveLoadLevels(record, out var inferred);
        return new ModelCatalogCandidate
        {
            RepoId = record.Entry.RepoId,
            PipelineTag = record.Entry.PipelineTag,
            License = record.Entry.License,
            ParameterCount = record.Entry.ParameterCount,
            ContextLength = record.Entry.ContextLength,
            BaseModels = record.Entry.BaseModels.ToList(),
            ModelType = record.Entry.ModelType,
            Directions = record.CatalogDirections
                .Concat(record.SeedSlots.Select(slot => slot.Direction))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Roles = record.SeedSlots.Select(slot => slot.Role)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            LoadLevels = loadLevels,
            LoadLevelWasInferred = inferred,
            Source = record.IsRadarDiscovery ? "radar" : "curated_seed",
            LastCheckedUtc = record.LastSuccessfulCheckUtc,
            Downloads = record.Entry.Downloads,
            Likes = record.Entry.Likes,
            Hardware = hardware,
            MatchReasons = reasons.Count == 0
                ? [string.Equals(requestedLoadLevel, "any", StringComparison.OrdinalIgnoreCase) ? "catalog candidate" : $"load: {requestedLoadLevel}"]
                : reasons
        };
    }

    private static List<string> ResolveLoadLevels(HuggingFaceCatalogRecord record, out bool inferred)
    {
        var explicitLevels = record.SeedSlots.Select(slot => slot.LoadLevel)
            .Where(level => !string.IsNullOrWhiteSpace(level))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (explicitLevels.Count > 0)
        {
            inferred = false;
            return explicitLevels;
        }

        inferred = true;
        return record.Entry.ParameterCount switch
        {
            <= 16_000_000_000 => ["light"],
            <= 40_000_000_000 => ["optimal"],
            > 40_000_000_000 => ["extreme"],
            _ => ["optimal"]
        };
    }

    private static List<string> Normalize(IEnumerable<string>? values) => (values ?? [])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim().ToLowerInvariant())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static IEnumerable<string> Tokenize(IEnumerable<string> values) => values
        .SelectMany(value => value.ToLowerInvariant().Split(
            [' ', '_', '-', ',', '/', ';'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Where(value => value.Length >= 3)
        .Distinct(StringComparer.OrdinalIgnoreCase);

    private sealed record ScoredRecord(
        HuggingFaceCatalogRecord Record,
        bool IsEligible,
        double Score,
        List<string> Reasons);
}
