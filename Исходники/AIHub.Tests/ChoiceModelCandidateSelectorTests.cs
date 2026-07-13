using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ChoiceModelCandidateSelectorTests
{
    [TestMethod]
    public void IsVerifiedChoice_AcceptsExactRepositoryOrFileFromEvidence()
    {
        var evidence = """
            Hugging Face model search:
            - developer-a/ReasoningModel-12B-GGUF
              file: ReasoningModel-12B-Q4_K_M.gguf; size: 7 GB
            - new-lab/KnowledgeModel-20B-GGUF
            """;

        Assert.IsTrue(ChoiceModelCandidateSelector.IsVerifiedChoice("new-lab/KnowledgeModel-20B-GGUF", [evidence]));
        Assert.IsTrue(ChoiceModelCandidateSelector.IsVerifiedChoice("ReasoningModel-12B-Q4_K_M.gguf", [evidence]));
    }

    [TestMethod]
    public void IsVerifiedChoice_RejectsProgramInventedCandidate()
    {
        var evidence = "- developer-a/ReasoningModel-12B-GGUF";

        Assert.IsFalse(ChoiceModelCandidateSelector.IsVerifiedChoice("preferred-vendor/OtherModel-14B-GGUF", [evidence]));
    }

    [TestMethod]
    public void IsVerifiedChoice_AcceptsOnlyRunnableInstalledInventoryExecutor()
    {
        var runnable = """
            Capability inventory:
            - executor: installed; installed=True; runnable=True; format=gguf
              name: local/VerifiedModel-20B-GGUF
              path: X:\Models\verified.gguf
            """;
        var broken = """
            Capability inventory:
            - executor: runtime_incompatible; installed=True; runnable=False; format=gguf
              name: local/BrokenModel-31B-GGUF
            """;

        Assert.IsTrue(ChoiceModelCandidateSelector.IsVerifiedChoice(
            "local/VerifiedModel-20B-GGUF",
            [runnable]));
        Assert.IsFalse(ChoiceModelCandidateSelector.IsVerifiedChoice(
            "local/BrokenModel-31B-GGUF",
            [broken]));
        Assert.IsTrue(ChoiceModelCandidateSelector.IsRunnableInstalledInventoryChoice(
            "local/VerifiedModel-20B-GGUF",
            [runnable]));
        Assert.IsFalse(ChoiceModelCandidateSelector.IsRunnableInstalledInventoryChoice(
            "local/BrokenModel-31B-GGUF",
            [broken]));
    }

    [TestMethod]
    public void TryGetVerifiedParameterCount_ReadsLocalCatalogEvidence()
    {
        var evidence = """
            model_catalog_search:
            - lab/ReasoningModel
              pipeline: text-generation; license: apache-2.0; parameters: 21511953984; context: 32768
              evidence: direction: text_knowledge
            """;

        Assert.IsTrue(ChoiceModelCandidateSelector.TryGetVerifiedParameterCount(
            "lab/ReasoningModel",
            [evidence],
            out var parameterCount));
        Assert.AreEqual(21_511_953_984, parameterCount);
    }

    [TestMethod]
    public void TryGetCatalogCandidate_ReadsLineageAndHardwareEvidence()
    {
        var evidence = """
            model_catalog_search:
            - lab/ReasoningModel-20B
              pipeline: text-generation; license: apache-2.0; parameters: 21511953984; context: 32768
              lineage: base_models=Qwen/Qwen3.5-20B; model_type=qwen3_5
              directions: text_knowledge; roles: general_reasoning
              load: optimal; source: curated_seed
              hardware: status=gpu_fit; compatible=True; estimated_q4_runtime_gb=12.03; ram_gb=128; vram_gb=24
              evidence: direction: text_knowledge
            """;

        Assert.IsTrue(ChoiceModelCandidateSelector.TryGetCatalogCandidate(
            "lab/ReasoningModel-20B",
            [evidence],
            out var candidate));
        Assert.AreEqual(21_511_953_984, candidate.ParameterCount);
        CollectionAssert.AreEqual(new[] { "Qwen/Qwen3.5-20B" }, candidate.BaseModels);
        Assert.AreEqual("gpu_fit", candidate.Hardware.Status);
        Assert.IsTrue(candidate.Hardware.IsCompatible);
    }
}
