namespace AIHub.Models;

public sealed class ImageAnalysisBundleDefinition
{
    public string Id { get; init; } = string.Empty;

    public int Level { get; init; }

    public string TitleKey { get; init; } = string.Empty;

    public string PurposeKey { get; init; } = string.Empty;

    public string StatusKey { get; init; } = string.Empty;

    public IReadOnlyList<ImageAnalysisBundleComponent> Components { get; init; } = [];

    public ImageAnalysisHardwareRequirements Requirements { get; init; } = new();

    public bool IsAvailable { get; init; }

    public bool IsCurrentProjectBundle { get; init; }

    public bool IsPreliminary { get; init; } = true;
}

public sealed class ImageAnalysisBundleComponent
{
    public string RoleKey { get; init; } = string.Empty;

    public string ModelName { get; init; } = string.Empty;

    public string PlacementKey { get; init; } = string.Empty;
}

public sealed class ImageAnalysisHardwareRequirements
{
    public double RamGb { get; init; }

    public double VramGb { get; init; }

    public int LogicalProcessorCount { get; init; }

    public double FreeDiskGb { get; init; }
}

public sealed class ImageAnalysisHardwareSnapshot
{
    public double? RamGb { get; init; }

    public double? VramGb { get; init; }

    public int? LogicalProcessorCount { get; init; }

    public double? FreeDiskGb { get; init; }
}

public sealed class ImageAnalysisResourceAssessment
{
    public string ResourceId { get; init; } = string.Empty;

    public double? ActualValue { get; init; }

    public double RequiredValue { get; init; }

    public bool? IsSatisfied { get; init; }

    public double NormalizedDeficit { get; init; }
}

public sealed class ImageAnalysisBundleAssessment
{
    public required ImageAnalysisBundleDefinition Bundle { get; init; }

    public IReadOnlyList<ImageAnalysisResourceAssessment> Resources { get; init; } = [];

    public bool HasCompleteHardwareData { get; init; }

    public bool IsFullyCompatible { get; init; }

    public double TotalNormalizedDeficit { get; init; }
}

public sealed class ImageAnalysisRecommendationResult
{
    public IReadOnlyList<ImageAnalysisBundleAssessment> Assessments { get; init; } = [];

    public ImageAnalysisBundleAssessment? Recommendation { get; init; }

    public bool HasCompleteHardwareData { get; init; }

    public bool IsComfortableMatch => Recommendation?.IsFullyCompatible == true;
}
