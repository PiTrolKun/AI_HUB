using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutionBundlePlannerService
{
    private readonly ComponentManager _componentManager;
    private readonly WorkPatternCatalogService _patternCatalogService;

    public ExecutionBundlePlannerService(
        ComponentManager? componentManager = null,
        WorkPatternCatalogService? patternCatalogService = null)
    {
        _componentManager = componentManager ?? new ComponentManager();
        _patternCatalogService = patternCatalogService ?? new WorkPatternCatalogService();
    }

    public ExecutionBundlePlan Build(
        WorkPatternSelectionResult selection,
        ArtifactContract artifactContract,
        ExecutionRoutePlan preferredRoute)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(artifactContract);
        ArgumentNullException.ThrowIfNull(preferredRoute);

        var unresolved = preferredRoute.Resolution.Bindings
            .Where(binding => binding.Required && !binding.IsExecutable)
            .Select(binding => binding.RequestedCapabilityId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var degraded = CloneRoute(preferredRoute);
        foreach (var requirement in degraded.Requirements)
        {
            var binding = degraded.Resolution.Bindings.FirstOrDefault(item =>
                string.Equals(
                    item.RequestedCapabilityId,
                    requirement.Request.Id,
                    StringComparison.OrdinalIgnoreCase));
            if (binding is not { IsExecutable: false })
            {
                continue;
            }

            requirement.Request.Required = false;
            binding.Required = false;
        }

        var emergency = CloneRoute(degraded);
        foreach (var requirement in emergency.Requirements.Where(requirement =>
                     requirement.Layer is ExecutionRouteLayers.SemanticAnalysis
                         or ExecutionRouteLayers.Action))
        {
            requirement.Request.Required = false;
            var binding = emergency.Resolution.Bindings.FirstOrDefault(item =>
                string.Equals(
                    item.RequestedCapabilityId,
                    requirement.Request.Id,
                    StringComparison.OrdinalIgnoreCase));
            if (binding is not null)
            {
                binding.Required = false;
            }
        }

        var plan = new ExecutionBundlePlan
        {
            PatternIds = selection.Selections
                .Select(item => item.PatternId)
                .ToList(),
            ArtifactContract = artifactContract,
            PreferredRoute = new ExecutionRouteVariant
            {
                Id = ExecutionRouteLevels.Preferred,
                Level = ExecutionRouteLevels.Preferred,
                Description = "Use every requested specialist capability and trusted adapter.",
                Route = preferredRoute,
                MissingCapabilities = unresolved,
                OutputGuarantee = "Best available quality for the complete requested route.",
                IsStartable = preferredRoute.IsExecutable
            },
            DegradedRoute = new ExecutionRouteVariant
            {
                Id = ExecutionRouteLevels.Degraded,
                Level = ExecutionRouteLevels.Degraded,
                Description = "Start the coordinator with ready capabilities and acquire or replace missing modules during the session.",
                Route = degraded,
                MissingCapabilities = unresolved,
                OutputGuarantee = artifactContract.EmergencyAcceptableResult,
                IsStartable = true
            },
            EmergencyRoute = new ExecutionRouteVariant
            {
                Id = ExecutionRouteLevels.Emergency,
                Level = ExecutionRouteLevels.Emergency,
                Description = "Produce the requested artifact type with the safest deterministic fallback available.",
                Route = emergency,
                MissingCapabilities = unresolved,
                OutputGuarantee = artifactContract.EmergencyAcceptableResult,
                IsStartable = true
            },
            SelectedRouteLevel = ExecutionRouteLevels.Preferred
        };
        var selectedPatterns = _patternCatalogService.ResolveSelected(selection);
        plan.Recipes = new SandboxExecutionRecipeService().Build(
            selectedPatterns,
            artifactContract);
        PopulateComponentState(plan, preferredRoute);
        return plan;
    }

    private void PopulateComponentState(
        ExecutionBundlePlan plan,
        ExecutionRoutePlan route)
    {
        var statuses = _componentManager.GetStatus(ComponentKinds.Processing)
            .ToDictionary(
                status => status.Entry.Id,
                StringComparer.OrdinalIgnoreCase);
        var componentIds = route.Resolution.Bindings
            .Where(binding => !string.IsNullOrWhiteSpace(binding.ComponentId))
            .Select(binding => binding.ComponentId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var componentId in componentIds)
        {
            var entry = ComponentCatalog.All.FirstOrDefault(item =>
                string.Equals(item.Id, componentId, StringComparison.OrdinalIgnoreCase));
            if (entry is null)
            {
                continue;
            }

            statuses.TryGetValue(componentId, out var status);
            var adapters = entry.Capabilities
                .Select(ComponentAdapterRegistry.Find)
                .Where(adapter => adapter is not null)
                .ToList();
            var adapterAvailable = adapters.Count > 0;
            var manifestStatus = ResolveManifestStatus(entry, status, adapterAvailable);
            plan.InstallationManifest.Entries.Add(new InstallationManifestEntry
            {
                ComponentId = entry.Id,
                ComponentName = entry.Name,
                Role = InferRole(entry),
                Status = manifestStatus,
                Version = entry.Version,
                Capabilities = entry.Capabilities.ToList(),
                AdapterAvailable = adapterAvailable
            });
            if (manifestStatus != InstallationManifestStatuses.DownloadRequired)
            {
                continue;
            }

            plan.AcquisitionPlan.Items.Add(new SandboxAcquisitionItem
            {
                ComponentId = entry.Id,
                Role = InferRole(entry),
                Reason = route.Resolution.Bindings.FirstOrDefault(binding =>
                    string.Equals(
                        binding.ComponentId,
                        entry.Id,
                        StringComparison.OrdinalIgnoreCase))?.Purpose ?? string.Empty,
                Source = entry.Source,
                Version = entry.Version,
                License = entry.License,
                ExpectedSizeBytes = entry.DownloadSizeBytes,
                Capabilities = entry.Capabilities.ToList(),
                Fallback = plan.ArtifactContract.EmergencyAcceptableResult,
                Status = InstallationManifestStatuses.DownloadRequired
            });
        }
    }

    private static string ResolveManifestStatus(
        ComponentCatalogEntry entry,
        ComponentStatusSnapshot? status,
        bool adapterAvailable)
    {
        if (!adapterAvailable)
        {
            return InstallationManifestStatuses.MissingAdapter;
        }

        if (entry.IsBuiltIn)
        {
            return InstallationManifestStatuses.Bundled;
        }

        if (status is { IsAvailable: true })
        {
            return InstallationManifestStatuses.Runnable;
        }

        return entry.IsPlanned
            ? InstallationManifestStatuses.Available
            : InstallationManifestStatuses.DownloadRequired;
    }

    private static string InferRole(ComponentCatalogEntry entry)
    {
        var values = entry.Capabilities.ToList();
        if (entry.Id.StartsWith("model.", StringComparison.OrdinalIgnoreCase))
        {
            return "specialist_model";
        }

        if (values.Any(value => value.StartsWith("analyze.", StringComparison.OrdinalIgnoreCase)))
        {
            return "specialist_model";
        }

        if (values.Any(value => value.StartsWith("read.", StringComparison.OrdinalIgnoreCase)))
        {
            return "decoder";
        }

        if (values.Any(value =>
                value.StartsWith("edit.", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("generate.", StringComparison.OrdinalIgnoreCase)))
        {
            return "processor";
        }

        return "runtime";
    }

    private static ExecutionRoutePlan CloneRoute(ExecutionRoutePlan route)
    {
        var json = JsonSerializer.Serialize(route);
        return JsonSerializer.Deserialize<ExecutionRoutePlan>(json)
            ?? new ExecutionRoutePlan();
    }
}
