using AIHub.Models;

namespace AIHub.Services;

public static class ExecutorContextBudgetManager
{
    public const double CompactAtPercent = 80;

    public static ExecutorContextBudget Measure(
        IEnumerable<string> content,
        int contextLimit,
        int reservedTokens = 2048)
    {
        var safeLimit = Math.Max(1, contextLimit);
        var safeReserve = Math.Clamp(reservedTokens, 0, Math.Max(0, safeLimit - 1));
        var workingLimit = Math.Max(1, safeLimit - safeReserve);
        var estimatedTokens = content
            .Where(value => !string.IsNullOrEmpty(value))
            .Sum(EstimateTokens);
        var fillPercent = estimatedTokens * 100d / workingLimit;
        return new ExecutorContextBudget(
            safeLimit,
            safeReserve,
            estimatedTokens,
            Math.Round(fillPercent, 2),
            fillPercent >= CompactAtPercent);
    }

    private static int EstimateTokens(string value) =>
        Math.Max(1, (int)Math.Ceiling(value.Length / 3.2d));
}
