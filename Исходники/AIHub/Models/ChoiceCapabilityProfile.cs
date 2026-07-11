namespace AIHub.Models;

public static class ChoiceDecisionDimensions
{
    public const string TaskType = "task_type";
    public const string DomainSpecialization = "domain_specialization";
    public const string ReasoningStrength = "reasoning_strength";
    public const string KnowledgeFreshness = "knowledge_freshness";
    public const string ContextVolume = "context_volume";
    public const string InputModality = "input_modality";
    public const string OutputModality = "output_modality";
    public const string ToolRequirements = "tool_requirements";
    public const string SpecializationNeed = "specialization_need";
    public const string LanguageQuality = "language_quality";
    public const string LatencyPriority = "latency_priority";
    public const string AccuracyPriority = "accuracy_priority";
    public const string PrivacyRequirement = "privacy_requirement";
    public const string HardwareBudget = "hardware_budget";
    public const string ExecutionMode = "execution_mode";

    public static IReadOnlyList<string> All { get; } =
    [
        TaskType,
        DomainSpecialization,
        ReasoningStrength,
        KnowledgeFreshness,
        ContextVolume,
        InputModality,
        OutputModality,
        ToolRequirements,
        SpecializationNeed,
        LanguageQuality,
        LatencyPriority,
        AccuracyPriority,
        PrivacyRequirement,
        HardwareBudget,
        ExecutionMode
    ];

    public static bool IsKnown(string value) => All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

public static class ChoiceSelectionImpacts
{
    public static IReadOnlyList<string> All { get; } =
    [
        "model_class", "model_size", "reasoning_strength", "context_window", "web_access",
        "file_access", "rag_required", "multimodal_required", "code_capability",
        "image_generation", "audio_capability", "video_capability", "specialization",
        "backend", "hardware_load", "latency", "privacy", "language_quality", "tool_set"
    ];

    public static bool IsKnown(string value) => All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

public static class ChoiceDimensionStatuses
{
    public const string Unknown = "unknown";
    public const string Provisional = "provisional";
    public const string Resolved = "resolved";
    public const string NotApplicable = "not_applicable";

    public static IReadOnlyList<string> All { get; } = [Unknown, Provisional, Resolved, NotApplicable];

    public static bool IsKnown(string value) => All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

public sealed class ChoiceCapabilityDimension
{
    public string Dimension { get; set; } = string.Empty;

    public string Status { get; set; } = ChoiceDimensionStatuses.Unknown;

    public List<string> Values { get; set; } = [];

    public string Evidence { get; set; } = string.Empty;
}

public sealed class ChoiceCapabilityProfile
{
    public List<ChoiceCapabilityDimension> Dimensions { get; set; } = [];

    public ChoiceCapabilityProfile Clone() => new()
    {
        Dimensions = Dimensions.Select(CloneDimension).ToList()
    };

    public void ReplaceWith(ChoiceCapabilityProfile source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Dimensions = source.Dimensions.Select(CloneDimension).ToList();
    }

    public void Apply(IEnumerable<ChoiceCapabilityDimension> updates)
    {
        foreach (var update in updates)
        {
            var existing = Dimensions.FirstOrDefault(item =>
                string.Equals(item.Dimension, update.Dimension, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                Dimensions.Add(CloneDimension(update));
                continue;
            }

            existing.Status = update.Status;
            existing.Values = update.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            existing.Evidence = update.Evidence;
        }
    }

    public string GetStatus(string dimension) => Dimensions.FirstOrDefault(item =>
        string.Equals(item.Dimension, dimension, StringComparison.OrdinalIgnoreCase))?.Status
        ?? ChoiceDimensionStatuses.Unknown;

    public IReadOnlyList<string> ResolvedDimensions => Dimensions
        .Where(item => item.Status is ChoiceDimensionStatuses.Resolved or ChoiceDimensionStatuses.NotApplicable)
        .Select(item => item.Dimension)
        .ToList();

    private static ChoiceCapabilityDimension CloneDimension(ChoiceCapabilityDimension source) => new()
    {
        Dimension = source.Dimension,
        Status = source.Status,
        Values = source.Values.ToList(),
        Evidence = source.Evidence
    };
}
