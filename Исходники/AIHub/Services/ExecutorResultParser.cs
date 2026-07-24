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
            parsed.Action = NormalizeAction(parsed.Action, parsed.Status);
            parsed.StageSummary = parsed.StageSummary.Trim();
            parsed.Thought = parsed.Thought.Trim();
            parsed.Question = parsed.Question.Trim();
            parsed.CurrentResultSummary = ExecutorResultSummaryPolicy.Clamp(parsed.CurrentResultSummary);
            parsed.WorkingResultFragment = ExecutorWorkingResultPolicy.Clamp(parsed.WorkingResultFragment);
            parsed.CompletionReason = parsed.CompletionReason.Trim();
            parsed.Result = parsed.Result.Trim();
            parsed.Options = NormalizeOptions(parsed.Options);
            parsed.RequestedTools = NormalizeList(parsed.RequestedTools, 6);
            parsed.RequestedCapability = parsed.RequestedCapability.Trim().ToLowerInvariant();
            parsed.CapabilityReason = parsed.CapabilityReason.Trim();
            parsed.RequestedCapabilities = NormalizeCapabilities(
                parsed.RequestedCapabilities,
                parsed.RequestedCapability,
                parsed.CapabilityReason,
                parsed.CapabilityRequired);
            if (parsed.RequestedCapabilities.Count > 0)
            {
                var primary = parsed.RequestedCapabilities[0];
                parsed.RequestedCapability = primary.Id;
                parsed.CapabilityReason = primary.Purpose;
                parsed.CapabilityRequired = primary.Required;
            }
            parsed.MissingCriticalInputs = NormalizeList(parsed.MissingCriticalInputs, 12);
            parsed.Assumptions = NormalizeList(parsed.Assumptions, 12);
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
                ExecutorTurnStatuses.Working => parsed.Action switch
                {
                    ExecutorTurnActions.AskUser => !string.IsNullOrWhiteSpace(parsed.Question)
                        && (parsed.Options.Count > 0 || parsed.AllowCustom),
                    ExecutorTurnActions.RequestTool => parsed.RequestedTools.Count > 0,
                    ExecutorTurnActions.RequestCapability =>
                        parsed.RequestedCapabilities.Count > 0
                        && parsed.RequestedCapabilities.All(request =>
                            !string.IsNullOrWhiteSpace(request.Id)
                            && !string.IsNullOrWhiteSpace(request.Purpose)),
                    ExecutorTurnActions.SuggestFinalization =>
                        parsed.CanFinalize
                        &&
                        !string.IsNullOrWhiteSpace(parsed.CompletionReason)
                        && !string.IsNullOrWhiteSpace(parsed.CurrentResultSummary),
                    _ => false
                },
                ExecutorTurnStatuses.StageReady => !string.IsNullOrWhiteSpace(parsed.StageSummary)
                    && parsed.Action == ExecutorTurnActions.ConfirmBrief,
                ExecutorTurnStatuses.Blocked => IsMeaningfulResult(parsed.Result)
                    || parsed.Action == ExecutorTurnActions.Blocked
                    && !string.IsNullOrWhiteSpace(parsed.Thought),
                _ => false
            };
            if (!valid || !ExecutorTurnSemanticPolicy.IsAllowed(parsed))
            {
                return false;
            }

            if (parsed.Action == ExecutorTurnActions.AskUser
                || parsed.Status is ExecutorTurnStatuses.StageReady
                    or ExecutorTurnStatuses.Blocked)
            {
                parsed.AllowCustom = true;
            }

            if (parsed.Status == ExecutorTurnStatuses.StageReady)
            {
                parsed.Options.Clear();
            }
            else if (parsed.Action == ExecutorTurnActions.SuggestFinalization)
            {
                parsed.Question = string.Empty;
                parsed.Options.Clear();
                parsed.AllowCustom = false;
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
        "cannot_continue" => ExecutorTurnStatuses.Blocked,
        var value => value
    };

    private static string NormalizeAction(string action, string status)
    {
        var normalized = action.Trim().ToLowerInvariant() switch
        {
            "needs_user_input" => ExecutorTurnActions.AskUser,
            "await_user" => ExecutorTurnActions.AskUser,
            "confirm" => ExecutorTurnActions.ConfirmBrief,
            "tool" => ExecutorTurnActions.RequestTool,
            "capability" => ExecutorTurnActions.RequestCapability,
            "suggest_finish" => ExecutorTurnActions.SuggestFinalization,
            "ready_for_finalization" => ExecutorTurnActions.SuggestFinalization,
            var value => value
        };
        if (normalized == ExecutorTurnActions.AskUser
            && status != ExecutorTurnStatuses.Working)
        {
            return status switch
            {
                ExecutorTurnStatuses.StageReady => ExecutorTurnActions.ConfirmBrief,
                ExecutorTurnStatuses.Blocked => ExecutorTurnActions.Blocked,
                _ => normalized
            };
        }

        return normalized;
    }

    private static List<ExecutorTurnOption> NormalizeOptions(
        IEnumerable<ExecutorTurnOption>? values)
    {
        var result = new List<ExecutorTurnOption>();
        var signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recommendationUsed = false;
        foreach (var source in values ?? [])
        {
            if (source is null || string.IsNullOrWhiteSpace(source.Title))
            {
                continue;
            }

            var option = new ExecutorTurnOption
            {
                Title = source.Title.Trim(),
                Intent = NormalizeOptionIntent(source.Intent),
                Action = source.Action.Trim().ToLowerInvariant(),
                TargetId = source.TargetId.Trim(),
                Effect = source.Effect.Trim(),
                IsRecommended = source.IsRecommended && !recommendationUsed
            };
            if (option.Intent == ExecutorOptionIntents.ApproveAction
                && !IsAllowedOptionAction(option.Action))
            {
                continue;
            }

            if (option.Intent != ExecutorOptionIntents.ApproveAction)
            {
                option.Action = string.Empty;
                option.TargetId = string.Empty;
            }

            var signature = option.Intent == ExecutorOptionIntents.ApproveAction
                ? $"{option.Intent}|{option.Action}|{option.TargetId}"
                : $"{option.Intent}|{NormalizeOptionText(option.Title)}";
            if (!signatures.Add(signature))
            {
                continue;
            }

            recommendationUsed |= option.IsRecommended;
            result.Add(option);
            if (result.Count == 6)
            {
                break;
            }
        }

        return result;
    }

    private static string NormalizeOptionIntent(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            ExecutorOptionIntents.ApproveAction => ExecutorOptionIntents.ApproveAction,
            ExecutorOptionIntents.DeclineAction => ExecutorOptionIntents.DeclineAction,
            _ => ExecutorOptionIntents.Answer
        };

    private static bool IsAllowedOptionAction(string action) =>
        action is "session_files_list"
            or "session_file_inspect"
            or "session_file_read";

    private static string NormalizeOptionText(string value) =>
        string.Join(
            ' ',
            value.Trim().ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

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

    private static List<ExecutorCapabilityRequest> NormalizeCapabilities(
        IEnumerable<ExecutorCapabilityRequest>? values,
        string legacyCapability,
        string legacyReason,
        bool legacyRequired)
    {
        var normalized = (values ?? [])
            .Where(value => value is not null && !string.IsNullOrWhiteSpace(value.Id))
            .Select(value => new ExecutorCapabilityRequest
            {
                Id = value.Id.Trim().ToLowerInvariant(),
                Purpose = value.Purpose.Trim(),
                Required = value.Required,
                Alternatives = NormalizeList(value.Alternatives, 6)
                    .Select(item => item.ToLowerInvariant())
                    .ToList()
            })
            .GroupBy(value => value.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(8)
            .ToList();
        if (normalized.Count == 0 && !string.IsNullOrWhiteSpace(legacyCapability))
        {
            normalized.Add(new ExecutorCapabilityRequest
            {
                Id = legacyCapability.Trim().ToLowerInvariant(),
                Purpose = legacyReason.Trim(),
                Required = legacyRequired
            });
        }

        return normalized;
    }
}
