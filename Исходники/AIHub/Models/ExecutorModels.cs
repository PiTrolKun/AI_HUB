namespace AIHub.Models;

public sealed class ExecutorModelArtifact
{
    public string RequestedModel { get; set; } = string.Empty;
    public string RepoId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string Quantization { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsInstalled { get; set; }
    public string InstalledPath { get; set; } = string.Empty;
}

public sealed class ExecutorModelManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string Role { get; set; } = "executor";
    public string RequestedModel { get; set; } = string.Empty;
    public string RepoId { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string Format { get; set; } = "gguf";
    public string Quantization { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string Status { get; set; } = "missing";
    public string RuntimeBackend { get; set; } = string.Empty;
    public string RuntimeError { get; set; } = string.Empty;
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? RuntimeVerifiedAt { get; set; }
}

public sealed record ExecutorDownloadProgress(
    long DownloadedBytes,
    long TotalBytes,
    double BytesPerSecond,
    string Stage);

public sealed class ExecutorHandoffPackage
{
    public string SuggestedDirection { get; set; } = string.Empty;
    public List<ExecutorHandoffItem> ProgramFacts { get; set; } = [];
    public List<ExecutorHandoffItem> UserSignals { get; set; } = [];
    public List<ExecutorHandoffItem> CoreHypotheses { get; set; } = [];
    public ChoiceCapabilityProfile CapabilityProfile { get; set; } = new();
    public List<string> Unknowns { get; set; } = [];
    public string Goal { get; set; } = string.Empty;
    public List<string> Criteria { get; set; } = [];
    public List<string> Constraints { get; set; } = [];
    public bool NeedsWeb { get; set; }
    public List<string> RequiredTools { get; set; } = [];
    public string Prompt { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "ru";
    public string WorkloadMode { get; set; } = UserWorkloadModes.Balanced;
    public UserAnswerPreferences AnswerPreferences { get; set; } = new();
    public string ParentCoreSessionId { get; set; } = string.Empty;
}

public sealed class ExecutorHandoffItem
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsAuthoritative { get; set; }
}

public sealed record ModelStreamChunk(string Text, bool IsComplete = false);

public sealed record ExecutorContextBudget(
    int ContextLimit,
    int ReservedTokens,
    int EstimatedUsedTokens,
    double FillPercent,
    bool ShouldCompact);

public static class ExecutorTurnStatuses
{
    public const string Working = "working";
    public const string StageReady = "stage_ready";
    public const string Blocked = "blocked";
}

public static class ExecutorTurnActions
{
    public const string AskUser = "ask_user";
    public const string ConfirmBrief = "confirm_brief";
    public const string RequestTool = "request_tool";
    public const string Blocked = "blocked";
}

public sealed class ExecutorTurnResult
{
    public string Status { get; set; } = ExecutorTurnStatuses.Working;
    public string Action { get; set; } = ExecutorTurnActions.AskUser;
    public string StageId { get; set; } = string.Empty;
    public string StageSummary { get; set; } = string.Empty;
    public string Thought { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = [];
    public bool AllowCustom { get; set; } = true;
    public string CurrentResultSummary { get; set; } = string.Empty;
    public List<string> RequestedTools { get; set; } = [];
    public List<string> MissingCriticalInputs { get; set; } = [];
    public List<string> Assumptions { get; set; } = [];
    public string Result { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class ExecutorResultSnapshot
{
    public string Id { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string StageId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public string DisplayName => $"v{Version} - {Title}";
}
