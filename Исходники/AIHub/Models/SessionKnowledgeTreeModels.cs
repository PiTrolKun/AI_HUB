namespace AIHub.Models;

public static class SessionKnowledgeNodeTypes
{
    public const string Task = "task";
    public const string Requirement = "requirement";
    public const string Decision = "decision";
    public const string Knowledge = "knowledge";
    public const string ResultFragment = "result_fragment";
    public const string OpenQuestion = "open_question";
    public const string Assumption = "assumption";
    public const string Source = "source";

    public static bool IsKnown(string value) => value is
        Task or
        Requirement or
        Decision or
        Knowledge or
        ResultFragment or
        OpenQuestion or
        Assumption or
        Source;
}

public sealed class SessionKnowledgeNode
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Type { get; set; } = SessionKnowledgeNodeTypes.Knowledge;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string StageId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsResolved { get; set; }
    public bool IsStructural { get; set; }
    public int Sequence { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SessionKnowledgeTreeSnapshot
{
    public int Version { get; set; }
    public int Sequence { get; set; }
    public string LanguageCode { get; set; } = "ru";
    public string RootId { get; set; } = string.Empty;
    public string ActiveNodeId { get; set; } = string.Empty;
    public string RequirementsRootId { get; set; } = string.Empty;
    public string DecisionsRootId { get; set; } = string.Empty;
    public string KnowledgeRootId { get; set; } = string.Empty;
    public string ResultRootId { get; set; } = string.Empty;
    public string QuestionsRootId { get; set; } = string.Empty;
    public string AssumptionsRootId { get; set; } = string.Empty;
    public string SourcesRootId { get; set; } = string.Empty;
    public string ActiveConversationParentId { get; set; } = string.Empty;
    public string? PendingQuestionId { get; set; }
    public List<string> PendingOptions { get; set; } = [];
    public List<SessionKnowledgeNode> Nodes { get; set; } = [];

    public bool HasNodes => Nodes.Count > 0;
}

public sealed class SessionKnowledgeTreeChangedEventArgs : EventArgs
{
    public required string ChangeType { get; init; }
    public required string NodeId { get; init; }
    public required SessionKnowledgeTreeSnapshot Snapshot { get; init; }
}
