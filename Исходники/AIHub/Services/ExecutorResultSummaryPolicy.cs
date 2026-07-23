namespace AIHub.Services;

public static class ExecutorResultSummaryPolicy
{
    public const int MaximumCharacters = 900;

    public static string Clamp(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length <= MaximumCharacters)
        {
            return normalized;
        }

        var cut = normalized.LastIndexOf(' ', MaximumCharacters - 1, MaximumCharacters);
        if (cut < MaximumCharacters * 3 / 4)
        {
            cut = MaximumCharacters - 1;
        }

        return normalized[..cut].TrimEnd() + "…";
    }
}
