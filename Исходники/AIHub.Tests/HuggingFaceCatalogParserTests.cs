using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class HuggingFaceCatalogParserTests
{
    [TestMethod]
    public void ParseRepositoryIds_RejectsDuplicatesAndInvalidIds()
    {
        const string json = """
            [
              { "id": "Qwen/Qwen3-14B-GGUF" },
              { "id": "Qwen/Qwen3-14B-GGUF" },
              { "id": "brand-new-lab/first-public-model" },
              { "id": "invalid" },
              { "modelId": "missing-id/value" }
            ]
            """;

        var ids = HuggingFaceCatalogParser.ParseRepositoryIds(json);

        CollectionAssert.AreEqual(
            new[] { "Qwen/Qwen3-14B-GGUF", "brand-new-lab/first-public-model" },
            ids.ToArray());
    }

    [TestMethod]
    public void ParseModel_MapsHubMetadataAndPreservesProvenance()
    {
        const string json = """
            {
              "id": "publisher/model-14B-GGUF",
              "author": "publisher",
              "sha": "abc123",
              "createdAt": "2025-01-01T00:00:00Z",
              "lastModified": "2025-02-01T00:00:00Z",
              "downloads": 1200,
              "likes": 42,
              "pipeline_tag": "text-generation",
              "library_name": "transformers",
              "gated": false,
              "private": false,
              "disabled": false,
              "tags": [
                "gguf",
                "en",
                "ru",
                "dataset:owner/unusual-decisions",
                "base_model:quantized:owner/base-14B",
                "license:apache-2.0"
              ],
              "cardData": {
                "license": "apache-2.0",
                "license_link": "https://example.test/license",
                "base_model": "owner/base-14B",
                "base_model_relation": "quantized"
              },
              "config": {
                "architectures": ["ExampleForCausalLM"],
                "model_type": "example"
              },
              "gguf": {
                "total": 14768307200,
                "architecture": "example",
                "context_length": 40960,
                "totalFileSize": 9001752960
              }
            }
            """;
        const string card = """
            ---
            license: apache-2.0
            ---
            # Model title

            This model was fine-tuned for unusual decision-making tasks using a documented dataset.
            """;

        var entry = HuggingFaceCatalogParser.ParseModel(
            json,
            card,
            "https://huggingface.co/api/models/publisher/model-14B-GGUF",
            "https://huggingface.co/publisher/model-14B-GGUF/resolve/abc123/README.md",
            DateTimeOffset.Parse("2026-07-11T00:00:00Z"),
            "raw/models/publisher_model.json",
            "detail-hash",
            "raw/cards/publisher_model.md",
            "card-hash");

        Assert.AreEqual("publisher/model-14B-GGUF", entry.RepoId);
        Assert.AreEqual("abc123", entry.RevisionSha);
        Assert.AreEqual("apache-2.0", entry.License);
        Assert.AreEqual("quantized", entry.BaseModelRelation);
        CollectionAssert.Contains(entry.BaseModels, "owner/base-14B");
        CollectionAssert.Contains(entry.Languages, "ru");
        CollectionAssert.Contains(entry.Datasets, "owner/unusual-decisions");
        Assert.AreEqual(14768307200, entry.ParameterCount);
        Assert.AreEqual(40960, entry.ContextLength);
        StringAssert.Contains(entry.AuthorDescription, "unusual decision-making");
        Assert.AreEqual("huggingface_hub_api", entry.MetadataEvidence);
        Assert.AreEqual("model_card_author_claim", entry.DescriptionEvidence);
        Assert.IsTrue(entry.IsRevisionPinned);
        Assert.AreEqual("detail-hash", entry.RawApiSha256);
        Assert.AreEqual("card-hash", entry.RawModelCardSha256);
        Assert.AreEqual(0, entry.Warnings.Count);
    }

    [TestMethod]
    public void ParseModel_ReportsMissingEvidenceInsteadOfInventingValues()
    {
        const string json = """{ "id": "owner/incomplete-model", "tags": [] }""";

        var entry = HuggingFaceCatalogParser.ParseModel(
            json,
            string.Empty,
            "api",
            "card",
            DateTimeOffset.UtcNow,
            "raw.json",
            "hash",
            string.Empty,
            string.Empty);

        Assert.AreEqual(string.Empty, entry.License);
        Assert.IsNull(entry.ParameterCount);
        Assert.IsTrue(entry.Warnings.Count >= 4);
    }

    [TestMethod]
    public void ParseSearchCandidates_ReadsRadarMetadataWithoutModelCards()
    {
        const string json = """
            [
              {
                "id": "new-lab/model-9b",
                "author": "new-lab",
                "createdAt": "2026-07-01T00:00:00Z",
                "downloads": 1234,
                "likes": 56,
                "trendingScore": 7.5,
                "pipeline_tag": "text-generation",
                "safetensors": { "total": 9000000001 }
              }
            ]
            """;

        var candidate = HuggingFaceCatalogParser.ParseSearchCandidates(json).Single();

        Assert.AreEqual("new-lab/model-9b", candidate.RepoId);
        Assert.AreEqual(9_000_000_001, candidate.ParameterCount);
        Assert.AreEqual(7.5, candidate.TrendingScore);
        Assert.AreEqual("text-generation", candidate.PipelineTag);
    }
}
