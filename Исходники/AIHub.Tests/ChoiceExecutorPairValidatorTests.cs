using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ChoiceExecutorPairValidatorTests
{
    private const string InventoryEvidence = """
        Capability inventory:
        - executor: installed; installed=True; runnable=True; format=gguf
          name: local/GeneralModel-27B
          path: X:\Models\general.gguf
        """;

    private const string CatalogEvidence = """
        model_catalog_search:
        - lab/ResearchModel-20B
          pipeline: text-generation; license: apache-2.0; parameters: 20000000000; context: 32768
          lineage: base_models=Other/ResearchModel-20B; model_type=research
          hardware: status=gpu_fit; compatible=True; estimated_q4_runtime_gb=12; ram_gb=128; vram_gb=24
        """;

    [TestMethod]
    public void Validate_AcceptsInstalledAndDownloadPairWithEvidence()
    {
        var card = CreateCard();

        var valid = ChoiceExecutorPairValidator.Validate(
            card,
            [InventoryEvidence, CatalogEvidence],
            false,
            UserWorkloadModes.Balanced,
            "Qwen3-8B",
            ModelHardwareCompatibilityTests.CreatePassport(),
            out var error);

        Assert.IsTrue(valid, error);
    }

    [TestMethod]
    public void Validate_RejectsInstalledLabelWithoutRunnableInventoryEvidence()
    {
        var card = CreateCard();

        var valid = ChoiceExecutorPairValidator.Validate(
            card,
            [CatalogEvidence],
            false,
            UserWorkloadModes.Balanced,
            "Qwen3-8B",
            ModelHardwareCompatibilityTests.CreatePassport(),
            out var error);

        Assert.IsFalse(valid);
        StringAssert.Contains(error, "not a runnable installed inventory model");
    }

    private static ChoiceTaskCard CreateCard() => new()
    {
        RecommendedExecutor = "lab/ResearchModel-20B",
        ExecutorCandidates =
        [
            new ChoiceExecutorCandidate
            {
                Model = "local/GeneralModel-27B",
                Status = ChoiceExecutorCandidateStatuses.Installed,
                Role = "general_worker",
                CapabilityClass = ChoiceExecutorPolicy.Above8B,
                Advantage = "Ready now",
                Limitation = "Less specialized",
                Reason = "General fallback"
            },
            new ChoiceExecutorCandidate
            {
                Model = "lab/ResearchModel-20B",
                Status = ChoiceExecutorCandidateStatuses.NotInstalled,
                Role = "specialist_model",
                CapabilityClass = ChoiceExecutorPolicy.Above8B,
                Advantage = "Better research fit",
                Limitation = "Requires download",
                Reason = "Specialized candidate",
                IsRecommended = true
            }
        ]
    };
}
