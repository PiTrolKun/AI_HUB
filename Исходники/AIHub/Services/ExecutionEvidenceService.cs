using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutionEvidenceService
{
    private static readonly string[] OutputPathProperties =
    [
        "artifact_reference",
        "artifact_path",
        "output_path",
        "file_path"
    ];

    public ExecutionEvidenceReceipt CreateReceipt(
        StructuredToolCall toolCall,
        ExecutorToolExecution execution,
        ExecutionActionGraph graph,
        SessionFileManifest manifest,
        StorageSettings storageSettings)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(manifest);

        var toolName = toolCall.Function.Name.Trim();
        var inputFileId = ReadJsonString(toolCall.Function.Arguments, "file_id")
            ?? ReadJsonString(toolCall.Function.Arguments, "source_file_id")
            ?? string.Empty;
        var inputFile = manifest.Files.FirstOrDefault(file =>
            string.Equals(file.Id, inputFileId, StringComparison.Ordinal));
        var outputPath = ResolveOutputPath(execution.Content, storageSettings);
        var toolEvidenceContract = SpecialistToolEvidenceContractCatalog.Find(toolName);
        var evidenceType = ResolveEvidenceType(
            execution.Success,
            outputPath,
            toolEvidenceContract);
        var adapterCapabilities = ComponentAdapterRegistry.FindByToolName(toolName)?
            .Capabilities
            .Select(ComponentCapabilityAliasCatalog.Canonicalize)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];
        var confirmedToolCapabilities = adapterCapabilities
            .Concat(toolEvidenceContract?.ConfirmedCapabilities ?? [])
            .Select(ComponentCapabilityAliasCatalog.Canonicalize)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var linkedNodes = graph.Nodes
            .Where(ExecutionActionGraphService.IsOperational)
            .Where(node =>
                node.ToolNames.Contains(toolName, StringComparer.OrdinalIgnoreCase)
                || confirmedToolCapabilities.Any(capability =>
                    ExecutionActionGraphService.CapabilitiesMatch(
                        capability,
                        node.CapabilityId)))
            .Where(node => InputIdentityMatches(node, inputFileId))
            .ToList();
        var confirmedCapabilities = linkedNodes
            .Select(node => ComponentCapabilityAliasCatalog.Canonicalize(node.CapabilityId))
            .Concat(confirmedToolCapabilities)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var normalizedResult = new SpecialistToolResultNormalizer().Normalize(
            toolName,
            execution.Content);
        var evidenceTypeMismatch = linkedNodes.FirstOrDefault(node =>
            node.ExpectedEvidenceTypes.Count > 0
            && !node.ExpectedEvidenceTypes.Contains(
                evidenceType,
                StringComparer.OrdinalIgnoreCase));

        return new ExecutionEvidenceReceipt
        {
            ActionId = linkedNodes.FirstOrDefault()?.Id ?? string.Empty,
            OutcomeActionIds = linkedNodes
                .Select(node => node.OutcomeActionId)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ToolCallId = toolCall.Id,
            ToolName = toolName,
            ComponentIds = linkedNodes
                .SelectMany(node => node.ComponentIds)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Command = execution.Command,
            ArgumentsHash = HashText(toolCall.Function.Arguments),
            InputFileId = inputFile?.Id ?? inputFileId,
            InputFileName = inputFile?.DisplayName ?? string.Empty,
            InputSha256 = HashFile(inputFile?.SourcePath),
            OutputSha256 = HashFile(outputPath),
            OutputArtifactPath = outputPath,
            ResultHash = HashText(execution.Content),
            ResultExcerpt = CreateExcerpt(execution.Content),
            NormalizedResultText = normalizedResult.UserText,
            EvidenceType = evidenceType,
            ConfirmedClaimScopes = [.. confirmedCapabilities],
            Limitations = ResolveLimitations(execution.Success, outputPath),
            DiagnosticMessage = !execution.Success
                ? CreateExcerpt(execution.Content)
                : evidenceTypeMismatch is null
                    ? string.Empty
                    : $"Evidence type '{evidenceType}' is incompatible with action "
                        + $"'{evidenceTypeMismatch.Id}' for capability "
                        + $"'{evidenceTypeMismatch.CapabilityId}'. Expected: "
                        + string.Join(", ", evidenceTypeMismatch.ExpectedEvidenceTypes) + ".",
            Success = execution.Success,
            Capabilities = confirmedCapabilities
        };
    }

    public EvidenceValidationResult Validate(
        ExecutionActionGraph graph,
        IReadOnlyCollection<ExecutionEvidenceReceipt> receipts)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(receipts);

        var result = new EvidenceValidationResult
        {
            GraphId = graph.Id,
            ReceiptIds = receipts.Select(receipt => receipt.Id).Distinct().ToList()
        };
        var requiredNodes = graph.Nodes
            .Where(ExecutionActionGraphService.IsOperational)
            .Where(node => node.Required)
            .ToList();

        if (!graph.RequiresExternalEvidence)
        {
            result.Status = EvidenceValidationStatuses.Valid;
            result.Warnings.Add("No external or file-backed evidence is required for this text/dialog task.");
            return result;
        }

        if (requiredNodes.Count == 0)
        {
            result.Status = EvidenceValidationStatuses.Invalid;
            result.Warnings.Add(
                "The route requires external evidence but contains no executable evidence action.");
            return result;
        }

        foreach (var node in requiredNodes)
        {
            var expectedInputFileIds = node.InputFileIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var succeeded = expectedInputFileIds.Count == 0
                ? receipts.Any(receipt =>
                    receipt.Success
                    && ExecutionActionGraphService.ReceiptMatches(node, receipt))
                : expectedInputFileIds.All(fileId => receipts.Any(receipt =>
                    receipt.Success
                    && string.Equals(
                        receipt.InputFileId,
                        fileId,
                        StringComparison.OrdinalIgnoreCase)
                    && ExecutionActionGraphService.ReceiptMatches(node, receipt)));
            if (succeeded)
            {
                result.SatisfiedActionIds.Add(node.Id);
            }
            else
            {
                result.MissingActionIds.Add(node.Id);
                var mismatchedReceipt = receipts.FirstOrDefault(receipt =>
                    receipt.Success
                    && (string.Equals(receipt.ActionId, node.Id, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(node.OutcomeActionId)
                            && receipt.OutcomeActionIds.Contains(
                                node.OutcomeActionId,
                                StringComparer.OrdinalIgnoreCase)))
                    && node.ExpectedEvidenceTypes.Count > 0
                    && !node.ExpectedEvidenceTypes.Contains(
                        receipt.EvidenceType,
                        StringComparer.OrdinalIgnoreCase));
                result.Warnings.Add(mismatchedReceipt is null
                    ? $"No successful tool receipt for required capability '{node.CapabilityId}'"
                        + (expectedInputFileIds.Count == 0
                            ? "."
                            : $" for every required input file ({string.Join(", ", expectedInputFileIds)}).")
                    : $"Receipt '{mismatchedReceipt.Id}' is linked to capability "
                        + $"'{node.CapabilityId}', but evidence type "
                        + $"'{mismatchedReceipt.EvidenceType}' is incompatible. Expected: "
                        + string.Join(", ", node.ExpectedEvidenceTypes) + ".");
            }
        }

        result.Status = result.MissingActionIds.Count == 0
            ? EvidenceValidationStatuses.Valid
            : receipts.Any(receipt => receipt.Success)
                ? EvidenceValidationStatuses.Limited
                : EvidenceValidationStatuses.Invalid;
        return result;
    }

    public TaskFulfillmentValidationResult ValidateTask(
        ArtifactValidationResult technicalValidation,
        EvidenceValidationResult evidenceValidation,
        string goal)
    {
        ArgumentNullException.ThrowIfNull(technicalValidation);
        ArgumentNullException.ThrowIfNull(evidenceValidation);

        var result = new TaskFulfillmentValidationResult
        {
            Goal = goal,
            TechnicalStatus = technicalValidation.Status,
            EvidenceStatus = evidenceValidation.Status
        };
        result.Checks.Add($"technical_artifact:{technicalValidation.Status}");
        result.Checks.Add($"execution_evidence:{evidenceValidation.Status}");

        if (!technicalValidation.IsValid)
        {
            result.Status = TaskFulfillmentStatuses.Failed;
            result.Warnings.AddRange(technicalValidation.Errors);
        }
        else if (evidenceValidation.IsValid)
        {
            result.Status = TaskFulfillmentStatuses.Complete;
        }
        else
        {
            result.Status = TaskFulfillmentStatuses.Limited;
            result.Warnings.AddRange(evidenceValidation.Warnings);
        }

        return result;
    }

    public string BuildHonestLimitedMarkdown(
        ExecutorHandoffPackage handoff,
        string confirmedBrief,
        ExecutionActionGraph graph,
        IReadOnlyCollection<ExecutionEvidenceReceipt> receipts,
        EvidenceValidationResult validation,
        IReadOnlyCollection<string>? workingFragments = null,
        string? currentResultSummary = null) =>
        new LimitedResultDocumentBuilder().Build(
            handoff,
            confirmedBrief,
            graph,
            receipts,
            validation,
            workingFragments,
            currentResultSummary);

    public string BuildEvidencePacket(
        ExecutionActionGraph graph,
        IReadOnlyCollection<ExecutionEvidenceReceipt> receipts,
        EvidenceValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(receipts);
        ArgumentNullException.ThrowIfNull(validation);

        var builder = new StringBuilder();
        builder.AppendLine("[AI_HUB_VERIFIED_EXECUTION_EVIDENCE]");
        builder.AppendLine($"Graph: {graph.Id}");
        builder.AppendLine($"Validation: {validation.Status}");
        builder.AppendLine(
            "This packet is the program-owned factual boundary for external, file-backed and tool-produced claims in the result.");
        builder.AppendLine(
            "A receipt proves only the operation and output stated in that receipt. It does not prove plausible details absent from its excerpt or artifact.");

        var successful = receipts
            .Where(receipt => receipt.Success)
            .OrderBy(receipt => receipt.CreatedAt)
            .TakeLast(24)
            .ToList();
        if (successful.Count == 0)
        {
            builder.AppendLine("Successful receipts: none.");
        }
        else
        {
            builder.AppendLine("Successful receipts:");
            var resultNormalizer = new SpecialistToolResultNormalizer();
            foreach (var receipt in successful)
            {
                var node = graph.Nodes.FirstOrDefault(item =>
                    string.Equals(item.Id, receipt.ActionId, StringComparison.OrdinalIgnoreCase)
                    || item.ReceiptIds.Contains(receipt.Id, StringComparer.OrdinalIgnoreCase));
                builder.AppendLine($"- Receipt: {receipt.Id}");
                builder.AppendLine($"  Capability: {FirstNonEmpty(node?.CapabilityId, receipt.Capabilities.FirstOrDefault())}");
                builder.AppendLine($"  Components: {JoinOrFallback(receipt.ComponentIds, "not-recorded")}");
                builder.AppendLine($"  Tool: {receipt.ToolName}");
                builder.AppendLine($"  Evidence type: {receipt.EvidenceType}");
                builder.AppendLine($"  Confirmed scopes: {JoinOrFallback(receipt.ConfirmedClaimScopes, "operation-only")}");
                builder.AppendLine($"  Input: {FirstNonEmpty(receipt.InputFileName, receipt.InputFileId, "none")}");
                builder.AppendLine($"  Input SHA-256: {FirstNonEmpty(receipt.InputSha256, "not-recorded")}");
                builder.AppendLine($"  Output artifact: {FirstNonEmpty(receipt.OutputArtifactPath, "none")}");
                builder.AppendLine($"  Output SHA-256: {FirstNonEmpty(receipt.OutputSha256, "not-recorded")}");
                builder.AppendLine($"  Result SHA-256: {receipt.ResultHash}");
                var normalizedResult = resultNormalizer.Normalize(
                    receipt.ToolName,
                    receipt.ResultExcerpt);
                builder.AppendLine($"  Verified result: {Shorten(
                    NormalizeExcerpt(FirstNonEmpty(
                        receipt.NormalizedResultText,
                        normalizedResult.UserText)),
                    800)}");
                builder.AppendLine($"  Limits: {FirstNonEmpty(receipt.Limitations, "none-recorded")}");
            }
        }

        if (validation.MissingActionIds.Count > 0)
        {
            builder.AppendLine("Required actions without successful evidence:");
            foreach (var actionId in validation.MissingActionIds)
            {
                var node = graph.Nodes.FirstOrDefault(item =>
                    string.Equals(item.Id, actionId, StringComparison.OrdinalIgnoreCase));
                builder.AppendLine(
                    $"- {FirstNonEmpty(node?.CapabilityId, actionId)}: {FirstNonEmpty(node?.Purpose, "No verified result.")}");
            }
        }

        builder.AppendLine("[/AI_HUB_VERIFIED_EXECUTION_EVIDENCE]");
        return builder.ToString();
    }

    private static string ResolveOutputPath(string content, StorageSettings storageSettings)
    {
        foreach (var property in OutputPathProperties)
        {
            var value = ReadJsonString(content, property);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Path.IsPathRooted(value))
            {
                var absolute = Path.GetFullPath(value);
                return File.Exists(absolute) ? absolute : string.Empty;
            }

            var root = ResolveArtifactRoot(storageSettings);
            var candidate = Path.GetFullPath(Path.Combine(
                root,
                value.Replace('/', Path.DirectorySeparatorChar)));
            if (candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                && File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static string ResolveEvidenceType(
        bool success,
        string outputPath,
        SpecialistToolEvidenceContract? toolContract)
    {
        if (!success)
        {
            return ExecutionEvidenceTypes.FailedExecution;
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            return ExecutionEvidenceTypes.ProducedArtifact;
        }

        return toolContract?.EvidenceType ?? ExecutionEvidenceTypes.ToolResult;
    }

    private static bool InputIdentityMatches(ExecutionActionNode node, string inputFileId) =>
        node.InputFileIds.Count == 0
        || (!string.IsNullOrWhiteSpace(inputFileId)
            && node.InputFileIds.Contains(inputFileId, StringComparer.OrdinalIgnoreCase));

    private static string ResolveLimitations(bool success, string outputPath)
    {
        if (!success)
        {
            return "The tool call failed and confirms no requested task result.";
        }

        return string.IsNullOrWhiteSpace(outputPath)
            ? "The receipt confirms only the recorded tool response; no physical output artifact was produced."
            : "The receipt confirms the produced artifact and the recorded excerpt only.";
    }

    private static string ResolveArtifactRoot(StorageSettings storageSettings)
    {
        var configuredRoot = storageSettings.Results.Locations
            .Select(location => location.Path?.Trim())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(AppDataPaths.RuntimeDirectory, "Sandbox", "Artifacts")
            : Path.Combine(configuredRoot, "AI_HUB", "Sandbox", "Artifacts");
        return Path.GetFullPath(root);
    }

    private static string? ReadJsonString(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return FindString(document.RootElement, propertyName);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FindString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                var nested = FindString(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindString(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string HashFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return string.Empty;
        }

        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string HashText(string? value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)))
            .ToLowerInvariant();

    private static string CreateExcerpt(string value)
    {
        var normalized = value.Trim();
        return normalized.Length <= 1200 ? normalized : normalized[..1200] + "...";
    }

    private static string NormalizeExcerpt(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "completed"
            : value.ReplaceLineEndings(" ").Trim();

    private static string Shorten(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters
            ? value
            : value[..maximumCharacters] + "...";

    private static string JoinOrFallback(IEnumerable<string> values, string fallback)
    {
        var joined = string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(joined) ? fallback : joined;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? "unspecified";
}
