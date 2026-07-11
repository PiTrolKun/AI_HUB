using System.Globalization;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public static partial class ChoiceExecutorPolicy
{
    public const string Above8B = "above_8b";
    public const string EightBOrLess = "eight_b_or_less";
    public const string NonLlmSpecialist = "non_llm_specialist";

    public static bool Validate(
        ChoiceTaskCard card,
        string workloadMode,
        bool modelSearchUnavailable,
        string currentCoreName,
        out string error) => Validate(
            card,
            workloadMode,
            modelSearchUnavailable,
            currentCoreName,
            null,
            out error);

    public static bool Validate(
        ChoiceTaskCard card,
        string workloadMode,
        bool modelSearchUnavailable,
        string currentCoreName,
        ModelCatalogCandidate? catalogCandidate,
        out string error)
    {
        error = string.Empty;
        var isLightMode = string.Equals(workloadMode, UserWorkloadModes.Light, StringComparison.OrdinalIgnoreCase);
        var isCurrentCore = IsCurrentCore(card.RecommendedExecutor);
        var parameterCount = catalogCandidate?.ParameterCount is { } verifiedParameters
            ? verifiedParameters / 1_000_000_000d
            : TryReadParameterBillions(card.RecommendedExecutor);

        if (!isLightMode && isCurrentCore)
        {
            error = "Balanced and extreme modes forbid the current 8B core as worker. Choose a different above-8B model from the search results; core_fallback is forbidden.";
            return false;
        }

        if (card.ExecutorRole == "core_fallback")
        {
            if (!isLightMode)
            {
                error = "The 8B core is search/planning only unless the user selected light workload mode.";
                return false;
            }

            if (!isCurrentCore)
            {
                error = "core_fallback must point to current_core or the installed 8B core.";
                return false;
            }

            if (!modelSearchUnavailable)
            {
                error = "core_fallback is allowed only when model search is unavailable.";
                return false;
            }

            if (!string.Equals(card.ExecutorCapabilityClass, EightBOrLess, StringComparison.OrdinalIgnoreCase))
            {
                error = "current_core must use executorCapabilityClass=eight_b_or_less.";
                return false;
            }

            return true;
        }

        if (isCurrentCore)
        {
            error = "The current core must use executorRole=core_fallback in light mode.";
            return false;
        }

        if (IsSameFamilyWithoutSignificantGenerationAdvance(
                card.RecommendedExecutor,
                currentCoreName,
                catalogCandidate))
        {
            error = "A model from the current core family is allowed only when its name proves a significantly newer generation. A larger parameter count in the same generation is not enough.";
            return false;
        }

        if (!isLightMode)
        {
            if (!string.Equals(card.ExecutorCapabilityClass, Above8B, StringComparison.OrdinalIgnoreCase))
            {
                error = "Balanced and extreme workload modes require executorCapabilityClass=above_8b.";
                return false;
            }

            if (isCurrentCore || parameterCount is <= 8.0)
            {
                error = "Balanced and extreme workload modes require an executor stronger than 8B.";
                return false;
            }

            if (parameterCount is null)
            {
                error = "Balanced and extreme workload modes require verified parameter metadata above 8B.";
                return false;
            }
        }

        return true;
    }

    public static bool IsKnownCapabilityClass(string value) =>
        string.Equals(value, Above8B, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, EightBOrLess, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, NonLlmSpecialist, StringComparison.OrdinalIgnoreCase);

    public static bool IsExplicitlyAbove8B(string value) =>
        TryReadParameterBillions(value) is > 8.0;

    public static bool IsCandidateLineageAllowed(
        ModelCatalogCandidate candidate,
        string currentCoreName) =>
        string.IsNullOrWhiteSpace(currentCoreName)
        || !IsSameFamilyWithoutSignificantGenerationAdvance(candidate.RepoId, currentCoreName, candidate);

    private static bool IsSameFamilyWithoutSignificantGenerationAdvance(
        string candidate,
        string currentCore,
        ModelCatalogCandidate? catalogCandidate)
    {
        if (!TryReadFamilyGeneration(currentCore, out var coreFamily, out var coreGeneration))
        {
            return false;
        }

        var lineageSources = catalogCandidate?.BaseModels
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList() ?? [];
        var lineageMatches = FindFamilyGenerations(lineageSources, coreFamily);
        if (lineageSources.Count == 0 && catalogCandidate is not null)
        {
            lineageMatches = FindFamilyGenerations(
                [catalogCandidate.ModelType, catalogCandidate.RepoId],
                coreFamily);
        }
        if (catalogCandidate is null)
        {
            lineageMatches = FindFamilyGenerations([candidate], coreFamily);
        }

        return lineageMatches.Count > 0 && lineageMatches.Max() < coreGeneration + 0.5;
    }

    private static List<double> FindFamilyGenerations(IEnumerable<string> sources, string coreFamily) => sources
        .SelectMany(source => ModelGenerationRegex().Matches(source ?? string.Empty).Cast<Match>())
        .Select(match => new
        {
            Family = NormalizeFamily(match.Groups["family"].Value),
            Generation = ReadGeneration(match.Groups["generation"].Value)
        })
        .Where(value => value.Generation is not null
            && string.Equals(value.Family, coreFamily, StringComparison.OrdinalIgnoreCase))
        .Select(value => value.Generation!.Value)
        .ToList();

    private static bool TryReadFamilyGeneration(string value, out string family, out double generation)
    {
        var match = ModelGenerationRegex().Match(value);
        family = match.Success ? NormalizeFamily(match.Groups["family"].Value) : string.Empty;
        var parsed = match.Success ? ReadGeneration(match.Groups["generation"].Value) : null;
        generation = parsed ?? 0;
        return !string.IsNullOrWhiteSpace(family) && parsed is not null;
    }

    private static string NormalizeFamily(string value)
    {
        var parts = value.Split(['_', '-', '.'], StringSplitOptions.RemoveEmptyEntries);
        return (parts.LastOrDefault() ?? value).Trim();
    }

    private static double? ReadGeneration(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static bool IsCurrentCore(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "current_core" or "core" or "ядро"
            || normalized.Contains("qwen3-8b", StringComparison.Ordinal)
            || normalized.Contains("qwen3 8b", StringComparison.Ordinal);
    }

    private static double? TryReadParameterBillions(string value)
    {
        var match = ParameterCountRegex().Match(value);
        return match.Success
            && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
    }

    [GeneratedRegex(@"(?<![\d.])(\d+(?:\.\d+)?)\s*[bB]\b", RegexOptions.CultureInvariant)]
    private static partial Regex ParameterCountRegex();

    [GeneratedRegex(@"(?<family>[A-Za-z][A-Za-z_.-]{1,30})(?<generation>\d+(?:\.\d+)?)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex ModelGenerationRegex();
}
