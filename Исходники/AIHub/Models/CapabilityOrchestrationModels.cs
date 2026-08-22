namespace AIHub.Models;

public static class CapabilityBindingStatuses
{
    public const string Ready = "ready";
    public const string PackageMissing = "package_missing";
    public const string AdapterMissing = "adapter_missing";
    public const string UnknownCapability = "unknown_capability";
    public const string ExternalCliFound = "external_cli_found";
}

public sealed class ExecutorCapabilityRequest
{
    public string Id { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public bool Required { get; set; } = true;

    public List<string> Alternatives { get; set; } = [];
}

public sealed class CapabilityAdapterBinding
{
    public string RequestedCapabilityId { get; set; } = string.Empty;

    public string CapabilityId { get; set; } = string.Empty;

    public string ComponentId { get; set; } = string.Empty;

    public string ComponentName { get; set; } = string.Empty;

    public string AdapterId { get; set; } = string.Empty;

    public List<string> ToolNames { get; set; } = [];

    public string Status { get; set; } = CapabilityBindingStatuses.UnknownCapability;

    public bool Required { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public bool PackageAvailable { get; set; }

    public bool AdapterAvailable { get; set; }

    public bool IsExecutable => PackageAvailable && AdapterAvailable
        && Status is CapabilityBindingStatuses.Ready
            or CapabilityBindingStatuses.ExternalCliFound;
}

public sealed class CapabilityResolutionPlan
{
    public List<ExecutorCapabilityRequest> Requests { get; set; } = [];

    public List<CapabilityAdapterBinding> Bindings { get; set; } = [];

    public ComponentAcquisitionPlan Acquisition { get; set; } = new();

    public bool RequiresExternalDiscovery =>
        Bindings.Any(binding =>
            binding.Status is CapabilityBindingStatuses.AdapterMissing
                or CapabilityBindingStatuses.UnknownCapability);

    public bool IsExecutable => Bindings.All(binding =>
        !binding.Required || binding.IsExecutable);
}

public static class ExecutionRouteLayers
{
    public const string FileAccess = "file_access";
    public const string Decode = "decode";
    public const string SemanticAnalysis = "semantic_analysis";
    public const string Action = "action";
}

public sealed class ExecutionRouteRequirement
{
    public string Layer { get; set; } = string.Empty;

    public ExecutorCapabilityRequest Request { get; set; } = new();
}

public sealed class ExecutionRoutePlan
{
    public List<string> SourceFormats { get; set; } = [];

    public List<ExecutionRouteRequirement> Requirements { get; set; } = [];

    public CapabilityResolutionPlan Resolution { get; set; } = new();

    public List<string> Warnings { get; set; } = [];

    public int RequiredOutcomeActionCount { get; set; }

    public int CoveredOutcomeActionCount { get; set; }

    public int OutcomeCoveragePercent { get; set; } = 100;

    public List<string> MissingOutcomeActionIds { get; set; } = [];

    public bool HasCompleteOutcomeCoverage => RequiredOutcomeActionCount == 0
        || (CoveredOutcomeActionCount >= RequiredOutcomeActionCount
            && MissingOutcomeActionIds.Count == 0);

    public bool IsExecutable => Resolution.IsExecutable
        && HasCompleteOutcomeCoverage;

    public bool RequiresAcquisition => Resolution.Bindings.Any(binding =>
        binding.Required
        && binding.Status == CapabilityBindingStatuses.PackageMissing
        && binding.AdapterAvailable);

    public bool HasBlockedRequirements => Resolution.Bindings.Any(binding =>
        binding.Required
        && binding.Status is CapabilityBindingStatuses.AdapterMissing
            or CapabilityBindingStatuses.UnknownCapability);
}
