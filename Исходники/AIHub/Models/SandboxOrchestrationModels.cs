using System.Text;

namespace AIHub.Models;

public sealed class WorkPatternCatalogDocument
{
    public int SchemaVersion { get; set; } = 1;

    public List<SandboxWorkPattern> Patterns { get; set; } = [];
}

public sealed class SandboxWorkPattern
{
    public string Id { get; set; } = string.Empty;

    public string NameRu { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string DescriptionRu { get; set; } = string.Empty;

    public string DescriptionEn { get; set; } = string.Empty;

    public List<string> Signals { get; set; } = [];

    public List<string> InputCategories { get; set; } = [];

    public List<string> InputFormats { get; set; } = [];

    public List<string> ArtifactTypes { get; set; } = [];

    public List<string> RequiredCapabilities { get; set; } = [];

    public List<string> OptionalCapabilities { get; set; } = [];

    public List<string> CommonCombinations { get; set; } = [];

    public List<string> PreferredRecipe { get; set; } = [];

    public List<string> DegradedRecipe { get; set; } = [];

    public List<string> RouteChangingQuestions { get; set; } = [];

    public List<string> ValidationRules { get; set; } = [];

    public List<string> Risks { get; set; } = [];
}

public sealed class WorkPatternSelectionResult
{
    public List<WorkPatternSelection> Selections { get; set; } = [];

    public List<string> MissingData { get; set; } = [];

    public string Source { get; set; } = string.Empty;

    public bool UsedFallback { get; set; }
}

public sealed class WorkPatternSelection
{
    public string PatternId { get; set; } = string.Empty;

    public int MatchPercent { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public sealed class ExternalComponentDiscoveryReport
{
    public List<ExternalComponentDiscoverySearch> Searches { get; set; } = [];

    public bool HasCandidates => Searches.Any(search => search.Candidates.Count > 0);

    public int CandidateCount => Searches.Sum(search => search.Candidates.Count);

    public ExternalComponentDiscoveryCandidate? FindBestCandidate() => Searches
        .SelectMany(search => search.Candidates)
        .OrderByDescending(candidate => candidate.RelevanceScore)
        .FirstOrDefault();

    public ExternalComponentDiscoveryCandidate? FindBestInstallableCandidate() => Searches
        .SelectMany(search => search.Candidates)
        .Where(candidate => string.Equals(
            candidate.AcquisitionStatus,
            ExternalComponentAcquisitionStatuses.VerifiedInstallable,
            StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(candidate => candidate.RelevanceScore)
        .FirstOrDefault();

    public bool CoversCapabilities(IEnumerable<string> capabilityIds)
    {
        var required = capabilityIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return required.Count > 0
            && required.All(capabilityId => Searches.Any(search =>
                string.Equals(
                    search.CapabilityId,
                    capabilityId,
                    StringComparison.OrdinalIgnoreCase)));
    }

    public string ToPromptText()
    {
        if (Searches.Count == 0)
        {
            return "external_discovery_status=no_unknown_capabilities";
        }

        var builder = new StringBuilder();
        builder.AppendLine("external_discovery_status=completed_unverified");
        builder.AppendLine(
            "external_discovery_warning=Search results are untrusted leads only. They are not installed, approved or callable.");
        foreach (var search in Searches)
        {
            builder.AppendLine(
                $"capability={search.CapabilityId}; query={search.Query}; provider={search.Provider}; saved={search.SavedPath}");
            foreach (var candidate in search.Candidates)
            {
                builder.AppendLine(
                    $"- score={candidate.RelevanceScore}; kind={candidate.CandidateKind}; acquisition={candidate.AcquisitionStatus}; title={candidate.Title}; url={candidate.Url}; snippet={candidate.Snippet}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}

public sealed class ExternalComponentDiscoverySearch
{
    public string CapabilityId { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string SavedPath { get; set; } = string.Empty;

    public List<ExternalComponentDiscoveryCandidate> Candidates { get; set; } = [];
}

public sealed class ExternalComponentDiscoveryCandidate
{
    public int RelevanceScore { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public string CandidateKind { get; set; } =
        ExternalComponentCandidateKinds.InformationalReference;

    public string AcquisitionStatus { get; set; } =
        ExternalComponentAcquisitionStatuses.ReferenceOnly;
}

public static class ExternalComponentCandidateKinds
{
    public const string InformationalReference = "informational_reference";
    public const string SourceRepository = "source_repository";
    public const string ModelRepository = "model_repository";
    public const string ReleaseAsset = "release_asset";
    public const string Package = "package";
}

public static class ExternalComponentAcquisitionStatuses
{
    public const string ReferenceOnly = "reference_only";
    public const string RecipeRequired = "recipe_required";
    public const string VerifiedInstallable = "verified_installable";
}

public static class ArtifactKinds
{
    public const string Text = "text";
    public const string Document = "document";
    public const string Table = "table";
    public const string Image = "image";
    public const string Audio = "audio";
    public const string Video = "video";
    public const string Presentation = "presentation";
    public const string Code = "code";
    public const string Archive = "archive";
    public const string File = "file";
}

public sealed class ArtifactContract
{
    public List<string> InputFileNames { get; set; } = [];

    public List<string> InputFormats { get; set; } = [];

    public string ArtifactKind { get; set; } = ArtifactKinds.Text;

    public bool RequiresExternalEvidence { get; set; }

    public string PreferredExtension { get; set; } = ".txt";

    public string MimeType { get; set; } = "text/plain";

    public List<string> RequiredProperties { get; set; } = [];

    public string EmergencyAcceptableResult { get; set; } = string.Empty;

    public List<string> ValidationRules { get; set; } = [];

    public bool UserClarificationAllowed { get; set; } = true;

    public string QualityTarget { get; set; } = "best_effort";

    public int MaximumRuntimeSeconds { get; set; }

    public long MaximumMemoryBytes { get; set; }

    public long MaximumDiskBytes { get; set; }
}

public static class ExecutionOutcomeActionKinds
{
    public const string InputAccess = "input_access";

    public const string ContentUnderstanding = "content_understanding";

    public const string Transformation = "transformation";

    public const string ArtifactProduction = "artifact_production";

    public const string Validation = "validation";
}

public sealed class ExecutionOutcomeContract
{
    public string Goal { get; set; } = string.Empty;

    public string ArtifactKind { get; set; } = ArtifactKinds.Text;

    public List<ExecutionOutcomeAction> Actions { get; set; } = [];

    public List<string> ExpectedResultClaims { get; set; } = [];
}

public sealed class ExecutionOutcomeAction
{
    public string Id { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public bool Required { get; set; } = true;

    public bool RequiresExecutionComponent { get; set; } = true;

    public List<string> CapabilityIds { get; set; } = [];

    public List<string> InputFileIds { get; set; } = [];

    public List<string> ExpectedEvidenceTypes { get; set; } = [];
}

public static class ExecutionRouteLevels
{
    public const string Preferred = "preferred";
    public const string Degraded = "degraded";
    public const string Emergency = "emergency";
}

public sealed class ExecutionBundlePlan
{
    public List<string> PatternIds { get; set; } = [];

    public ArtifactContract ArtifactContract { get; set; } = new();

    public ExecutionRouteVariant PreferredRoute { get; set; } = new()
    {
        Id = ExecutionRouteLevels.Preferred,
        Level = ExecutionRouteLevels.Preferred
    };

    public ExecutionRouteVariant DegradedRoute { get; set; } = new()
    {
        Id = ExecutionRouteLevels.Degraded,
        Level = ExecutionRouteLevels.Degraded
    };

    public ExecutionRouteVariant EmergencyRoute { get; set; } = new()
    {
        Id = ExecutionRouteLevels.Emergency,
        Level = ExecutionRouteLevels.Emergency
    };

    public SandboxAcquisitionPlan AcquisitionPlan { get; set; } = new();

    public InstallationManifest InstallationManifest { get; set; } = new();

    public List<SandboxExecutionRecipe> Recipes { get; set; } = [];

    public string SelectedRouteLevel { get; set; } = ExecutionRouteLevels.Preferred;

    public bool CanStart => PreferredRoute.IsStartable
        || DegradedRoute.IsStartable
        || EmergencyRoute.IsStartable;
}

public sealed class SandboxExecutionRecipe
{
    public string Id { get; set; } = string.Empty;

    public List<string> PatternIds { get; set; } = [];

    public string ArtifactKind { get; set; } = ArtifactKinds.Text;

    public string Purpose { get; set; } = string.Empty;

    public List<string> PreferredSteps { get; set; } = [];

    public List<string> DegradedSteps { get; set; } = [];

    public List<string> EmergencySteps { get; set; } = [];

    public List<string> RequiredCapabilities { get; set; } = [];

    public List<string> ValidationRules { get; set; } = [];
}

public static class ArtifactQualityLevels
{
    public const string Preferred = "preferred";
    public const string Degraded = "degraded";
    public const string Emergency = "emergency";
}

public static class ArtifactValidationStatuses
{
    public const string Pending = "pending";
    public const string Valid = "valid";
    public const string Invalid = "invalid";
}

public sealed class ArtifactValidationResult
{
    public string Status { get; set; } = ArtifactValidationStatuses.Pending;

    public string FilePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string DetectedExtension { get; set; } = string.Empty;

    public string DetectedMimeType { get; set; } = string.Empty;

    public List<string> Checks { get; set; } = [];

    public List<string> Errors { get; set; } = [];

    public bool IsValid => string.Equals(
        Status,
        ArtifactValidationStatuses.Valid,
        StringComparison.Ordinal);
}

public static class ExecutionActionStatuses
{
    public const string Planned = "planned";
    public const string Ready = "ready";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Blocked = "blocked";
    public const string Skipped = "skipped";
}

public static class EvidenceValidationStatuses
{
    public const string Pending = "pending";
    public const string Valid = "valid";
    public const string Limited = "limited";
    public const string Invalid = "invalid";
}

public static class TaskFulfillmentStatuses
{
    public const string Pending = "pending";
    public const string Complete = "complete";
    public const string Limited = "limited";
    public const string Failed = "failed";
}

public static class ExecutionEvidenceTypes
{
    public const string ToolResult = "tool_result";
    public const string FileInspection = "file_inspection";
    public const string ProducedArtifact = "produced_artifact";
    public const string FailedExecution = "failed_execution";
}

public sealed class ExecutionActionGraph
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Goal { get; set; } = string.Empty;

    public string RouteLevel { get; set; } = ExecutionRouteLevels.Emergency;

    public string ArtifactKind { get; set; } = ArtifactKinds.Text;

    public bool RequiresExternalEvidence { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ExecutionActionNode> Nodes { get; set; } = [];
}

public sealed class ExecutionActionNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Layer { get; set; } = string.Empty;

    public string CapabilityId { get; set; } = string.Empty;

    public string OutcomeActionId { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public bool Required { get; set; }

    public string Status { get; set; } = ExecutionActionStatuses.Planned;

    public List<string> ToolNames { get; set; } = [];

    public List<string> ComponentIds { get; set; } = [];

    public List<string> InputFileIds { get; set; } = [];

    public Dictionary<string, string> InputSha256ByFileId { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public List<string> DependencyIds { get; set; } = [];

    public List<string> ReceiptIds { get; set; } = [];

    public List<string> ExpectedEvidenceTypes { get; set; } = [];
}

public sealed class ExecutionEvidenceReceipt
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string ActionId { get; set; } = string.Empty;

    public List<string> OutcomeActionIds { get; set; } = [];

    public string ToolCallId { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public List<string> ComponentIds { get; set; } = [];

    public string Command { get; set; } = string.Empty;

    public string ArgumentsHash { get; set; } = string.Empty;

    public string InputFileId { get; set; } = string.Empty;

    public string InputFileName { get; set; } = string.Empty;

    public string InputSha256 { get; set; } = string.Empty;

    public string OutputSha256 { get; set; } = string.Empty;

    public string OutputArtifactPath { get; set; } = string.Empty;

    public string ResultHash { get; set; } = string.Empty;

    public string ResultExcerpt { get; set; } = string.Empty;

    public string NormalizedResultText { get; set; } = string.Empty;

    public string EvidenceType { get; set; } = ExecutionEvidenceTypes.ToolResult;

    public List<string> ConfirmedClaimScopes { get; set; } = [];

    public string Limitations { get; set; } = string.Empty;

    public string DiagnosticMessage { get; set; } = string.Empty;

    public bool Success { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<string> Capabilities { get; set; } = [];
}

public sealed class EvidenceValidationResult
{
    public string Status { get; set; } = EvidenceValidationStatuses.Pending;

    public string GraphId { get; set; } = string.Empty;

    public List<string> SatisfiedActionIds { get; set; } = [];

    public List<string> MissingActionIds { get; set; } = [];

    public List<string> ReceiptIds { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public bool IsValid => string.Equals(Status, EvidenceValidationStatuses.Valid, StringComparison.Ordinal);
}

public sealed class TaskFulfillmentValidationResult
{
    public string Status { get; set; } = TaskFulfillmentStatuses.Pending;

    public string TechnicalStatus { get; set; } = ArtifactValidationStatuses.Pending;

    public string EvidenceStatus { get; set; } = EvidenceValidationStatuses.Pending;

    public string Goal { get; set; } = string.Empty;

    public List<string> Checks { get; set; } = [];

    public List<string> Warnings { get; set; } = [];

    public bool IsComplete => string.Equals(Status, TaskFulfillmentStatuses.Complete, StringComparison.Ordinal);
}

public sealed class SandboxArtifactMaterializationResult
{
    public string FilePath { get; set; } = string.Empty;

    public string ArtifactKind { get; set; } = ArtifactKinds.Text;

    public string MimeType { get; set; } = "application/octet-stream";

    public string QualityLevel { get; set; } = ArtifactQualityLevels.Emergency;

    public string RecipeId { get; set; } = string.Empty;

    public List<string> Warnings { get; set; } = [];

    public ArtifactValidationResult Validation { get; set; } = new();

    public string SourceReceiptId { get; set; } = string.Empty;

    public EvidenceValidationResult EvidenceValidation { get; set; } = new();

    public TaskFulfillmentValidationResult TaskValidation { get; set; } = new();
}

public sealed class ExecutionRouteVariant
{
    public string Id { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ExecutionRoutePlan Route { get; set; } = new();

    public List<string> MissingCapabilities { get; set; } = [];

    public string OutputGuarantee { get; set; } = string.Empty;

    public bool IsStartable { get; set; }
}

public static class InstallationManifestStatuses
{
    public const string Bundled = "bundled";
    public const string Available = "available";
    public const string DownloadRequired = "download_required";
    public const string Downloading = "downloading";
    public const string Downloaded = "downloaded";
    public const string Verified = "verified";
    public const string Installed = "installed";
    public const string Runnable = "runnable";
    public const string MissingAdapter = "missing_adapter";
    public const string Failed = "failed";
    public const string UpdateAvailable = "update_available";
    public const string Removed = "removed";
}

public sealed class InstallationManifest
{
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<InstallationManifestEntry> Entries { get; set; } = [];
}

public sealed class InstallationManifestEntry
{
    public string ComponentId { get; set; } = string.Empty;

    public string ComponentName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = InstallationManifestStatuses.Available;

    public string Version { get; set; } = string.Empty;

    public List<string> Capabilities { get; set; } = [];

    public bool AdapterAvailable { get; set; }
}

public sealed class SandboxAcquisitionPlan
{
    public List<SandboxAcquisitionItem> Items { get; set; } = [];

    public bool RequiresConfirmation => Items.Any(item =>
        item.Status == InstallationManifestStatuses.DownloadRequired);
}

public sealed class SandboxAcquisitionItem
{
    public string ComponentId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public long ExpectedSizeBytes { get; set; }

    public List<string> Capabilities { get; set; } = [];

    public string Fallback { get; set; } = string.Empty;

    public string Status { get; set; } = InstallationManifestStatuses.DownloadRequired;
}
