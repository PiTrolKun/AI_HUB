using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public static class ExecutorResultParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static bool TryReadTurn(string response, out ExecutorTurnResult result)
    {
        result = new ExecutorTurnResult();
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ExecutorTurnResult>(response[start..(end + 1)], JsonOptions);
            if (parsed is null)
            {
                return false;
            }

            parsed.Status = NormalizeStatus(parsed.Status);
            parsed.Thought = parsed.Thought.Trim();
            parsed.Question = parsed.Question.Trim();
            parsed.Result = parsed.Result.Trim();
            parsed.Options = NormalizeList(parsed.Options, 6);
            parsed.Sources = NormalizeList(parsed.Sources, 24);
            parsed.Warnings = NormalizeList(parsed.Warnings, 24);

            var valid = parsed.Status switch
            {
                ExecutorTurnStatuses.Clarification => !string.IsNullOrWhiteSpace(parsed.Question)
                    && (parsed.Options.Count > 0 || parsed.AllowCustom),
                ExecutorTurnStatuses.Final => !string.IsNullOrWhiteSpace(parsed.Result),
                ExecutorTurnStatuses.CannotContinue => !string.IsNullOrWhiteSpace(parsed.Result)
                    || !string.IsNullOrWhiteSpace(parsed.Thought),
                _ => false
            };
            if (!valid)
            {
                return false;
            }

            result = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string NormalizeStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "needs_clarification" => ExecutorTurnStatuses.Clarification,
        "clarification" => ExecutorTurnStatuses.Clarification,
        "completed" => ExecutorTurnStatuses.Final,
        "complete" => ExecutorTurnStatuses.Final,
        "final" => ExecutorTurnStatuses.Final,
        var value => value
    };

    private static List<string> NormalizeList(IEnumerable<string>? values, int maximum) =>
        (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maximum)
            .ToList();
}
