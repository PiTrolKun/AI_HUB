using AIHub.Models;

namespace AIHub.Services;

public static class ImageAnalysisBundleCatalog
{
    public const string LightId = "light";
    public const string MediumId = "medium";
    public const string HeavyId = "heavy";

    public static IReadOnlyList<ImageAnalysisBundleDefinition> Create() =>
    [
        CreateBundle(
            LightId,
            level: 1,
            titleKey: "ImageAnalysis.Bundle.Light",
            purposeKey: "ImageAnalysis.Bundle.LightPurpose",
            statusKey: "ImageAnalysis.Bundle.InDevelopment",
            visualModel: "SmolVLM2 2.2B Instruct Q4_K_M + Q8 mmproj",
            localizerModel: "Florence-2-base-ft",
            requirements: new ImageAnalysisHardwareRequirements
            {
                RamGb = 16,
                VramGb = 4,
                LogicalProcessorCount = 8,
                FreeDiskGb = 20
            },
            isAvailable: false,
            isCurrentProjectBundle: false),
        CreateBundle(
            MediumId,
            level: 2,
            titleKey: "ImageAnalysis.Bundle.Medium",
            purposeKey: "ImageAnalysis.Bundle.MediumPurpose",
            statusKey: "ImageAnalysis.Bundle.Current",
            visualModel: "Kimi-VL-A3B-Thinking-2506 GGMM Q4_1",
            localizerModel: "Florence-2-large-ft",
            requirements: new ImageAnalysisHardwareRequirements
            {
                RamGb = 32,
                VramGb = 16,
                LogicalProcessorCount = 12,
                FreeDiskGb = 35
            },
            isAvailable: true,
            isCurrentProjectBundle: true),
        CreateHeavyBundle()
    ];

    private static ImageAnalysisBundleDefinition CreateHeavyBundle() => new()
    {
        Id = HeavyId,
        Level = 3,
        TitleKey = "ImageAnalysis.Bundle.Heavy",
        PurposeKey = "ImageAnalysis.Bundle.HeavyPurpose",
        StatusKey = "ImageAnalysis.Bundle.Experimental",
        Components =
        [
            new ImageAnalysisBundleComponent
            {
                RoleKey = "ImageAnalysis.Role.Omni",
                ModelName = "Qwen2.5-Omni-3B BF16 · Thinker",
                PlacementKey = "ImageAnalysis.Placement.Gpu"
            }
        ],
        Requirements = new ImageAnalysisHardwareRequirements
        {
            RamGb = 32,
            VramGb = 24,
            LogicalProcessorCount = 12,
            FreeDiskGb = 16
        },
        IsAvailable = true,
        IsCurrentProjectBundle = false,
        IsPreliminary = true
    };

    private static ImageAnalysisBundleDefinition CreateBundle(
        string id,
        int level,
        string titleKey,
        string purposeKey,
        string statusKey,
        string visualModel,
        string localizerModel,
        ImageAnalysisHardwareRequirements requirements,
        bool isAvailable,
        bool isCurrentProjectBundle) =>
        new()
        {
            Id = id,
            Level = level,
            TitleKey = titleKey,
            PurposeKey = purposeKey,
            StatusKey = statusKey,
            Components =
            [
                new ImageAnalysisBundleComponent
                {
                    RoleKey = "ImageAnalysis.Role.Vision",
                    ModelName = visualModel,
                    PlacementKey = id == MediumId
                        ? "ImageAnalysis.Placement.CpuRam"
                        : id == HeavyId
                        ? "ImageAnalysis.Placement.GpuHybrid"
                        : "ImageAnalysis.Placement.Gpu"
                },
                new ImageAnalysisBundleComponent
                {
                    RoleKey = "ImageAnalysis.Role.Localizer",
                    ModelName = localizerModel,
                    PlacementKey = "ImageAnalysis.Placement.CpuRam"
                },
                new ImageAnalysisBundleComponent
                {
                    RoleKey = "ImageAnalysis.Role.Core",
                    ModelName = "Qwen3 8B Q4_K_M",
                    PlacementKey = "ImageAnalysis.Placement.CpuRam"
                }
            ],
            Requirements = requirements,
            IsAvailable = isAvailable,
            IsCurrentProjectBundle = isCurrentProjectBundle,
            IsPreliminary = true
        };
}
