using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ModelHardwareCompatibilityTests
{
    [TestMethod]
    public void Assess_ThirtyTwoBillionParametersFitsHighMemoryPc()
    {
        var result = ModelHardwareCompatibilityService.Assess(
            32_000_000_000,
            CreatePassport(),
            UserWorkloadModes.Balanced);

        Assert.IsTrue(result.IsCompatible);
        Assert.AreEqual("gpu_fit", result.Status);
        Assert.IsTrue(result.EstimatedQ4RuntimeGb is > 17 and < 19);
    }

    [TestMethod]
    public void Assess_HugeModelIsRejectedWhenQ4CannotFitCombinedBudget()
    {
        var result = ModelHardwareCompatibilityService.Assess(
            685_000_000_000,
            CreatePassport(),
            UserWorkloadModes.Extreme);

        Assert.IsFalse(result.IsCompatible);
        Assert.AreEqual("not_fit", result.Status);
    }

    [TestMethod]
    public void Assess_UnknownSizeRemainsUnverified()
    {
        var result = ModelHardwareCompatibilityService.Assess(null, CreatePassport(), UserWorkloadModes.Balanced);

        Assert.IsNull(result.IsCompatible);
        Assert.AreEqual("unknown", result.Status);
    }

    internal static ComputerPassport CreatePassport() => new()
    {
        RamTotalGb = 128,
        Gpus = [new GpuPassport { Name = "Test GPU", VramGb = 24 }],
        Drives = [new DrivePassport { Name = "D:", FreeGb = 1000, TotalGb = 2000 }]
    };
}
