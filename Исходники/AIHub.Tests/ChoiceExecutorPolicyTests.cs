using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ChoiceExecutorPolicyTests
{
    [TestMethod]
    public void BalancedMode_RejectsExplicit8BExecutor()
    {
        var card = CreateCard("Qwen3 8B Q4_K_M", ChoiceExecutorPolicy.Above8B);

        Assert.IsFalse(ChoiceExecutorPolicy.Validate(card, UserWorkloadModes.Balanced, false, "Qwen3 8B", out var error));
        StringAssert.Contains(error, "core_fallback");
    }

    [TestMethod]
    public void BalancedMode_AcceptsAbove8BExecutorFromDifferentFamily()
    {
        var card = CreateCard("IndependentModel 14B", ChoiceExecutorPolicy.Above8B);

        Assert.IsTrue(ChoiceExecutorPolicy.Validate(card, UserWorkloadModes.Balanced, false, "Qwen3 8B", out var error), error);
    }

    [TestMethod]
    public void SameCoreFamily_RequiresSignificantlyNewerGeneration()
    {
        var sameGeneration = CreateCard("Qwen3 14B", ChoiceExecutorPolicy.Above8B);
        var newerGeneration = CreateCard("Qwen3.5 14B", ChoiceExecutorPolicy.Above8B);

        Assert.IsFalse(ChoiceExecutorPolicy.Validate(sameGeneration, UserWorkloadModes.Balanced, false, "Qwen3 8B", out var error));
        StringAssert.Contains(error, "newer generation");
        Assert.IsTrue(ChoiceExecutorPolicy.Validate(newerGeneration, UserWorkloadModes.Balanced, false, "Qwen3 8B", out error), error);
    }

    [TestMethod]
    public void CatalogLineage_UsesBaseModelMetadataBeforeRepositoryName()
    {
        var sameGeneration = CreateCard("vendor/SpecializedModel-30B", ChoiceExecutorPolicy.Above8B);
        var sameFamilyMetadata = new ModelCatalogCandidate
        {
            RepoId = sameGeneration.RecommendedExecutor,
            ParameterCount = 30_000_000_000,
            BaseModels = ["Qwen/Qwen3-30B"]
        };
        var differentFamilyMetadata = new ModelCatalogCandidate
        {
            RepoId = "Qwen/Qwen3-Labeled-30B",
            ParameterCount = 30_000_000_000,
            BaseModels = ["mistralai/Mistral-4-30B"]
        };
        var misleadingName = CreateCard(differentFamilyMetadata.RepoId, ChoiceExecutorPolicy.Above8B);

        Assert.IsFalse(ChoiceExecutorPolicy.Validate(
            sameGeneration,
            UserWorkloadModes.Balanced,
            false,
            "Qwen3 8B",
            sameFamilyMetadata,
            out var error));
        StringAssert.Contains(error, "newer generation");
        Assert.IsTrue(ChoiceExecutorPolicy.Validate(
            misleadingName,
            UserWorkloadModes.Balanced,
            false,
            "Qwen3 8B",
            differentFamilyMetadata,
            out error), error);
    }

    [TestMethod]
    public void LightMode_AllowsCoreFallbackOnlyWhenSearchIsUnavailable()
    {
        var card = CreateCard("current_core", ChoiceExecutorPolicy.EightBOrLess);
        card.ExecutorRole = "core_fallback";

        Assert.IsTrue(ChoiceExecutorPolicy.Validate(card, UserWorkloadModes.Light, true, "Qwen3 8B", out var error), error);
        Assert.IsFalse(ChoiceExecutorPolicy.Validate(card, UserWorkloadModes.Light, false, "Qwen3 8B", out _));
    }

    [TestMethod]
    public void LightMode_DoesNotAllowCurrentCoreAsOptimalWorker()
    {
        var card = CreateCard("current_core", ChoiceExecutorPolicy.EightBOrLess);

        Assert.IsFalse(ChoiceExecutorPolicy.Validate(card, UserWorkloadModes.Light, true, "Qwen3 8B", out var error));
        StringAssert.Contains(error, "core_fallback");
    }

    private static ChoiceTaskCard CreateCard(string executor, string capabilityClass) => new()
    {
        ExecutorRole = "general_worker",
        ExecutorCapabilityClass = capabilityClass,
        RecommendedExecutor = executor
    };
}
