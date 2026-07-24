namespace AIHub.Models;

public sealed class ChoiceExecutorCandidatePool
{
    public ExecutionRoutePlan ExecutionRoute { get; set; } = new();

    public List<string> RequiredProtocols { get; set; } = [];

    public List<string> RequiredCapabilities { get; set; } = [];

    public List<string> AvailableCapabilities { get; set; } = [];

    public List<string> MissingCapabilities { get; set; } = [];

    public List<string> UnresolvedCapabilities { get; set; } = [];

    public List<ChoiceExecutorPoolCandidate> InstalledCandidates { get; set; } = [];

    public List<ChoiceExecutorPoolCandidate> AlternativeCandidates { get; set; } = [];

    public bool UsedLiveSearch { get; set; }

    public List<string> Warnings { get; set; } = [];

    public bool HasCandidatePair => InstalledCandidates.Count > 0
        && AlternativeCandidates.Count > 0;

    public bool IsExecutionReady => HasCandidatePair
        && ExecutionRoute.IsExecutable;

    public bool HasValidPair => IsExecutionReady;
}

public sealed class ChoiceExecutorPoolCandidate
{
    public string Id { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Family { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Role { get; set; } = "general_worker";

    public string CapabilityClass { get; set; } = "above_8b";

    public long? ParameterCount { get; set; }

    public string PipelineTag { get; set; } = string.Empty;

    public string ModelType { get; set; } = string.Empty;

    public List<string> Directions { get; set; } = [];

    public List<string> Roles { get; set; } = [];

    public string HardwareStatus { get; set; } = string.Empty;

    public string Evidence { get; set; } = string.Empty;

    public string SemanticDescriptionRu { get; set; } = string.Empty;

    public string SemanticDescriptionEn { get; set; } = string.Empty;

    public string RuntimeBackend { get; set; } = string.Empty;

    public string ArtifactFormat { get; set; } = string.Empty;

    public string CatalogMatchScope { get; set; } = string.Empty;

    public bool RuntimeCompatible { get; set; }

    public List<string> RequiredCapabilities { get; set; } = [];

    public List<string> AvailableCapabilities { get; set; } = [];

    public List<string> MissingCapabilities { get; set; } = [];

    public List<string> UnresolvedCapabilities { get; set; } = [];

    public bool IsExecutionCompatible => RuntimeCompatible
        && UnresolvedCapabilities.Count == 0;
}
