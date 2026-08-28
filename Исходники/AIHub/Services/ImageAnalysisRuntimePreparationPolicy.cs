using AIHub.Models;

namespace AIHub.Services;

public static class ImageAnalysisRuntimePreparationPolicy
{
    public const double ParallelPreparationMinimumRamGb = 16;
    public static readonly TimeSpan ModelStartDelay = TimeSpan.FromSeconds(2);

    public static bool ShouldPrepareCoreConcurrently(ComputerPassport? passport) =>
        passport is not null
        && passport.RamTotalGb >= ParallelPreparationMinimumRamGb;
}
