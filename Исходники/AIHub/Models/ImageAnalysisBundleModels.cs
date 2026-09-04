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

public static class ImageAnalysisBundleInstallStates
{
    public const string Checking = "checking";
    public const string StorageNotConfigured = "storage_not_configured";
    public const string DownloadRequired = "download_required";
    public const string ResumeAvailable = "resume_available";
    public const string NeedsVerification = "needs_verification";
    public const string RuntimeIncompatible = "runtime_incompatible";
    public const string Corrupted = "corrupted";
    public const string Ready = "ready";
    public const string Error = "error";
}

public sealed class ImageAnalysisBundleComponentState
{
    public string ModelArtifactId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Status { get; init; } = ManagedModelStatuses.NotInstalled;

    public long TotalBytes { get; init; }

    public long StoredBytes { get; init; }

    public bool IsShared { get; init; }

    public string LastError { get; init; } = string.Empty;

    public string RepositoryId { get; init; } = string.Empty;

    public string Revision { get; init; } = string.Empty;

    public string License { get; init; } = string.Empty;
}

public sealed class ImageAnalysisBundleInstallationSnapshot
{
    public string State { get; init; } = ImageAnalysisBundleInstallStates.Checking;

    public IReadOnlyList<ImageAnalysisBundleComponentState> Components { get; init; } = [];

    public string ModelsRoot { get; init; } = string.Empty;

    public long MissingBytes { get; init; }

    public long AvailableFreeBytes { get; init; }

    public long StoredBytes => Components.Sum(component => component.StoredBytes);

    public bool CanStart => State == ImageAnalysisBundleInstallStates.Ready;

    public bool CanRemoveVision => Components.Any(component =>
        component.Role == ManagedModelRoles.Vision
        && component.StoredBytes > 0);
}
