namespace AIHub.Models;

public sealed class ChoiceTaskCard
{
    public string Goal { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public List<string> Criteria { get; set; } = [];

    public List<string> Constraints { get; set; } = [];

    public bool NeedsWeb { get; set; }

    public List<string> RequiredTools { get; set; } = [];

    public ChoiceCapabilityProfile CapabilityProfile { get; set; } = new();

    public ChoiceExecutorSelection ExecutorSelection { get; set; } = new();

    public ExecutionRoutePlan ExecutionRoute { get; set; } = new();

    public string ExecutorRole { get; set; } = string.Empty;

    public string ExecutorCapabilityClass { get; set; } = string.Empty;

    public string RecommendedExecutor { get; set; } = string.Empty;

    public string ExecutorStatus { get; set; } = string.Empty;

    public string ExecutorReason { get; set; } = string.Empty;

    public List<ChoiceExecutorCandidate> ExecutorCandidates { get; set; } = [];

    public string PromptForExecutor { get; set; } = string.Empty;
}

public sealed class ChoiceExecutorCandidate
{
    public string Model { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Family { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string CapabilityClass { get; set; } = string.Empty;

    public string Advantage { get; set; } = string.Empty;

    public string Limitation { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string SemanticDescriptionRu { get; set; } = string.Empty;

    public string SemanticDescriptionEn { get; set; } = string.Empty;

    public bool IsRecommended { get; set; }

    public string RuntimeBackend { get; set; } = string.Empty;

    public string ArtifactFormat { get; set; } = string.Empty;

    public string CatalogMatchScope { get; set; } = string.Empty;

    public List<string> RequiredCapabilities { get; set; } = [];

    public List<string> AvailableCapabilities { get; set; } = [];

    public List<string> MissingCapabilities { get; set; } = [];

    public List<string> UnresolvedCapabilities { get; set; } = [];
}

public sealed class ChoiceExecutorSelection
{
    public string InstalledCandidateId { get; set; } = string.Empty;

    public string AlternativeCandidateId { get; set; } = string.Empty;

    public string PreferredCandidateId { get; set; } = string.Empty;

    public ChoiceExecutorAssessment InstalledAssessment { get; set; } = new();

    public ChoiceExecutorAssessment AlternativeAssessment { get; set; } = new();
}

public sealed class ChoiceExecutorAssessment
{
    public string Advantage { get; set; } = string.Empty;

    public string Limitation { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}

public static class ChoiceExecutorCandidateStatuses
{
    public const string Installed = "installed";

    public const string NotInstalled = "not_installed";
}

public sealed record ChoiceExecutorCandidateDisplay(
    ChoiceExecutorCandidate Candidate,
    string Model,
    string Status,
    string Description,
    string Advantage,
    string Limitation,
    string Recommendation,
    bool IsRecommended);
