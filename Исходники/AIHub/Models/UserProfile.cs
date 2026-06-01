namespace AIHub.Models;

public sealed class UserProfile
{
    public int ProfileVersion { get; set; } = 1;

    public string DisplayName { get; set; } = string.Empty;

    public UserLocation Location { get; set; } = new();

    public UserAnswerPreferences AnswerPreferences { get; set; } = new();

    public string WorkloadMode { get; set; } = UserWorkloadModes.Balanced;

    public bool IsComplete()
    {
        return !string.IsNullOrWhiteSpace(DisplayName)
            && HasManualLocation()
            && AnswerPreferences.HasAnySelected()
            && UserWorkloadModes.IsKnown(WorkloadMode);
    }

    public bool HasManualLocation()
    {
        return Location is not null
            && string.Equals(Location.Mode, "manual", StringComparison.OrdinalIgnoreCase)
            && (!string.IsNullOrWhiteSpace(Location.City)
                || !string.IsNullOrWhiteSpace(Location.Region)
                || !string.IsNullOrWhiteSpace(Location.Country));
    }
}

public sealed class UserAnswerPreferences
{
    public bool Concise { get; set; }

    public bool Detailed { get; set; }

    public bool SimpleLanguage { get; set; }

    public bool StepByStep { get; set; }

    public bool Examples { get; set; }

    public bool SourcesWhenSearching { get; set; }

    public bool WarnAboutRisks { get; set; }

    public bool HasAnySelected()
    {
        return Concise
            || Detailed
            || SimpleLanguage
            || StepByStep
            || Examples
            || SourcesWhenSearching
            || WarnAboutRisks;
    }
}

public static class UserWorkloadModes
{
    public const string Light = "light";

    public const string Balanced = "balanced";

    public const string Extreme = "extreme";

    public static bool IsKnown(string? mode)
    {
        return string.Equals(mode, Light, StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, Balanced, StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, Extreme, StringComparison.OrdinalIgnoreCase);
    }
}
