using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ImageAnalysisRuntimePreparationPolicyTests
{
    [TestMethod]
    public void ModelStartDelay_GivesKokoroAHeadStart()
    {
        Assert.AreEqual(TimeSpan.FromSeconds(2), ImageAnalysisRuntimePreparationPolicy.ModelStartDelay);
    }

    [TestMethod]
    [DataRow(16d)]
    [DataRow(32d)]
    [DataRow(48d)]
    [DataRow(64d)]
    [DataRow(128d)]
    public void ShouldPrepareCoreConcurrently_WhenRamHasSafeReserve(double ramTotalGb)
    {
        var passport = new ComputerPassport { RamTotalGb = ramTotalGb };

        Assert.IsTrue(ImageAnalysisRuntimePreparationPolicy.ShouldPrepareCoreConcurrently(passport));
    }

    [TestMethod]
    [DataRow(0d)]
    [DataRow(8d)]
    [DataRow(15.99d)]
    public void ShouldNotPrepareCoreConcurrently_WhenRamIsLimited(double ramTotalGb)
    {
        var passport = new ComputerPassport { RamTotalGb = ramTotalGb };

        Assert.IsFalse(ImageAnalysisRuntimePreparationPolicy.ShouldPrepareCoreConcurrently(passport));
    }

    [TestMethod]
    public void ShouldNotPrepareCoreConcurrently_WhenPassportIsUnknown()
    {
        Assert.IsFalse(ImageAnalysisRuntimePreparationPolicy.ShouldPrepareCoreConcurrently(null));
    }
}
