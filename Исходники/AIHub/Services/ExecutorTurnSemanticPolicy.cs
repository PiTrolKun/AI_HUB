using AIHub.Models;

namespace AIHub.Services;

public static class ExecutorTurnSemanticPolicy
{
    private static readonly string[] ProgramCommandPrefixes =
    [
        "завершить ",
        "закончить ",
        "сохранить ",
        "экспортировать ",
        "выдать результат",
        "показать результат",
        "запросить снимок",
        "собрать итог",
        "сформировать итог",
        "перейти к финалу",
        "finish ",
        "end session",
        "save ",
        "export ",
        "show result",
        "request result",
        "request snapshot",
        "build final",
        "create final",
        "go to final"
    ];

    public static bool IsAllowed(ExecutorTurnResult turn)
    {
        if (turn.CanFinalize
            && (turn.Action is not ExecutorTurnActions.AskUser
                    and not ExecutorTurnActions.SuggestFinalization
                || turn.Status != ExecutorTurnStatuses.Working
                || string.IsNullOrWhiteSpace(turn.CompletionReason)
                || string.IsNullOrWhiteSpace(turn.CurrentResultSummary)
                || !ExecutorWorkingResultPolicy.IsSubstantive(turn.WorkingResultFragment)
                || turn.MissingCriticalInputs.Count > 0))
        {
            return false;
        }

        if (turn.Action == ExecutorTurnActions.SuggestFinalization)
        {
            return turn.CanFinalize
                && turn.Options.Count == 0
                && string.IsNullOrWhiteSpace(turn.Question)
                && !string.IsNullOrWhiteSpace(turn.CompletionReason)
                && turn.MissingCriticalInputs.Count == 0;
        }

        if (turn.Action != ExecutorTurnActions.AskUser)
        {
            return true;
        }

        return !LooksLikeProgramCommand(turn.Question)
            && turn.Options.All(option =>
                !LooksLikeProgramCommand(option.Title)
                && (option.Intent != ExecutorOptionIntents.ApproveAction
                    || ExecutorToolCatalog.IsSessionFileTool(option.Action)));
    }

    public static bool LooksLikeProgramCommand(string value)
    {
        var normalized = Normalize(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (ProgramCommandPrefixes.Any(normalized.StartsWith))
        {
            return true;
        }

        return (normalized.Contains("заверш", StringComparison.Ordinal)
                && normalized.Contains("сесси", StringComparison.Ordinal))
            || normalized.Contains("инициировать сохран", StringComparison.Ordinal)
            || (normalized.Contains("отчет готов", StringComparison.Ordinal)
                && normalized.Contains("заверш", StringComparison.Ordinal))
            || (normalized.Contains("result is ready", StringComparison.Ordinal)
                && normalized.Contains("finish", StringComparison.Ordinal));
    }

    private static string Normalize(string value) =>
        string.Join(
            ' ',
            value.Trim().ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        .Trim(' ', '.', '!', '?', ':', ';', '"', '\'', '`');
}
