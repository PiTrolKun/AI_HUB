using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed class CoordinatorMatchScoringService
{
    public (int Percent, string Reason) Score(
        ChoiceExecutorPoolCandidate candidate,
        IReadOnlyList<SandboxWorkPattern> patterns,
        ChoiceCapabilityProfile profile,
        bool installed)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(profile);

        var taskTerms = BuildTaskTerms(patterns, profile);
        var candidateTerms = Tokenize(string.Join(
            ' ',
            candidate.Model,
            candidate.Family,
            candidate.PipelineTag,
            candidate.ModelType,
            string.Join(' ', candidate.Directions),
            string.Join(' ', candidate.Roles),
            candidate.SemanticDescriptionRu,
            candidate.SemanticDescriptionEn,
            candidate.Evidence));
        var overlap = taskTerms.Intersect(
            candidateTerms,
            StringComparer.OrdinalIgnoreCase).Count();
        var score = 35;
        var reasons = new List<string>
        {
            "compatible coordinator runtime"
        };
        if (candidate.RuntimeCompatible)
        {
            score += 15;
        }

        if (!string.Equals(
                candidate.HardwareStatus,
                "incompatible",
                StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
            reasons.Add("PC fit was not rejected");
        }

        if (installed)
        {
            score += 10;
            reasons.Add("already installed and runtime-verified");
        }

        if (overlap > 0)
        {
            score += Math.Min(25, overlap * 5);
            reasons.Add($"{overlap} task/passport term matches");
        }
        else
        {
            reasons.Add("no specific task/passport term match");
        }

        if (candidate.ParameterCount is > 8_000_000_000)
        {
            score += 5;
            reasons.Add("above the default 8B coordinator floor");
        }

        return (Math.Clamp(score, 1, 99), string.Join("; ", reasons));
    }

    private static HashSet<string> BuildTaskTerms(
        IReadOnlyList<SandboxWorkPattern> patterns,
        ChoiceCapabilityProfile profile)
    {
        var patternTerms = patterns.SelectMany(pattern => new[]
            {
                pattern.Id,
                pattern.NameEn,
                pattern.DescriptionEn,
                string.Join(' ', pattern.Signals),
                string.Join(' ', pattern.RequiredCapabilities),
                string.Join(' ', pattern.OptionalCapabilities)
            });
        var profileTerms = profile.Dimensions.SelectMany(dimension =>
            dimension.Values.Append(dimension.Dimension));
        return Tokenize(string.Join(' ', patternTerms.Concat(profileTerms)));
    }

    private static HashSet<string> Tokenize(string value) =>
        Regex.Matches(value?.ToLowerInvariant() ?? string.Empty, @"[\p{L}\p{Nd}]{3,}")
            .Select(match => match.Value)
            .Where(token => token is not "model" and not "task" and not "work"
                and not "with" and not "from" and not "this" and not "that")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
