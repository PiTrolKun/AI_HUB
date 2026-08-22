using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutionOutcomeContractService
{
    public ExecutionOutcomeContract Build(
        string goal,
        ChoiceCapabilityProfile profile,
        SessionFilePromptManifest? fileManifest,
        IReadOnlyList<SandboxWorkPattern>? workPatterns,
        ArtifactContract artifactContract)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(artifactContract);

        var contract = new ExecutionOutcomeContract
        {
            Goal = goal?.Trim() ?? string.Empty,
            ArtifactKind = artifactContract.ArtifactKind
        };
        var selectedPatterns = workPatterns ?? [];

        foreach (var file in fileManifest?.Files.Where(file => file.IsAvailable) ?? [])
        {
            var capability = ResolveInputCapability(file);
            if (capability.Length > 0)
            {
                AddCapabilityAction(
                    contract,
                    capability,
                    ExecutionOutcomeActionKinds.InputAccess,
                    $"Read the attached {NormalizeExtension(file.Extension).TrimStart('.')} input.",
                    required: true,
                    inputFileIds: [file.Id]);
            }
        }

        foreach (var pattern in selectedPatterns)
        {
            foreach (var capability in pattern.RequiredCapabilities)
            {
                if (ComponentCapabilityMapper.IsExplicitlyDenied(profile, capability))
                {
                    continue;
                }

                AddCapabilityAction(
                    contract,
                    capability,
                    InferKind(capability),
                    $"Complete the required '{pattern.Id}' operation.",
                    required: true,
                    inputFileIds: ResolveRelevantInputFileIds(capability, fileManifest));
            }

            foreach (var capability in pattern.OptionalCapabilities)
            {
                if (ComponentCapabilityMapper.IsExplicitlyDenied(profile, capability))
                {
                    continue;
                }

                AddCapabilityAction(
                    contract,
                    capability,
                    InferKind(capability),
                    $"Improve the '{pattern.Id}' result when this capability is available.",
                    required: false,
                    inputFileIds: ResolveRelevantInputFileIds(capability, fileManifest));
            }
        }

        foreach (var capability in ComponentCapabilityMapper.FromProfile(profile))
        {
            AddCapabilityAction(
                contract,
                capability,
                InferKind(capability),
                $"Fulfil the capability inferred from the task: {capability}.",
                required: true,
                inputFileIds: ResolveRelevantInputFileIds(capability, fileManifest));
        }

        contract.Actions.Add(new ExecutionOutcomeAction
        {
            Id = "outcome.artifact",
            Kind = ExecutionOutcomeActionKinds.ArtifactProduction,
            Purpose = $"Produce the requested {artifactContract.ArtifactKind} result.",
            Required = true,
            RequiresExecutionComponent = false,
            ExpectedEvidenceTypes = [ExecutionEvidenceTypes.ProducedArtifact]
        });
        contract.ExpectedResultClaims = BuildExpectedClaims(contract);
        return contract;
    }

    private static void AddCapabilityAction(
        ExecutionOutcomeContract contract,
        string capability,
        string kind,
        string purpose,
        bool required,
        IReadOnlyCollection<string>? inputFileIds = null)
    {
        var canonical = ComponentCapabilityAliasCatalog.Canonicalize(capability);
        if (canonical.Length == 0)
        {
            return;
        }

        var existing = contract.Actions.FirstOrDefault(action =>
            action.CapabilityIds.Contains(canonical, StringComparer.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Required |= required;
            existing.InputFileIds = existing.InputFileIds
                .Concat(inputFileIds ?? [])
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return;
        }

        contract.Actions.Add(new ExecutionOutcomeAction
        {
            Id = $"outcome.{canonical}",
            Kind = kind,
            Purpose = purpose,
            Required = required,
            RequiresExecutionComponent = true,
            CapabilityIds = [canonical],
            InputFileIds = inputFileIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? [],
            ExpectedEvidenceTypes =
            [
                kind == ExecutionOutcomeActionKinds.Transformation
                    ? ExecutionEvidenceTypes.ProducedArtifact
                    : ExecutionEvidenceTypes.ToolResult
            ]
        });
    }

    private static IReadOnlyList<string> ResolveRelevantInputFileIds(
        string capability,
        SessionFilePromptManifest? fileManifest)
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

        return fileManifest?.Files
            .Where(file => file.IsAvailable
                && string.Equals(file.Category, category, StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];
    }

    private static List<string> BuildExpectedClaims(ExecutionOutcomeContract contract)
    {
        var claims = contract.Actions
            .Where(action => action.Required && action.RequiresExecutionComponent)
            .SelectMany(action => action.CapabilityIds)
            .Select(capability => $"The result may claim '{capability}' only when matching execution evidence exists.")
            .ToList();
        claims.Add($"The final artifact must be a '{contract.ArtifactKind}' result.");
        return claims;
    }

    private static string InferKind(string capability)
    {
        var canonical = ComponentCapabilityAliasCatalog.Canonicalize(capability);
        if (canonical.StartsWith("analyze.", StringComparison.OrdinalIgnoreCase)
            || canonical.StartsWith("extract.", StringComparison.OrdinalIgnoreCase))
        {
            return ExecutionOutcomeActionKinds.ContentUnderstanding;
        }

        if (canonical.StartsWith("edit.", StringComparison.OrdinalIgnoreCase)
            || canonical.StartsWith("convert.", StringComparison.OrdinalIgnoreCase)
            || canonical.StartsWith("generate.", StringComparison.OrdinalIgnoreCase)
            || canonical.StartsWith("restore.", StringComparison.OrdinalIgnoreCase)
            || canonical.StartsWith("enhance.", StringComparison.OrdinalIgnoreCase))
        {
            return ExecutionOutcomeActionKinds.Transformation;
        }

        return ExecutionOutcomeActionKinds.InputAccess;
    }

    private static string ResolveInputCapability(SessionFilePromptItem file)
    {
        var extension = NormalizeExtension(file.Extension);
        return file.Category switch
        {
            SessionFileCategories.Image => "read.image_pixels",
            SessionFileCategories.Audio => "read.audio",
            SessionFileCategories.Video => "read.video",
            SessionFileCategories.Text or SessionFileCategories.Code => "read.text",
            SessionFileCategories.Archive => "read.archive",
            SessionFileCategories.Table when extension == ".csv" => "read.csv",
            SessionFileCategories.Table => "read.office_openxml",
            SessionFileCategories.Document when extension == ".pdf" => "read.pdf_text",
            SessionFileCategories.Document when extension is ".docx" or ".pptx" => "read.office_openxml",
            _ => string.Empty
        };
    }

    private static string NormalizeExtension(string value)
    {
        var extension = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return extension.Length == 0 || extension.StartsWith('.')
            ? extension
            : $".{extension}";
    }
}
