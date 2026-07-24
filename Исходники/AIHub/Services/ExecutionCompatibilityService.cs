using AIHub.Models;

namespace AIHub.Services;

public static class ExecutionCompatibilityService
{
    public const string CoordinatorRole = "coordinator";
    public const string LlamaRuntime = "llama.cpp";
    public const string GgufArtifact = "gguf";
    public const string TaskProfileMatch = "task_profile";
    public const string CoordinatorFallbackMatch = "coordinator_fallback";

    private static readonly HashSet<string> LlamaCoordinatorPipelines =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "text-generation",
            "conversational"
        };

    public static ExecutionCapabilityResolution ResolveCapabilities(
        ChoiceCapabilityProfile profile,
        CapabilityInventoryResponse inventory)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(inventory);

        var required = ComponentCapabilityMapper.FromProfile(profile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var installed = inventory.Items
            .Where(item => string.Equals(
                    item.Role,
                    "component_capability",
                    StringComparison.OrdinalIgnoreCase)
                && item.IsInstalled
                && item.IsRunnable)
            .Select(item => item.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var available = new List<string>();
        var missing = new List<string>();
        var unresolved = new List<string>();

        foreach (var capability in required)
        {
            if (installed.Contains(capability))
            {
                available.Add(capability);
                continue;
            }

            var providers = ComponentCatalog.FindProviders(capability);
            if (providers.Count == 0
                || providers.All(provider => provider.IsPlanned)
                || !ComponentAdapterRegistry.IsCallable(capability))
            {
                unresolved.Add(capability);
                continue;
            }

            missing.Add(capability);
        }

        return new ExecutionCapabilityResolution
        {
            Required = required,
            Available = available,
            Missing = missing,
            Unresolved = unresolved
        };
    }

    public static bool IsLlamaCoordinatorCandidate(ModelCatalogCandidate candidate) =>
        candidate is not null
        && LlamaCoordinatorPipelines.Contains(candidate.PipelineTag);

    public static bool IsLlamaCoordinatorPipeline(string pipelineTag) =>
        LlamaCoordinatorPipelines.Contains(pipelineTag ?? string.Empty);

    public static void ApplyExecutionPassport(
        ChoiceExecutorPoolCandidate candidate,
        ExecutionCapabilityResolution resolution,
        string catalogMatchScope)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(resolution);

        candidate.Role = CoordinatorRole;
        candidate.RuntimeBackend = LlamaRuntime;
        candidate.ArtifactFormat = GgufArtifact;
        candidate.CatalogMatchScope = catalogMatchScope;
        candidate.RuntimeCompatible = IsLlamaCoordinatorPipeline(candidate.PipelineTag);
        candidate.RequiredCapabilities = resolution.Required.ToList();
        candidate.AvailableCapabilities = resolution.Available.ToList();
        candidate.MissingCapabilities = resolution.Missing.ToList();
        candidate.UnresolvedCapabilities = resolution.Unresolved.ToList();
    }
}

public sealed class ExecutionCapabilityResolution
{
    public List<string> Required { get; set; } = [];

    public List<string> Available { get; set; } = [];

    public List<string> Missing { get; set; } = [];

    public List<string> Unresolved { get; set; } = [];
}
