using AIHub.Models;

namespace AIHub.Services;

public sealed class CapabilityResolverService
{
    private readonly ComponentManager _componentManager;
    private readonly SystemCliDiscoveryService _cliDiscovery;

    public CapabilityResolverService(
        ComponentManager componentManager,
        SystemCliDiscoveryService? cliDiscovery = null)
    {
        _componentManager = componentManager;
        _cliDiscovery = cliDiscovery ?? new SystemCliDiscoveryService();
    }

    public CapabilityResolutionPlan Resolve(
        IEnumerable<ExecutorCapabilityRequest> requests,
        string reason,
        IReadOnlyCollection<string>? preferredComponentIds = null)
    {
        var normalized = Normalize(requests);
        var statuses = _componentManager.GetStatus(ComponentKinds.Processing);
        var preferred = (preferredComponentIds ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var bindings = normalized
            .Select(request => ResolveBinding(request, statuses, preferred))
            .ToList();
        var providerIds = bindings
            .Where(binding =>
                binding.AdapterAvailable
                && !string.IsNullOrWhiteSpace(binding.ComponentId))
            .Select(binding => binding.ComponentId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CapabilityResolutionPlan
        {
            Requests = normalized,
            Bindings = bindings,
            Acquisition = _componentManager.BuildPlanForComponents(
                providerIds,
                reason,
                normalized.Any(request => request.Required))
        };
    }

    private CapabilityAdapterBinding ResolveBinding(
        ExecutorCapabilityRequest request,
        IReadOnlyList<ComponentStatusSnapshot> statuses,
        IReadOnlySet<string> preferredComponentIds)
    {
        var candidates = ComponentCapabilityAliasCatalog.Expand(
            new[] { request.Id }.Concat(request.Alternatives));
        var matches = candidates
            .SelectMany((capability, capabilityIndex) => statuses
                .Where(status => status.Entry.Capabilities.Contains(
                    capability,
                    StringComparer.OrdinalIgnoreCase))
                .Select(status => new
                {
                    Capability = capability,
                    CapabilityIndex = capabilityIndex,
                    Provider = status,
                    Adapter = ComponentAdapterRegistry.Find(capability)
                }))
            .OrderBy(match => preferredComponentIds.Contains(match.Provider.Entry.Id) ? 0 : 1)
            .ThenBy(match => GetReadinessRank(
                match.Provider,
                match.Adapter))
            .ThenBy(match => match.CapabilityIndex)
            .ThenBy(match => match.Provider.Entry.IsPlanned)
            .ThenBy(match => match.Provider.Entry.DownloadSizeBytes)
            .ToList();
        if (matches.Count == 0)
        {
            var discoveredCli = candidates
                .Select(capability => new
                {
                    Capability = capability,
                    Discovery = _cliDiscovery.Find(capability),
                    Adapter = ComponentAdapterRegistry.Find(capability)
                })
                .FirstOrDefault(match => match.Discovery is not null);
            if (discoveredCli?.Discovery is not null)
            {
                return new CapabilityAdapterBinding
                {
                    RequestedCapabilityId = request.Id,
                    CapabilityId = discoveredCli.Capability,
                    AdapterId = discoveredCli.Adapter?.Id ?? string.Empty,
                    ToolNames = discoveredCli.Adapter?.ToolNames.ToList() ?? [],
                    Required = request.Required,
                    Purpose = request.Purpose,
                    Status = discoveredCli.Adapter is null
                        ? CapabilityBindingStatuses.AdapterMissing
                        : CapabilityBindingStatuses.ExternalCliFound,
                    Details = discoveredCli.Adapter is null
                        ? $"A compatible command-line runtime '{discoveredCli.Discovery.CommandName}' exists, but LOPATA has no trusted adapter for it."
                        : $"A compatible command-line runtime '{discoveredCli.Discovery.CommandName}' was found and requires adapter health-check.",
                    AdapterAvailable = discoveredCli.Adapter is not null
                };
            }

            return new CapabilityAdapterBinding
            {
                RequestedCapabilityId = request.Id,
                CapabilityId = request.Id,
                Required = request.Required,
                Purpose = request.Purpose,
                Status = CapabilityBindingStatuses.UnknownCapability,
                Details = "No provider exists in the trusted LOPATA catalog. External discovery may be requested."
            };
        }

        var selected = matches[0];
        var provider = selected.Provider;
        var adapter = selected.Adapter;
        if (provider.IsAvailable && adapter is not null)
        {
            return CreateBinding(
                request,
                selected.Capability,
                provider,
                adapter,
                CapabilityBindingStatuses.Ready,
                "Package, dependencies, adapter and tool schema are ready.");
        }

        var external = _cliDiscovery.Find(selected.Capability);
        if (!provider.IsAvailable && external is not null && adapter is not null)
        {
            return CreateBinding(
                request,
                selected.Capability,
                provider,
                adapter,
                CapabilityBindingStatuses.ExternalCliFound,
                $"A compatible command-line runtime was found and still requires adapter health-check: {external.CommandName}.");
        }

        if (adapter is null)
        {
            return CreateBinding(
                request,
                selected.Capability,
                provider,
                adapter,
                CapabilityBindingStatuses.AdapterMissing,
                provider.IsAvailable
                    ? "The package is installed, but LOPATA has no trusted callable adapter for this capability yet."
                    : "A provider exists in the catalog, but LOPATA has no trusted callable adapter; downloading the package alone would not make the route executable.");
        }

        if (!provider.IsAvailable)
        {
            return CreateBinding(
                request,
                selected.Capability,
                provider,
                adapter,
                CapabilityBindingStatuses.PackageMissing,
                "The selected provider or one of its dependencies is not installed and verified.");
        }

        return CreateBinding(
            request,
            selected.Capability,
            provider,
            adapter,
            CapabilityBindingStatuses.Ready,
            "Package, dependencies, adapter and tool schema are ready.");
    }

    private static int GetReadinessRank(
        ComponentStatusSnapshot provider,
        ComponentAdapterDescriptor? adapter) =>
        (provider.IsAvailable, adapter is not null) switch
        {
            (true, true) => 0,
            (false, true) => 1,
            (true, false) => 2,
            _ => 3
        };

    private static CapabilityAdapterBinding CreateBinding(
        ExecutorCapabilityRequest request,
        string resolvedCapabilityId,
        ComponentStatusSnapshot provider,
        ComponentAdapterDescriptor? adapter,
        string status,
        string details) =>
        new()
        {
            RequestedCapabilityId = request.Id,
            CapabilityId = resolvedCapabilityId,
            ComponentId = provider.Entry.Id,
            ComponentName = provider.Entry.Name,
            AdapterId = adapter?.Id ?? string.Empty,
            ToolNames = adapter?.ToolNames.ToList() ?? [],
            Status = status,
            Required = request.Required,
            Purpose = request.Purpose,
            Details = details,
            PackageAvailable = provider.IsAvailable,
            AdapterAvailable = adapter is not null
        };

    private static List<ExecutorCapabilityRequest> Normalize(
        IEnumerable<ExecutorCapabilityRequest> requests) =>
        requests
            .Where(request => !string.IsNullOrWhiteSpace(request.Id))
            .Select(request => new ExecutorCapabilityRequest
            {
                Id = ComponentCapabilityAliasCatalog.Canonicalize(request.Id),
                Purpose = request.Purpose.Trim(),
                Required = request.Required,
                Alternatives = request.Alternatives
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(ComponentCapabilityAliasCatalog.Canonicalize)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(6)
                    .ToList()
            })
            .GroupBy(request => request.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(24)
            .ToList();
}
