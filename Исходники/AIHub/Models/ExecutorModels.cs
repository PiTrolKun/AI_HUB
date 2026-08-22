using System.Text.Json;
using System.Text.Json.Serialization;

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
    public ModelSemanticPassport SemanticPassport { get; set; } = new();
}

public sealed class ModelSemanticPassport
{
    public int SchemaVersion { get; set; } = 1;
    public string Status { get; set; } = ModelSemanticPassportStatuses.Missing;
    public string DescriptionRu { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string GeneratorModel { get; set; } = string.Empty;
    public string FactsHash { get; set; } = string.Empty;
    public DateTimeOffset? GeneratedAt { get; set; }
    public string LastError { get; set; } = string.Empty;
}

public static class ModelSemanticPassportStatuses
{
    public const string Missing = "missing";
    public const string Pending = "pending";
    public const string Generated = "generated";
    public const string Failed = "failed";
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

    public string ParentRunId { get; set; } = string.Empty;

    public SessionFilePromptManifest FileManifest { get; set; } = new();

    public WorkPatternSelectionResult WorkPatterns { get; set; } = new();

    public ArtifactContract ArtifactContract { get; set; } = new();

    public ExecutionOutcomeContract OutcomeContract { get; set; } = new();

    public ExecutionBundlePlan ExecutionBundle { get; set; } = new();
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
    public const string RequestCapability = "request_capability";
    public const string SuggestFinalization = "suggest_finalization";
    public const string Blocked = "blocked";
}

public static class ExecutorOptionIntents
{
    public const string Answer = "answer";
    public const string ApproveAction = "approve_action";
    public const string DeclineAction = "decline_action";
}

[JsonConverter(typeof(ExecutorTurnOptionJsonConverter))]
public sealed class ExecutorTurnOption
{
    public string Title { get; set; } = string.Empty;
    public string Intent { get; set; } = ExecutorOptionIntents.Answer;
    public string Action { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }

    public static implicit operator ExecutorTurnOption(string title) => new()
    {
        Title = title
    };
}

public sealed class ExecutorTurnOptionJsonConverter : JsonConverter<ExecutorTurnOption>
{
    public override ExecutorTurnOption Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new ExecutorTurnOption
            {
                Title = reader.GetString() ?? string.Empty
            };
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        return new ExecutorTurnOption
        {
            Title = ReadString(root, "title"),
            Intent = ReadString(root, "intent", ExecutorOptionIntents.Answer),
            Action = ReadString(root, "action"),
            TargetId = ReadString(root, "targetId"),
            Effect = ReadString(root, "effect"),
            IsRecommended = root.TryGetProperty("isRecommended", out var recommended)
                && recommended.ValueKind is JsonValueKind.True or JsonValueKind.False
                && recommended.GetBoolean()
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        ExecutorTurnOption value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("title", value.Title);
        writer.WriteString("intent", value.Intent);
        writer.WriteString("action", value.Action);
        writer.WriteString("targetId", value.TargetId);
        writer.WriteString("effect", value.Effect);
        writer.WriteBoolean("isRecommended", value.IsRecommended);
        writer.WriteEndObject();
    }

    private static string ReadString(
        JsonElement element,
        string name,
        string fallback = "") =>
        element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;
}

public sealed class ExecutorTurnResult
{
    public string Status { get; set; } = ExecutorTurnStatuses.Working;
    public string Action { get; set; } = ExecutorTurnActions.AskUser;
    public string StageId { get; set; } = string.Empty;
    public string StageSummary { get; set; } = string.Empty;
    public string Thought { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public List<ExecutorTurnOption> Options { get; set; } = [];
    public bool AllowCustom { get; set; } = true;
    public string CurrentResultSummary { get; set; } = string.Empty;
    public string WorkingResultFragment { get; set; } = string.Empty;
    public bool CanFinalize { get; set; }
    public string CompletionReason { get; set; } = string.Empty;
    public List<string> RequestedTools { get; set; } = [];
    public List<ExecutorCapabilityRequest> RequestedCapabilities { get; set; } = [];
    public string RequestedCapability { get; set; } = string.Empty;
    public string CapabilityReason { get; set; } = string.Empty;
    public bool CapabilityRequired { get; set; }
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
    public bool IsFinal { get; set; }
    public string ArtifactPath { get; set; } = string.Empty;
    public string ArtifactKind { get; set; } = string.Empty;
    public string ArtifactMimeType { get; set; } = string.Empty;
    public string ArtifactQualityLevel { get; set; } = ArtifactQualityLevels.Emergency;
    public string ArtifactValidationStatus { get; set; } = ArtifactValidationStatuses.Pending;
    public string EvidenceValidationStatus { get; set; } = EvidenceValidationStatuses.Pending;
    public string TaskFulfillmentStatus { get; set; } = TaskFulfillmentStatuses.Pending;
    public string ActionGraphId { get; set; } = string.Empty;
    public List<string> EvidenceReceiptIds { get; set; } = [];
    public string RecipeId { get; set; } = string.Empty;
    public List<string> ArtifactWarnings { get; set; } = [];
    public string DisplayName => $"v{Version} - {Title}";
}
