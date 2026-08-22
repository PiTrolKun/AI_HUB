using AIHub.Models;

namespace AIHub.Services;

public sealed class ImageAnalysisRecommendationService
{
    public const string RamResourceId = "ram";
    public const string VramResourceId = "vram";
    public const string CpuResourceId = "cpu";
    public const string DiskResourceId = "disk";

    public ImageAnalysisRecommendationResult Evaluate(
        IReadOnlyList<ImageAnalysisBundleDefinition> bundles,
        ImageAnalysisHardwareSnapshot hardware)
    {
        ArgumentNullException.ThrowIfNull(bundles);
        ArgumentNullException.ThrowIfNull(hardware);

        var assessments = bundles
            .OrderBy(bundle => bundle.Level)
            .Select(bundle => Assess(bundle, hardware))
            .ToList();
        var hasCompleteData = assessments.Count > 0
            && assessments.All(assessment => assessment.HasCompleteHardwareData);

        if (!hasCompleteData)
        {
            return new ImageAnalysisRecommendationResult
            {
                Assessments = assessments,
                HasCompleteHardwareData = false
            };
        }

        var recommendation = assessments
            .Where(assessment => assessment.IsFullyCompatible)
            .OrderByDescending(assessment => assessment.Bundle.Level)
            .FirstOrDefault()
            ?? assessments
                .OrderBy(assessment => assessment.TotalNormalizedDeficit)
                .ThenBy(assessment => assessment.Bundle.Level)
                .FirstOrDefault();

        return new ImageAnalysisRecommendationResult
        {
            Assessments = assessments,
            Recommendation = recommendation,
            HasCompleteHardwareData = true
        };
    }

    private static ImageAnalysisBundleAssessment Assess(
        ImageAnalysisBundleDefinition bundle,
        ImageAnalysisHardwareSnapshot hardware)
    {
        var requirements = bundle.Requirements;
        var resources = new List<ImageAnalysisResourceAssessment>
        {
            AssessResource(RamResourceId, hardware.RamGb, requirements.RamGb),
            AssessResource(VramResourceId, hardware.VramGb, requirements.VramGb),
            AssessResource(CpuResourceId, hardware.LogicalProcessorCount, requirements.LogicalProcessorCount),
            AssessResource(DiskResourceId, hardware.FreeDiskGb, requirements.FreeDiskGb)
        };
        var hasCompleteData = resources.All(resource => resource.IsSatisfied.HasValue);

        return new ImageAnalysisBundleAssessment
        {
            Bundle = bundle,
            Resources = resources,
            HasCompleteHardwareData = hasCompleteData,
            IsFullyCompatible = hasCompleteData && resources.All(resource => resource.IsSatisfied == true),
            TotalNormalizedDeficit = resources.Sum(resource => resource.NormalizedDeficit)
        };
    }

    private static ImageAnalysisResourceAssessment AssessResource(
        string resourceId,
        double? actualValue,
        double requiredValue)
    {
        var isKnown = actualValue.HasValue;
        var tolerance = resourceId is RamResourceId or VramResourceId
            ? Math.Max(0.1, requiredValue * 0.01)
            : 0;
        var isSatisfied = isKnown
            ? actualValue!.Value + tolerance >= requiredValue
            : (bool?)null;
        var deficit = isKnown && requiredValue > 0 && isSatisfied == false
            ? Math.Max(0, requiredValue - actualValue!.Value) / requiredValue
            : 0;

        return new ImageAnalysisResourceAssessment
        {
            ResourceId = resourceId,
            ActualValue = actualValue,
            RequiredValue = requiredValue,
            IsSatisfied = isSatisfied,
            NormalizedDeficit = deficit
        };
    }
}
