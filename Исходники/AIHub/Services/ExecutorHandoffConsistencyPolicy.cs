using AIHub.Models;

namespace AIHub.Services;

public static class ExecutorHandoffConsistencyPolicy
{
    private static readonly HashSet<string> WebToolNames =
    [
        "web_search",
        "web_research",
        "web_read"
    ];

    public static bool Normalize(ExecutorHandoffPackage handoff)
    {
        ArgumentNullException.ThrowIfNull(handoff);

        var explicitlyOffline = handoff.CapabilityProfile.Dimensions
                .Where(item => string.Equals(
                    item.Dimension,
                    ChoiceDecisionDimensions.ToolRequirements,
                    StringComparison.OrdinalIgnoreCase))
                .SelectMany(item => item.Values)
                .Any(IsOfflineValue)
            || handoff.Constraints.Any(IsOfflineValue)
            || handoff.UserSignals.Any(item => IsOfflineValue(item.Value));

        var changed = false;
        if (explicitlyOffline && handoff.NeedsWeb)
        {
            handoff.NeedsWeb = false;
            changed = true;
        }

        if (!handoff.NeedsWeb)
        {
            changed |= handoff.RequiredTools.RemoveAll(WebToolNames.Contains) > 0;
        }

        return changed;
    }

    private static bool IsOfflineValue(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized.Contains("без интернета", StringComparison.Ordinal)
            || normalized.Contains("no web", StringComparison.Ordinal)
            || normalized.Contains("no_web", StringComparison.Ordinal)
            || normalized.Contains("offline", StringComparison.Ordinal)
            || normalized.Contains("интернет не", StringComparison.Ordinal);
    }
}
