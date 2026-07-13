using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class LocalModelCatalogToolTests
{
    [TestMethod]
    public async Task Search_ReturnsBoundedCandidatesWithoutSelectingWinner()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var catalogPath = Path.Combine(root, "catalog.json");
            await WriteCatalogAsync(catalogPath, 8);
            var tool = new LocalModelCatalogTool(
                catalogPath,
                () => DateTimeOffset.Parse("2026-07-11T12:00:00Z"),
                ModelHardwareCompatibilityTests.CreatePassport());

            var result = tool.Search("""
                {
                  "directions": ["text_knowledge"],
                  "taskType": "deep_research",
                  "requiredCapabilities": ["reasoning", "knowledge"],
                  "loadLevel": "optimal",
                  "limit": 6
                }
                """);

            Assert.AreEqual("ready", result.Status);
            Assert.AreEqual(6, result.Candidates.Count);
            Assert.IsTrue(result.Candidates.All(candidate => candidate.Directions.Contains("text_knowledge")));
            Assert.IsTrue(result.Candidates.All(candidate => candidate.LoadLevels.Contains("optimal")));
            Assert.IsTrue(result.Candidates.All(candidate => candidate.Hardware.IsCompatible == true));
            var json = JsonSerializer.Serialize(result);
            Assert.IsFalse(json.Contains("recommended", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains("winner", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void Search_MissingCatalogRequestsLiveFallback()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var tool = new LocalModelCatalogTool(Path.Combine(root, "missing.json"));

            var result = tool.Search("""
                { "directions": [], "taskType": "compare", "requiredCapabilities": [], "loadLevel": "any", "limit": 5 }
                """);

            Assert.AreEqual("missing", result.Status);
            Assert.IsTrue(result.RequiresLiveSearch);
            Assert.AreEqual(0, result.Candidates.Count);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task Search_BroadensUnknownDirectionWithoutSelectingWinner()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var catalogPath = Path.Combine(root, "catalog.json");
            await WriteCatalogAsync(catalogPath, 3);
            var tool = new LocalModelCatalogTool(
                catalogPath,
                computerPassport: ModelHardwareCompatibilityTests.CreatePassport());

            var result = tool.Search("""
                {
                  "directions": ["atypical_user_direction"],
                  "taskType": "analysis",
                  "requiredCapabilities": ["reasoning"],
                  "loadLevel": "optimal",
                  "limit": 3
                }
                """);

            Assert.AreEqual("ready", result.Status);
            Assert.AreEqual(3, result.Candidates.Count);
            Assert.IsFalse(result.RequiresLiveSearch);
            Assert.IsTrue(result.Warnings.Any(value => value.Contains("broader", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Candidates.All(candidate => candidate.ParameterCount > 8_000_000_000));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void ScenarioToolCatalog_BuildsIndependentCatalogCommand()
    {
        var call = new StructuredToolCall
        {
            Function = new StructuredToolCallFunction
            {
                Name = "model_catalog_search",
                Arguments = """{"directions":["science_professional"],"taskType":"analysis","requiredCapabilities":["reasoning"],"loadLevel":"extreme","limit":4}"""
            }
        };

        var command = ScenarioToolCatalog.BuildCommand(call);

        StringAssert.StartsWith(command, "model_catalog_search: ");
        StringAssert.Contains(command, "science_professional");
        Assert.IsTrue(ScenarioToolCatalog.CreateDefinitions().Any(tool =>
            tool.Function.Name == "model_catalog_search"));
    }

    [TestMethod]
    public async Task Search_FiltersCurrentCoreLineageWithoutChoosingReplacement()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var catalogPath = Path.Combine(root, "catalog.json");
            await WriteCatalogAsync(catalogPath, 2);
            var store = new HuggingFaceCatalogStore();
            var database = store.Load(catalogPath, DateTimeOffset.UtcNow);
            database.Records[0].Entry.RepoId = "Qwen/Qwen3-Coder-30B";
            database.Records[0].Entry.ParameterCount = 30_000_000_000;
            database.Records[0].Entry.BaseModels = ["Qwen/Qwen3-30B"];
            database.Records[1].Entry.RepoId = "independent/AnalysisModel-20B";
            database.Records[1].Entry.BaseModels = ["mistralai/Mistral-4-20B"];
            await store.SaveAsync(database, catalogPath, CancellationToken.None);
            var tool = new LocalModelCatalogTool(
                catalogPath,
                computerPassport: ModelHardwareCompatibilityTests.CreatePassport());

            var result = tool.Search("""
                {"directions":["text_knowledge"],"taskType":"analysis","requiredCapabilities":["reasoning"],"loadLevel":"optimal","limit":6}
                """, "Qwen3 8B");

            Assert.AreEqual(1, result.LineageRejectedCount);
            Assert.IsFalse(result.Candidates.Any(candidate => candidate.RepoId.StartsWith("Qwen/", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(result.Candidates.Any(candidate => candidate.RepoId == "independent/AnalysisModel-20B"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static async Task WriteCatalogAsync(string path, int count)
    {
        var now = DateTimeOffset.Parse("2026-07-11T10:00:00Z");
        var database = new HuggingFaceCatalogDatabase
        {
            CreatedAtUtc = now,
            LastSuccessfulSyncUtc = now,
            Records = Enumerable.Range(1, count).Select(index => new HuggingFaceCatalogRecord
            {
                Entry = new HuggingFaceCatalogEntry
                {
                    RepoId = $"lab/reasoning-model-{index}-20B",
                    PipelineTag = "text-generation",
                    License = "apache-2.0",
                    ParameterCount = 20_000_000_000,
                    Tags = ["reasoning", "knowledge"],
                    Downloads = 1000 + index,
                    Likes = 100 + index
                },
                CatalogDirections = ["text_knowledge"],
                SeedSlots =
                [
                    new HuggingFaceCatalogSeedSlot
                    {
                        Direction = "text_knowledge",
                        Slot = 4,
                        LoadLevel = "optimal",
                        RepoId = $"lab/reasoning-model-{index}-20B",
                        Role = "general_reasoning"
                    }
                ],
                FirstSeenUtc = now,
                LastSeenUtc = now,
                LastSuccessfulCheckUtc = now,
                IsAvailable = true
            }).ToList()
        };

        await new HuggingFaceCatalogStore().SaveAsync(database, path, CancellationToken.None);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AIHubCatalogToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
