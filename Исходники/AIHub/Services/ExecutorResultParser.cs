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
            parsed.StageSummary = parsed.StageSummary.Trim();
            parsed.Thought = parsed.Thought.Trim();
            parsed.Question = parsed.Question.Trim();
            parsed.Result = parsed.Result.Trim();
            parsed.Options = NormalizeList(parsed.Options, 6);
            parsed.Sources = NormalizeList(parsed.Sources, 24);
            parsed.Warnings = NormalizeList(parsed.Warnings, 24);
            if (parsed.Status is ExecutorTurnStatuses.Working or ExecutorTurnStatuses.StageReady)
            {
                parsed.Result = string.Empty;
            }
            else if (parsed.Status == ExecutorTurnStatuses.Blocked
                && !IsMeaningfulResult(parsed.Result))
            {
                parsed.Result = string.Empty;
            }

            var valid = parsed.Status switch
            {
                ExecutorTurnStatuses.Working => !string.IsNullOrWhiteSpace(parsed.Question)
                    && (parsed.Options.Count > 0 || parsed.AllowCustom),
                ExecutorTurnStatuses.StageReady => !string.IsNullOrWhiteSpace(parsed.StageSummary)
                    && (!string.IsNullOrWhiteSpace(parsed.Question) || parsed.AllowCustom),
                ExecutorTurnStatuses.ResultReady => IsMeaningfulResult(parsed.Result),
                ExecutorTurnStatuses.Blocked => IsMeaningfulResult(parsed.Result)
                    || !string.IsNullOrWhiteSpace(parsed.Thought),
                _ => false
            };
            if (!valid)
            {
                return false;
            }

            if (parsed.Status is ExecutorTurnStatuses.StageReady
                or ExecutorTurnStatuses.ResultReady
                or ExecutorTurnStatuses.Blocked)
            {
                parsed.AllowCustom = true;
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
        "clarification_step" => ExecutorTurnStatuses.Working,
        "needs_clarification" => ExecutorTurnStatuses.Working,
        "clarification" => ExecutorTurnStatuses.Working,
        "final_result" => ExecutorTurnStatuses.ResultReady,
        "completed" => ExecutorTurnStatuses.ResultReady,
        "complete" => ExecutorTurnStatuses.ResultReady,
        "final" => ExecutorTurnStatuses.ResultReady,
        "cannot_continue" => ExecutorTurnStatuses.Blocked,
        var value => value
    };

    private static bool IsMeaningfulResult(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = string.Join(
                ' ',
                value.Trim().ToLowerInvariant()
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Trim(' ', '.', '!', '?', ':', ';', '"', '\'', '`');
        if (normalized is "final_result"
            or "result_ready"
            or "stage_ready"
            or "working"
            or "completed"
            or "complete"
            or "done"
            or "готово"
            or "результат готов")
        {
            return false;
        }

        string[] intentOnlyPrefixes =
        [
            "приступаю ",
            "создаю ",
            "готовлю ",
            "формирую ",
            "сейчас создам",
            "сейчас подготовлю",
            "начинаю создавать",
            "начинаю готовить",
            "i will create",
            "i will prepare",
            "i am creating",
            "i am preparing"
        ];
        return !intentOnlyPrefixes.Any(normalized.StartsWith);
    }

    private static List<string> NormalizeList(IEnumerable<string>? values, int maximum) =>
        (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maximum)
            .ToList();
}
