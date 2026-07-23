namespace AIHub.Services;

public static class ExecutorWorkingResultPolicy
{
    public const int MaximumCharacters = 2400;

    private static readonly string[] MetaPrefixes =
    [
        "формируется ",
        "готовится ",
        "создаётся ",
        "сейчас доступен ",
        "доступен предварительный ",
        "будет подготовлен",
        "будет создан",
        "следующий шаг",
        "техническое задание",
        "план статьи",
        "план документа",
        "необходимо подготовить",
        "необходимо написать",
        "is being prepared",
        "is being created",
        "a draft is available",
        "currently available",
        "will be prepared",
        "will be created",
        "next step",
        "technical brief",
        "task specification",
        "article plan",
        "document plan",
        "the document should"
    ];

    public static string Clamp(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length <= MaximumCharacters)
        {
            return normalized;
        }

        return normalized[..(MaximumCharacters - 1)].TrimEnd() + "…";
    }

    public static bool IsSubstantive(string? value)
    {
        var normalized = Clamp(value);
        if (normalized.Length < 40)
        {
            return false;
        }

        return !LooksLikeMetaDescription(normalized);
    }

    public static bool LooksLikeMetaDescription(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        var beginning = normalized[..Math.Min(normalized.Length, 180)]
            .TrimStart('#', '*', ' ', '\t', '\r', '\n')
            .ToLowerInvariant();
        return MetaPrefixes.Any(beginning.StartsWith);
    }

    public static bool LooksLikeTaskSpecification(string? markdown)
    {
        var normalized = markdown?.TrimStart() ?? string.Empty;
        var beginning = normalized[..Math.Min(normalized.Length, 260)]
            .TrimStart('#', '*', ' ', '\t', '\r', '\n')
            .ToLowerInvariant();
        string[] prefixes =
        [
            "техническое задание",
            "тз:",
            "план статьи",
            "план документа",
            "структура статьи",
            "task specification",
            "technical brief",
            "article plan",
            "document plan",
            "content outline"
        ];
        return prefixes.Any(beginning.StartsWith);
    }

    public static bool TaskSpecificationWasRequested(string? confirmedBrief)
    {
        var value = confirmedBrief?.ToLowerInvariant() ?? string.Empty;
        string[] explicitRequests =
        [
            "создать техническое задание",
            "составить техническое задание",
            "подготовить техническое задание",
            "написать тз",
            "составить тз",
            "create a technical brief",
            "prepare a technical brief",
            "write a task specification",
            "create a task specification"
        ];
        return explicitRequests.Any(value.Contains);
    }
}
