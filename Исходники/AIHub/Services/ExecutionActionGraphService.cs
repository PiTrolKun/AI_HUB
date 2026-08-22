using System.IO;
using System.Security.Cryptography;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutionActionGraphService
{
    public ExecutionActionGraph Build(ExecutorHandoffPackage handoff)
    {
        ArgumentNullException.ThrowIfNull(handoff);

        var route = GetSelectedRoute(handoff);
        var graph = new ExecutionActionGraph
        {
            Goal = handoff.SuggestedDirection,
            RouteLevel = handoff.ExecutionBundle.SelectedRouteLevel,
            ArtifactKind = handoff.ArtifactContract.ArtifactKind,
            RequiresExternalEvidence = RequiresExternalEvidence(handoff)
        };

        var previousLayerNodeIds = new List<string>();
        foreach (var layerGroup in route.Requirements.GroupBy(item => item.Layer))
        {
            var currentLayerNodeIds = new List<string>();
            foreach (var requirement in layerGroup)
            {
                var outcomeAction = FindOutcomeAction(
                    handoff.OutcomeContract,
                    requirement.Request.Id);
                var bindings = route.Resolution.Bindings
                    .Where(binding => Matches(binding, requirement.Request.Id))
                    .ToList();
                var tools = bindings
                    .Where(binding => binding.IsExecutable)
                    .SelectMany(binding => binding.ToolNames)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var node = new ExecutionActionNode
                {
                    Layer = requirement.Layer,
                    CapabilityId = requirement.Request.Id,
                    OutcomeActionId = outcomeAction?.Id ?? string.Empty,
                    Purpose = requirement.Request.Purpose,
                    Required = requirement.Request.Required,
                    Status = tools.Count > 0
                        ? ExecutionActionStatuses.Ready
                        : requirement.Request.Required
                            ? ExecutionActionStatuses.Blocked
                            : ExecutionActionStatuses.Planned,
                    ToolNames = tools,
                    ComponentIds = bindings
                        .Select(binding => binding.ComponentId)
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                    InputFileIds = outcomeAction?.InputFileIds
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                        ?? [],
                    DependencyIds = [.. previousLayerNodeIds],
                    ExpectedEvidenceTypes = outcomeAction?.ExpectedEvidenceTypes
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                        ?? []
                };
                graph.Nodes.Add(node);
                currentLayerNodeIds.Add(node.Id);
            }

            previousLayerNodeIds = currentLayerNodeIds;
        }

        AddMissingOutcomeActions(graph, handoff.OutcomeContract, previousLayerNodeIds);

        var operationalNodeIds = graph.Nodes
            .Where(node => node.Required)
            .Select(node => node.Id)
            .ToList();
        var artifactNode = new ExecutionActionNode
        {
            Layer = "artifact",
            CapabilityId = $"artifact.{handoff.ArtifactContract.ArtifactKind}",
            OutcomeActionId = FindOutcomeActionByKind(
                handoff.OutcomeContract,
                ExecutionOutcomeActionKinds.ArtifactProduction)?.Id
                ?? "outcome.artifact",
            Purpose = "Create the requested physical result artifact from verified evidence.",
            Required = true,
            Status = ExecutionActionStatuses.Planned,
            DependencyIds = operationalNodeIds,
            ExpectedEvidenceTypes = [ExecutionEvidenceTypes.ProducedArtifact]
        };
        graph.Nodes.Add(artifactNode);
        graph.Nodes.Add(new ExecutionActionNode
        {
            Layer = "validation",
            CapabilityId = "artifact.validate",
            OutcomeActionId = FindOutcomeActionByKind(
                handoff.OutcomeContract,
                ExecutionOutcomeActionKinds.Validation)?.Id
                ?? "outcome.validation",
            Purpose = "Validate technical integrity and evidence-backed task fulfillment.",
            Required = true,
            Status = ExecutionActionStatuses.Planned,
            DependencyIds = [artifactNode.Id],
            ExpectedEvidenceTypes = [ExecutionEvidenceTypes.ToolResult]
        });

        return graph;
    }

    public ExecutionActionGraph BindInputFiles(
        ExecutionActionGraph graph,
        SessionFileManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(manifest);

        foreach (var node in graph.Nodes.Where(IsOperational))
        {
            if (node.InputFileIds.Count == 0)
            {
                node.InputFileIds = ResolveRelevantInputFiles(node.CapabilityId, manifest)
                    .Select(file => file.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            node.InputSha256ByFileId = manifest.Files
                .Where(file => node.InputFileIds.Contains(file.Id, StringComparer.OrdinalIgnoreCase))
                .Where(file => file.IsAvailable && File.Exists(file.SourcePath))
                .ToDictionary(
                    file => file.Id,
                    file => HashFile(file.SourcePath),
                    StringComparer.OrdinalIgnoreCase);
        }

        return graph;
    }

    public ExecutionActionGraph Reconcile(
        ExecutionActionGraph graph,
        IReadOnlyCollection<ExecutionEvidenceReceipt> receipts)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(receipts);

        foreach (var node in graph.Nodes.Where(node => IsOperational(node)))
        {
            var matching = receipts
                .Where(receipt => ReceiptMatches(node, receipt))
                .ToList();
            node.ReceiptIds = matching.Select(receipt => receipt.Id).Distinct().ToList();
            if (matching.Any(receipt => receipt.Success))
            {
                node.Status = ExecutionActionStatuses.Succeeded;
            }
            else if (matching.Count > 0)
            {
                node.Status = ExecutionActionStatuses.Failed;
            }
            else if (node.ToolNames.Count > 0)
            {
                node.Status = ExecutionActionStatuses.Ready;
            }
            else
            {
                node.Status = node.Required
                    ? ExecutionActionStatuses.Blocked
                    : ExecutionActionStatuses.Planned;
            }
        }

        return graph;
    }

    public ExecutionActionGraph MergeCapabilities(
        ExecutionActionGraph graph,
        IReadOnlyCollection<ExecutorCapabilityRequest> capabilities,
        IReadOnlyCollection<CapabilityAdapterBinding> bindings,
        IReadOnlyCollection<ExecutionEvidenceReceipt> receipts)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(receipts);

        foreach (var capability in capabilities)
        {
            var node = graph.Nodes.FirstOrDefault(item =>
                IsOperational(item)
                && CapabilitiesMatch(item.CapabilityId, capability.Id));
            if (node is null)
            {
                node = new ExecutionActionNode
                {
                    Layer = "dynamic_capability",
                    CapabilityId = capability.Id,
                    Purpose = capability.Purpose,
                    Required = capability.Required
                };
                graph.Nodes.Insert(
                    Math.Max(0, graph.Nodes.FindIndex(item =>
                        string.Equals(item.Layer, "artifact", StringComparison.OrdinalIgnoreCase))),
                    node);
            }

            node.Required |= capability.Required;
            if (!string.IsNullOrWhiteSpace(capability.Purpose))
            {
                node.Purpose = capability.Purpose;
            }

            node.ToolNames = bindings
                .Where(binding => Matches(binding, capability.Id))
                .Where(binding => binding.IsExecutable)
                .SelectMany(binding => binding.ToolNames)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            node.ComponentIds = bindings
                .Where(binding => Matches(binding, capability.Id))
                .Select(binding => binding.ComponentId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (capabilities.Any(capability => capability.Required))
        {
            graph.RequiresExternalEvidence = true;
        }

        var artifactNode = graph.Nodes.FirstOrDefault(item =>
            string.Equals(item.Layer, "artifact", StringComparison.OrdinalIgnoreCase));
        if (artifactNode is not null)
        {
            artifactNode.DependencyIds = graph.Nodes
                .Where(IsOperational)
                .Where(item => item.Required)
                .Select(item => item.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return Reconcile(graph, receipts);
    }

    public static ExecutionRoutePlan GetSelectedRoute(ExecutorHandoffPackage handoff)
    {
        return handoff.ExecutionBundle.SelectedRouteLevel switch
        {
            ExecutionRouteLevels.Degraded => handoff.ExecutionBundle.DegradedRoute.Route,
            ExecutionRouteLevels.Emergency => handoff.ExecutionBundle.EmergencyRoute.Route,
            _ => handoff.ExecutionBundle.PreferredRoute.Route
        };
    }

    public static bool IsOperational(ExecutionActionNode node) =>
        !string.Equals(node.Layer, "artifact", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(node.Layer, "validation", StringComparison.OrdinalIgnoreCase);

    private static bool Matches(CapabilityAdapterBinding binding, string capabilityId) =>
        CapabilitiesMatch(binding.RequestedCapabilityId, capabilityId)
        || CapabilitiesMatch(binding.CapabilityId, capabilityId);

    internal static bool ReceiptMatches(
        ExecutionActionNode node,
        ExecutionEvidenceReceipt receipt)
    {
        var identityMatches = string.Equals(
                receipt.ActionId,
                node.Id,
                StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(node.OutcomeActionId)
                && receipt.OutcomeActionIds.Contains(
                    node.OutcomeActionId,
                    StringComparer.OrdinalIgnoreCase))
            || node.ToolNames.Contains(receipt.ToolName, StringComparer.OrdinalIgnoreCase)
            || receipt.Capabilities.Any(capability =>
                CapabilitiesMatch(capability, node.CapabilityId));
        if (!identityMatches)
        {
            return false;
        }

        if (node.InputFileIds.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(receipt.InputFileId)
                || !node.InputFileIds.Contains(
                    receipt.InputFileId,
                    StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (node.InputSha256ByFileId.TryGetValue(receipt.InputFileId, out var expectedSha256)
                && !string.IsNullOrWhiteSpace(expectedSha256)
                && !string.Equals(
                    expectedSha256,
                    receipt.InputSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return node.ExpectedEvidenceTypes.Count == 0
            || node.ExpectedEvidenceTypes.Contains(
                receipt.EvidenceType,
                StringComparer.OrdinalIgnoreCase);
    }

    internal static bool CapabilitiesMatch(string? left, string? right)
    {
        var canonicalLeft = ComponentCapabilityAliasCatalog.Canonicalize(left ?? string.Empty);
        var canonicalRight = ComponentCapabilityAliasCatalog.Canonicalize(right ?? string.Empty);
        return !string.IsNullOrWhiteSpace(canonicalLeft)
            && string.Equals(canonicalLeft, canonicalRight, StringComparison.OrdinalIgnoreCase);
    }

    private static ExecutionOutcomeAction? FindOutcomeAction(
        ExecutionOutcomeContract contract,
        string capabilityId) =>
        contract.Actions.FirstOrDefault(action =>
            action.CapabilityIds.Any(capability =>
                CapabilitiesMatch(capability, capabilityId)));

    private static ExecutionOutcomeAction? FindOutcomeActionByKind(
        ExecutionOutcomeContract contract,
        string kind) =>
        contract.Actions.FirstOrDefault(action =>
            string.Equals(action.Kind, kind, StringComparison.OrdinalIgnoreCase));

    private static void AddMissingOutcomeActions(
        ExecutionActionGraph graph,
        ExecutionOutcomeContract contract,
        IReadOnlyCollection<string> dependencyIds)
    {
        foreach (var action in contract.Actions.Where(action =>
                     action.Required
                     && action.RequiresExecutionComponent
                     && !string.Equals(
                         action.Kind,
                         ExecutionOutcomeActionKinds.ArtifactProduction,
                         StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(
                         action.Kind,
                         ExecutionOutcomeActionKinds.Validation,
                         StringComparison.OrdinalIgnoreCase)))
        {
            var represented = graph.Nodes.Any(node =>
                string.Equals(
                    node.OutcomeActionId,
                    action.Id,
                    StringComparison.OrdinalIgnoreCase)
                || action.CapabilityIds.Any(capability =>
                    CapabilitiesMatch(capability, node.CapabilityId)));
            if (represented)
            {
                continue;
            }

            graph.Nodes.Add(new ExecutionActionNode
            {
                Layer = action.Kind,
                CapabilityId = action.CapabilityIds.FirstOrDefault() ?? action.Id,
                OutcomeActionId = action.Id,
                Purpose = action.Purpose,
                Required = true,
                Status = ExecutionActionStatuses.Blocked,
                InputFileIds = action.InputFileIds
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                DependencyIds = [.. dependencyIds],
                ExpectedEvidenceTypes = action.ExpectedEvidenceTypes
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            });
        }
    }

    private static IReadOnlyList<SessionFileReference> ResolveRelevantInputFiles(
        string capability,
        SessionFileManifest manifest)
    {
        var canonical = ComponentCapabilityAliasCatalog.Canonicalize(capability);
        var category = canonical.Contains(".image", StringComparison.OrdinalIgnoreCase)
            ? SessionFileCategories.Image
            : canonical.Contains(".audio", StringComparison.OrdinalIgnoreCase)
                ? SessionFileCategories.Audio
                : canonical.Contains(".video", StringComparison.OrdinalIgnoreCase)
                    ? SessionFileCategories.Video
                    : string.Empty;
        if (category.Length == 0)
        {
            return [];
        }

        return manifest.Files
            .Where(file => file.IsAvailable
                && string.Equals(file.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool RequiresExternalEvidence(ExecutorHandoffPackage handoff)
    {
        if (handoff.ArtifactContract.RequiresExternalEvidence
            || handoff.FileManifest.FileCount > 0
            || handoff.NeedsWeb
            || handoff.RequiredTools.Count > 0)
        {
            return true;
        }

        return handoff.ArtifactContract.ArtifactKind is ArtifactKinds.Image
            or ArtifactKinds.Audio
            or ArtifactKinds.Video
            or ArtifactKinds.Table
            or ArtifactKinds.Archive;
    }
}
