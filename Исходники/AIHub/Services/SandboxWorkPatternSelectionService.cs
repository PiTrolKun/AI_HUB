using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class SandboxWorkPatternSelectionService
{
    private readonly WorkPatternCatalogService _catalogService;

    public SandboxWorkPatternSelectionService(
        WorkPatternCatalogService? catalogService = null)
    {
        _catalogService = catalogService ?? new WorkPatternCatalogService();
    }

    public async Task<WorkPatternSelectionResult> SelectAsync(
        LlamaServerRuntimeService runtime,
        DebugModelInfo model,
        string taskContext,
        ChoiceCapabilityProfile capabilityProfile,
        SessionFilePromptManifest? fileManifest,
        ISessionEventLog sessionLog,
        CancellationToken cancellationToken)
    {
        var catalog = _catalogService.Load();
        var prompt = BuildPrompt(
            catalog,
            taskContext,
            capabilityProfile,
            fileManifest);
        try
        {
            var raw = await runtime.GenerateJsonAsync(
                model,
                "You classify a Sandbox task into a finite program-owned work-pattern catalog. Do not solve the task. Return only the requested JSON.",
                prompt,
                WorkPatternSelectionJsonContract.CreateResponseFormat(),
                _ => { },
                cancellationToken);
            var parsed = JsonSerializer.Deserialize<WorkPatternSelectionResult>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsed is null)
            {
                throw new InvalidDataException(
                    "The core returned an empty work-pattern selection.");
            }

            var validated = _catalogService.ValidateSelection(parsed);
            sessionLog.Write("sandbox_work_patterns_selected", new
            {
                RawResponse = raw,
                Selection = validated
            });
            return validated;
        }
        catch (Exception ex) when (
            ex is JsonException
                or InvalidDataException
                or HttpRequestException)
        {
            var fallback = BuildFallback(
                taskContext,
                capabilityProfile,
                fileManifest);
            sessionLog.Write("sandbox_work_patterns_fallback", new
            {
                ErrorType = ex.GetType().FullName,
                ex.Message,
                Selection = fallback
            });
            return fallback;
        }
    }

    public WorkPatternSelectionResult BuildFallback(
        string taskContext,
        ChoiceCapabilityProfile capabilityProfile,
        SessionFilePromptManifest? fileManifest)
    {
        var haystack = string.Join(
            ' ',
            taskContext,
            string.Join(
                ' ',
                capabilityProfile.Dimensions.SelectMany(dimension =>
                    dimension.Values)),
            string.Join(
                ' ',
                fileManifest?.Files.Select(file =>
                    $"{file.Category} {file.Extension}") ?? []))
            .ToLowerInvariant();
        var availableCategories = fileManifest?.Files
            .Where(file => file.IsAvailable)
            .Select(file => file.Category)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scored = _catalogService.Load().Patterns
            .Where(pattern => !string.Equals(
                pattern.Id,
                "other.custom",
                StringComparison.OrdinalIgnoreCase))
            .Select(pattern => new
            {
                Pattern = pattern,
                SignalMatches = pattern.Signals.Count(signal =>
                    haystack.Contains(signal.ToLowerInvariant(), StringComparison.Ordinal)),
                HasCompatibleInput = availableCategories.Count == 0
                    || pattern.InputCategories.Count == 0
                    || pattern.InputCategories.Any(availableCategories.Contains)
            })
            .Where(item => item.SignalMatches > 0 && item.HasCompatibleInput)
            .OrderByDescending(item => item.SignalMatches)
            .ThenBy(item => item.Pattern.Id, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        var result = new WorkPatternSelectionResult
        {
            Source = "program_fallback",
            UsedFallback = true,
            Selections = scored.Select((item, index) => new WorkPatternSelection
            {
                PatternId = item.Pattern.Id,
                MatchPercent = Math.Max(40, 90 - index * 15),
                Reason = "Matched an explicit requested outcome signal and a compatible trusted input type."
            }).ToList()
        };
        return _catalogService.ValidateSelection(result);
    }

    private static string BuildPrompt(
        WorkPatternCatalogDocument catalog,
        string taskContext,
        ChoiceCapabilityProfile capabilityProfile,
        SessionFilePromptManifest? fileManifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Classify the task before model and tool discovery.");
        builder.AppendLine("Percentages are conditional compatibility estimates, not probabilities and not proof of executability.");
        builder.AppendLine("Classify the requested outcome, not merely the attached file type.");
        builder.AppendLine("A file category is context and a compatibility constraint, never proof that the user requested OCR, editing, generation, restoration or conversion.");
        builder.AppendLine("Select OCR only when extracting text is requested; select editing only when changing content is requested; select generation only when a new artifact is requested.");
        builder.AppendLine("For requests to describe, inspect, recognize, interpret or explain media content, select the matching *.describe or *.analyze pattern.");
        builder.AppendLine("Select every pattern materially needed for the requested result. Use other.custom only when no listed pattern fits.");
        builder.AppendLine($"Omit weak alternatives below {WorkPatternCatalogService.MinimumExecutionMatchPercent}% instead of returning them as selected work. Every returned pattern can contribute required execution capabilities.");
        builder.AppendLine("Do not invent file contents. File entries contain metadata only.");
        builder.AppendLine();
        builder.AppendLine("TASK_CONTEXT:");
        builder.AppendLine(taskContext);
        builder.AppendLine();
        builder.AppendLine("CAPABILITY_PROFILE:");
        builder.AppendLine(JsonSerializer.Serialize(capabilityProfile));
        builder.AppendLine();
        builder.AppendLine("TRUSTED_FILE_MANIFEST:");
        builder.AppendLine(JsonSerializer.Serialize(fileManifest ?? new SessionFilePromptManifest()));
        builder.AppendLine();
        builder.AppendLine("FULL_SANDBOX_WORK_PATTERN_CATALOG:");
        builder.AppendLine(JsonSerializer.Serialize(catalog));
        builder.AppendLine();
        builder.AppendLine("Return selections, missingData, source=core and usedFallback=false.");
        return builder.ToString();
    }
}
