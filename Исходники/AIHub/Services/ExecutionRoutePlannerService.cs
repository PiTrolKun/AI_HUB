using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutionRoutePlannerService
{
    private static readonly HashSet<string> WpfImageExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tif", ".tiff", ".wdp"
        };

    private static readonly string[] SemanticMarkers =
    [
        "analysis", "analyz", "understand", "recogn", "caption", "description",
        "interpret", "meaning", "semantic", "vision", "multimodal",
        "information_interpretation"
    ];

    private static readonly string[] EditingMarkers =
    [
        "edit", "modify", "enhanc", "upscal", "restor", "replace", "remove"
    ];

    private static readonly string[] GenerationMarkers =
    [
        "generat", "synthes", "creation", "create", "compos"
    ];

    private readonly CapabilityResolverService _resolver;

    public ExecutionRoutePlannerService(CapabilityResolverService? resolver = null)
    {
        _resolver = resolver ?? new CapabilityResolverService(new ComponentManager());
    }

    public ExecutionRoutePlan Build(
        ChoiceCapabilityProfile profile,
        SessionFilePromptManifest? fileManifest,
        string reason,
        IReadOnlyList<SandboxWorkPattern>? workPatterns = null,
        ChoiceExecutionPlan? executionPlan = null,
        ExecutionOutcomeContract? outcomeContract = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var requirements = BuildRequirements(
            profile,
            fileManifest,
            workPatterns,
            outcomeContract);
        ApplyCoreExecutionPlan(requirements, executionPlan);
        var plan = new ExecutionRoutePlan
        {
            SourceFormats = GetSourceFormats(fileManifest),
            Requirements = requirements,
            Resolution = _resolver.Resolve(
                requirements.Select(requirement => requirement.Request),
                reason,
                executionPlan?.PreferredComponentIds)
        };
        ApplyOutcomeCoverage(plan, outcomeContract);
        AddResolutionWarnings(plan);
        return plan;
    }

    public ExecutionRoutePlan ApplyExecutionPlan(
        ExecutionRoutePlan baseline,
        ChoiceExecutionPlan executionPlan,
        string reason,
        ExecutionOutcomeContract? outcomeContract = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(executionPlan);

        var requirements = baseline.Requirements
            .Select(requirement => new ExecutionRouteRequirement
            {
                Layer = requirement.Layer,
                Request = new ExecutorCapabilityRequest
                {
                    Id = requirement.Request.Id,
                    Purpose = requirement.Request.Purpose,
                    Required = requirement.Request.Required,
                    Alternatives = requirement.Request.Alternatives.ToList()
                }
            })
            .ToList();
        ApplyCoreExecutionPlan(requirements, executionPlan);
        var plan = new ExecutionRoutePlan
        {
            SourceFormats = baseline.SourceFormats.ToList(),
            Requirements = requirements,
            Resolution = _resolver.Resolve(
                requirements.Select(requirement => requirement.Request),
                reason,
                executionPlan.PreferredComponentIds)
        };
        ApplyOutcomeCoverage(plan, outcomeContract);
        AddResolutionWarnings(plan);
        return plan;
    }

    private static void AddResolutionWarnings(ExecutionRoutePlan plan)
    {
        if (plan.Resolution.Bindings.Any(binding =>
                binding.Required
                && binding.Status == CapabilityBindingStatuses.AdapterMissing))
        {
            plan.Warnings.Add(
                "A provider exists, but LOPATA has no trusted callable adapter for one or more required route stages.");
        }

        if (plan.Resolution.Bindings.Any(binding =>
                binding.Required
                && binding.Status == CapabilityBindingStatuses.UnknownCapability))
        {
            plan.Warnings.Add(
                "One or more required route stages have no trusted provider in the local catalog.");
        }
    }

    private static void ApplyCoreExecutionPlan(
        ICollection<ExecutionRouteRequirement> requirements,
        ChoiceExecutionPlan? executionPlan)
    {
        if (executionPlan is null)
        {
            return;
        }

        foreach (var capability in executionPlan.RequiredCapabilities)
        {
            UpsertCoreRequirement(requirements, capability, required: true);
        }

        foreach (var capability in executionPlan.OptionalCapabilities)
        {
            UpsertCoreRequirement(requirements, capability, required: false);
        }
    }

    private static void UpsertCoreRequirement(
        ICollection<ExecutionRouteRequirement> requirements,
        string capability,
        bool required)
    {
        capability = Normalize(capability);
        if (capability.Length == 0)
        {
            return;
        }

        var existing = requirements.FirstOrDefault(requirement => string.Equals(
            requirement.Request.Id,
            capability,
            StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            if (required)
            {
                existing.Request.Required = true;
            }

            return;
        }

        AddRequirement(
            requirements,
            InferLayer(capability),
            capability,
            required
                ? "Required by the core-authored execution plan."
                : "Optional reserve capability chosen by the core.",
            required);
    }

    public static List<ExecutionRouteRequirement> BuildRequirements(
        ChoiceCapabilityProfile profile,
        SessionFilePromptManifest? fileManifest,
        IReadOnlyList<SandboxWorkPattern>? workPatterns = null,
        ExecutionOutcomeContract? outcomeContract = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var values = profile.Dimensions
            .Where(dimension => dimension.Status is ChoiceDimensionStatuses.Resolved
                or ChoiceDimensionStatuses.Provisional)
            .SelectMany(dimension => dimension.Values)
            .Select(Normalize)
            .Where(value => value.Length > 0)
            .ToList();
        var manifestFiles = fileManifest?.Files
            .Where(file => file.IsAvailable)
            .ToList() ?? [];
        var requirements = new List<ExecutionRouteRequirement>();

        foreach (var action in outcomeContract?.Actions.Where(action =>
                     action.RequiresExecutionComponent) ?? [])
        {
            AddOutcomeRequirement(requirements, action);
        }

        foreach (var capability in ComponentCapabilityMapper.FromProfile(profile))
        {
            if (capability is "read.image_pixels" or "read.audio" or "read.video")
            {
                continue;
            }

            AddRequirement(
                requirements,
                InferLayer(capability),
                capability,
                $"Required by the task capability profile: {capability}.",
                required: outcomeContract is null);
        }

        foreach (var pattern in workPatterns ?? [])
        {
            foreach (var capability in pattern.RequiredCapabilities)
            {
                if (ComponentCapabilityMapper.IsExplicitlyDenied(profile, capability))
                {
                    continue;
                }

                AddRequirement(
                    requirements,
                    InferLayer(capability),
                    capability,
                    $"Required by Sandbox work pattern '{pattern.Id}'.",
                    required: outcomeContract is null);
            }

            foreach (var capability in pattern.OptionalCapabilities)
            {
                if (ComponentCapabilityMapper.IsExplicitlyDenied(profile, capability))
                {
                    continue;
                }

                AddRequirement(
                    requirements,
                    InferLayer(capability),
                    capability,
                    $"Optional improvement for Sandbox work pattern '{pattern.Id}'.",
                    required: false);
            }
        }

        foreach (var file in manifestFiles)
        {
            AddFileRequirement(requirements, file);
        }

        var categories = manifestFiles
            .Select(file => file.Category)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        AddSemanticRequirement(requirements, values, categories, SessionFileCategories.Image);
        AddSemanticRequirement(requirements, values, categories, SessionFileCategories.Audio);
        AddSemanticRequirement(requirements, values, categories, SessionFileCategories.Video);

        if (manifestFiles.Count == 0)
        {
            AddProfileOnlyMediaRequirements(requirements, values);
        }

        return requirements
            .GroupBy(requirement => requirement.Request.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static void AddFileRequirement(
        ICollection<ExecutionRouteRequirement> requirements,
        SessionFilePromptItem file)
    {
        var extension = NormalizeExtension(file.Extension);
        switch (file.Category)
        {
            case SessionFileCategories.Image:
                AddRequirement(
                    requirements,
                    ExecutionRouteLayers.Decode,
                    WpfImageExtensions.Contains(extension)
                        ? "read.image_pixels"
                        : "read.image_extended",
                    $"Decode the attached {extension.TrimStart('.').ToUpperInvariant()} image.");
                break;
            case SessionFileCategories.Audio:
                AddRequirement(
                    requirements,
                    ExecutionRouteLayers.Decode,
                    "read.audio",
                    $"Decode the attached {extension.TrimStart('.').ToUpperInvariant()} audio.");
                break;
            case SessionFileCategories.Video:
                AddRequirement(
                    requirements,
                    ExecutionRouteLayers.Decode,
                    "read.video",
                    $"Decode the attached {extension.TrimStart('.').ToUpperInvariant()} video.");
                break;
            default:
                foreach (var capability in MapFileCapability(file))
                {
                    AddRequirement(
                        requirements,
                        ExecutionRouteLayers.FileAccess,
                        capability,
                        $"Read the attached {extension.TrimStart('.').ToUpperInvariant()} file.");
                }
                break;
        }
    }

    private static IEnumerable<string> MapFileCapability(SessionFilePromptItem file)
    {
        var extension = NormalizeExtension(file.Extension);
        return extension switch
        {
            ".docx" or ".xlsx" or ".pptx" => ["read.office_openxml"],
            ".pdf" => ["read.pdf_text"],
            ".zip" or ".rar" or ".7z" => ["read.archive"],
            ".csv" => ["read.csv"],
            ".html" or ".htm" => ["read.html"],
            ".svg" => ["read.svg"],
            ".md" or ".markdown" => ["read.markdown"],
            ".yaml" or ".yml" => ["read.yaml"],
            ".eml" => ["read.email"],
            ".sqlite" or ".db" => ["read.database.sqlite"],
            _ when file.Category is SessionFileCategories.Text or SessionFileCategories.Code
                => ["read.text"],
            _ => []
        };
    }

    private static void AddSemanticRequirement(
        ICollection<ExecutionRouteRequirement> requirements,
        IReadOnlyCollection<string> values,
        IReadOnlySet<string> categories,
        string category)
    {
        if (!categories.Contains(category)
            || !RequiresSemanticUnderstanding(values, category))
        {
            return;
        }

        AddRequirement(
            requirements,
            ExecutionRouteLayers.SemanticAnalysis,
            $"analyze.{category}.semantic",
            $"Understand the semantic content of the attached {category}.");
    }

    private static void AddProfileOnlyMediaRequirements(
        ICollection<ExecutionRouteRequirement> requirements,
        IReadOnlyCollection<string> values)
    {
        foreach (var category in new[]
                 {
                     SessionFileCategories.Image,
                     SessionFileCategories.Audio,
                     SessionFileCategories.Video
                 })
        {
            if (!values.Any(value => IsCategoryMarker(value, category)))
            {
                continue;
            }

            AddRequirement(
                requirements,
                ExecutionRouteLayers.Decode,
                category switch
                {
                    SessionFileCategories.Image => "read.image_pixels",
                    SessionFileCategories.Audio => "read.audio",
                    _ => "read.video"
                },
                $"Read the task's {category} input.");
            if (RequiresSemanticUnderstanding(values, category))
            {
                AddRequirement(
                    requirements,
                    ExecutionRouteLayers.SemanticAnalysis,
                    $"analyze.{category}.semantic",
                    $"Understand the semantic content of the task's {category} input.");
            }
        }
    }

    private static bool RequiresSemanticUnderstanding(
        IReadOnlyCollection<string> values,
        string category) =>
        values.Any(value =>
            IsCategoryMarker(value, category)
            && (SemanticMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                || (!EditingMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    && !GenerationMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase)))))
        || values.Any(value => SemanticMarkers.Any(marker =>
            value.Contains(marker, StringComparison.OrdinalIgnoreCase)));

    private static bool IsCategoryMarker(string value, string category) =>
        category switch
        {
            SessionFileCategories.Image => ContainsAny(value, ["image", "photo", "vision", "picture"]),
            SessionFileCategories.Audio => ContainsAny(value, ["audio", "music", "song", "speech", "voice"]),
            SessionFileCategories.Video => value.Contains("video", StringComparison.OrdinalIgnoreCase),
            _ => false
        };

    private static string InferLayer(string capability) =>
        capability.StartsWith("read.", StringComparison.OrdinalIgnoreCase)
        || capability.StartsWith("extract.", StringComparison.OrdinalIgnoreCase)
            ? ExecutionRouteLayers.FileAccess
            : capability.StartsWith("analyze.", StringComparison.OrdinalIgnoreCase)
            || capability.StartsWith("transcribe.", StringComparison.OrdinalIgnoreCase)
            || capability.StartsWith("ocr.", StringComparison.OrdinalIgnoreCase)
                ? ExecutionRouteLayers.SemanticAnalysis
                : ExecutionRouteLayers.Action;

    private static void AddRequirement(
        ICollection<ExecutionRouteRequirement> requirements,
        string layer,
        string capability,
        string purpose,
        bool required = true)
    {
        capability = ComponentCapabilityAliasCatalog.Canonicalize(capability);
        if (string.IsNullOrWhiteSpace(capability)
            || requirements.Any(requirement => string.Equals(
                requirement.Request.Id,
                capability,
                StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        requirements.Add(new ExecutionRouteRequirement
        {
            Layer = layer,
            Request = new ExecutorCapabilityRequest
            {
                Id = capability,
                Purpose = purpose,
                Required = required
            }
        });
    }

    private static void AddOutcomeRequirement(
        ICollection<ExecutionRouteRequirement> requirements,
        ExecutionOutcomeAction action)
    {
        var capabilities = action.CapabilityIds
            .Select(ComponentCapabilityAliasCatalog.Canonicalize)
            .Where(capability => capability.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (capabilities.Count == 0)
        {
            return;
        }

        AddRequirement(
            requirements,
            InferLayer(capabilities[0]),
            capabilities[0],
            action.Purpose,
            action.Required);
        var requirement = requirements.First(item => string.Equals(
            item.Request.Id,
            capabilities[0],
            StringComparison.OrdinalIgnoreCase));
        requirement.Request.Alternatives = capabilities.Skip(1).ToList();
    }

    private static void ApplyOutcomeCoverage(
        ExecutionRoutePlan plan,
        ExecutionOutcomeContract? outcomeContract)
    {
        var actions = outcomeContract?.Actions
            .Where(action => action.Required && action.RequiresExecutionComponent)
            .ToList() ?? [];
        plan.RequiredOutcomeActionCount = actions.Count;
        if (actions.Count == 0)
        {
            plan.CoveredOutcomeActionCount = 0;
            plan.OutcomeCoveragePercent = 100;
            return;
        }

        foreach (var action in actions)
        {
            var capabilities = action.CapabilityIds
                .Select(ComponentCapabilityAliasCatalog.Canonicalize)
                .Where(capability => capability.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var covered = plan.Resolution.Bindings.Any(binding =>
                binding.AdapterAvailable
                && binding.Status is (CapabilityBindingStatuses.Ready
                    or CapabilityBindingStatuses.ExternalCliFound
                    or CapabilityBindingStatuses.PackageMissing)
                && (capabilities.Contains(ComponentCapabilityAliasCatalog.Canonicalize(
                        binding.RequestedCapabilityId))
                    || capabilities.Contains(ComponentCapabilityAliasCatalog.Canonicalize(
                        binding.CapabilityId))));
            if (covered)
            {
                plan.CoveredOutcomeActionCount++;
            }
            else
            {
                plan.MissingOutcomeActionIds.Add(action.Id);
            }
        }

        plan.OutcomeCoveragePercent = (int)Math.Round(
            100d * plan.CoveredOutcomeActionCount / plan.RequiredOutcomeActionCount,
            MidpointRounding.AwayFromZero);
        if (!plan.HasCompleteOutcomeCoverage)
        {
            plan.Warnings.Add(
                $"The execution route covers {plan.OutcomeCoveragePercent}% of required outcome actions.");
        }
    }

    private static List<string> GetSourceFormats(SessionFilePromptManifest? manifest) =>
        manifest?.Files
            .Where(file => file.IsAvailable)
            .Select(file => NormalizeExtension(file.Extension))
            .Where(extension => extension.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static string NormalizeExtension(string value)
    {
        var extension = value.Trim().ToLowerInvariant();
        return extension.Length == 0 || extension.StartsWith('.')
            ? extension
            : $".{extension}";
    }

    private static bool ContainsAny(string value, IEnumerable<string> markers) =>
        markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string value) =>
        ComponentCapabilityAliasCatalog.Canonicalize(value);
}
