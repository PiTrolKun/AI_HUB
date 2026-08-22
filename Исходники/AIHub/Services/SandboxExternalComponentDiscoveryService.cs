using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed class SandboxExternalComponentDiscoveryService
{
    private readonly WebSearchTool _searchTool;

    public SandboxExternalComponentDiscoveryService(WebSearchTool? searchTool = null)
    {
        _searchTool = searchTool ?? new WebSearchTool();
    }

    public async Task<ExternalComponentDiscoveryReport> SearchAsync(
        IEnumerable<CapabilityAdapterBinding> bindings,
        StorageSettings storageSettings,
        CancellationToken cancellationToken,
        ExecutionOutcomeContract? outcomeContract = null,
        WorkPatternSelectionResult? workPatterns = null,
        string? taskGoal = null)
    {
        var unresolved = bindings
            .Where(binding =>
                binding.Status is CapabilityBindingStatuses.AdapterMissing
                    or CapabilityBindingStatuses.UnknownCapability)
            .DistinctBy(
                binding => binding.CapabilityId,
                StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        var report = new ExternalComponentDiscoveryReport();
        foreach (var binding in unresolved)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outcomeAction = outcomeContract?.Actions.FirstOrDefault(action =>
                action.CapabilityIds.Any(capability =>
                    ExecutionActionGraphService.CapabilitiesMatch(
                        capability,
                        binding.CapabilityId)));
            var descriptor = DescribeCapability(binding.CapabilityId);
            var query = BuildQuery(
                binding,
                descriptor,
                outcomeAction,
                workPatterns,
                taskGoal);
            var response = await _searchTool.SearchAsync(
                query,
                storageSettings,
                cancellationToken);
            report.Searches.Add(new ExternalComponentDiscoverySearch
            {
                CapabilityId = binding.CapabilityId,
                Query = query,
                Provider = response.Provider,
                SavedPath = response.SavedPath,
                Candidates = RankCandidates(
                        response.Results,
                        descriptor,
                        outcomeAction?.Purpose ?? binding.Purpose,
                        taskGoal)
                    .Take(5)
                    .Select(result => new ExternalComponentDiscoveryCandidate
                    {
                        Title = result.Result.Title,
                        Url = result.Result.Url,
                        Snippet = result.Result.Snippet,
                        RelevanceScore = result.Score,
                        CandidateKind = ClassifyCandidateKind(result.Result),
                        AcquisitionStatus = ClassifyAcquisitionStatus(result.Result)
                    })
                    .ToList()
            });
        }

        return report;
    }

    internal static string BuildQuery(
        CapabilityAdapterBinding binding,
        string descriptor,
        ExecutionOutcomeAction? outcomeAction,
        WorkPatternSelectionResult? workPatterns,
        string? taskGoal)
    {
        var purpose = FirstNonEmpty(outcomeAction?.Purpose, binding.Purpose);
        var patterns = workPatterns?.Selections
            .OrderByDescending(selection => selection.MatchPercent)
            .Take(2)
            .Select(selection => Humanize(selection.PatternId))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            ?? [];
        return string.Join(
            ' ',
            new[]
            {
                "local open source Windows",
                descriptor,
                Shorten(purpose, 160),
                Shorten(taskGoal ?? string.Empty, 160)
            }
            .Concat(patterns)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Concat(["GitHub Hugging Face install runtime API"]));
    }

    internal static IReadOnlyList<RankedDiscoveryCandidate> RankCandidates(
        IEnumerable<WebSearchResult> results,
        string descriptor,
        string? purpose,
        string? taskGoal)
    {
        var expectedTokens = Tokenize(string.Join(' ', descriptor, purpose, taskGoal))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return results
            .Select(result =>
            {
                var titleTokens = Tokenize(result.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var bodyTokens = Tokenize($"{result.Snippet} {result.Url}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var score = titleTokens.Count(expectedTokens.Contains) * 4
                    + bodyTokens.Count(expectedTokens.Contains)
                    + (IsTrustedDiscoveryHost(result.Url) ? 4 : 0)
                    + (LooksLikeDirectReleaseOrPackage(result) ? 6 : 0)
                    - (LooksLikeReferenceMaterial(result) ? 18 : 0)
                    + (result.RerankScore is { } rerank
                        ? (int)Math.Round(Math.Max(0, rerank) * 2)
                        : 0);
                return new RankedDiscoveryCandidate(result, score);
            })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Result.RerankedRank > 0
                ? candidate.Result.RerankedRank
                : candidate.Result.OriginalRank)
            .ToList();
    }

    internal static string DescribeCapability(string capabilityId)
    {
        var canonical = ComponentCapabilityAliasCatalog.Canonicalize(capabilityId);
        return canonical switch
        {
            "analyze.image.semantic" =>
                "multimodal vision image captioning object scene understanding model",
            "analyze.audio.semantic" =>
                "audio understanding classification captioning model",
            "analyze.video.semantic" =>
                "video understanding captioning temporal scene analysis model",
            "extract.image_ocr" => "OCR printed text recognition engine",
            "read.image_pixels" => "image decoder pixel inspection library",
            "read.image_extended" => "image metadata inspection library",
            "extract.audio_transcript.multilingual" =>
                "multilingual speech to text transcription model",
            "extract.video_frames" => "video frame extraction decoder library",
            "read.audio" => "audio decoder metadata inspection library",
            "read.video" => "video decoder metadata inspection library",
            "edit.image" => "image editing transformation engine",
            "edit.audio" => "audio editing transformation engine",
            "edit.video" => "video editing transformation engine",
            "generate.image" => "text to image generation model",
            "generate.audio" => "audio generation model",
            "generate.video" => "video generation model",
            _ => Humanize(canonical) + " local execution component"
        };
    }

    private static IEnumerable<string> Tokenize(string? value) =>
        Regex.Matches((value ?? string.Empty).ToLowerInvariant(), @"[\p{L}\p{N}]{4,}")
            .Select(match => match.Value)
            .Where(token => token is not "with" and not "from" and not "that"
                and not "this" and not "local" and not "open" and not "source"
                and not "windows" and not "model" and not "runtime");

    private static bool IsTrustedDiscoveryHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Host.EndsWith("github.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith("huggingface.co", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith("microsoft.com", StringComparison.OrdinalIgnoreCase));

    internal static string ClassifyCandidateKind(WebSearchResult result)
    {
        if (LooksLikeReferenceMaterial(result))
        {
            return ExternalComponentCandidateKinds.InformationalReference;
        }

        if (LooksLikeDirectReleaseOrPackage(result))
        {
            return result.Url.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase)
                ? ExternalComponentCandidateKinds.ReleaseAsset
                : ExternalComponentCandidateKinds.Package;
        }

        if (Uri.TryCreate(result.Url, UriKind.Absolute, out var uri))
        {
            if (uri.Host.EndsWith("huggingface.co", StringComparison.OrdinalIgnoreCase))
            {
                return ExternalComponentCandidateKinds.ModelRepository;
            }

            if (uri.Host.EndsWith("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return ExternalComponentCandidateKinds.SourceRepository;
            }
        }

        return ExternalComponentCandidateKinds.InformationalReference;
    }

    internal static string ClassifyAcquisitionStatus(WebSearchResult result) =>
        ClassifyCandidateKind(result) switch
        {
            ExternalComponentCandidateKinds.ModelRepository or
            ExternalComponentCandidateKinds.ReleaseAsset or
            ExternalComponentCandidateKinds.Package =>
                ExternalComponentAcquisitionStatuses.RecipeRequired,
            _ => ExternalComponentAcquisitionStatuses.ReferenceOnly
        };

    private static bool LooksLikeReferenceMaterial(WebSearchResult result)
    {
        var text = $"{result.Title} {result.Snippet} {result.Url}";
        return ReferenceMaterialMarkers.Any(marker =>
            text.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeDirectReleaseOrPackage(WebSearchResult result)
    {
        var url = result.Url;
        return url.Contains("/releases/download/", StringComparison.OrdinalIgnoreCase)
            || url.Contains("/resolve/", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".7z", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".whl", StringComparison.OrdinalIgnoreCase)
            || url.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly string[] ReferenceMaterialMarkers =
    [
        "benchmark",
        "comparison",
        "evaluation",
        "dataset",
        "paper",
        "tutorial",
        "notebook",
        ".ipynb",
        "course project"
    ];

    private static string Humanize(string value) =>
        Regex.Replace(value ?? string.Empty, "[._:/-]+", " ").Trim();

    private static string Shorten(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? string.Empty;
}

internal sealed record RankedDiscoveryCandidate(WebSearchResult Result, int Score);
