using System.Net;
using System.Text;
using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class HuggingFaceCatalogSyncTests
{
    [TestMethod]
    public void ProjectSeed_ContainsElevenDirectionsAndNineSlotsEach()
    {
        var root = FindProjectRoot(AppContext.BaseDirectory);
        var seed = HuggingFaceCatalogSeedStore.Load(
            Path.Combine(root, "Каталоги", "huggingface-catalog-seed.json"));

        Assert.AreEqual(99, seed.Slots.Count);
        Assert.AreEqual(11, seed.Slots.Select(slot => slot.Direction).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.IsTrue(seed.Slots.GroupBy(slot => slot.Direction).All(group => group.Count() == 9));
        Assert.IsTrue(seed.Slots.Any(slot => slot.IsManualException));
    }

    [TestMethod]
    public async Task SynchronizeAsync_PreservesRecordAndDetectsRevisionUpdate()
    {
        var temporaryRoot = CreateTemporaryDirectory();
        try
        {
            var seedPath = WriteTestSeed(temporaryRoot, includeRadar: false);
            var handler = new CatalogHttpHandler(revisionChanges: true, includeRadarCandidate: false);
            using var client = new HttpClient(handler);
            using var collector = new HuggingFaceCatalogCollector(client);
            var service = new HuggingFaceCatalogSyncService(
                collector,
                utcNow: CreateClock(
                    DateTimeOffset.Parse("2026-07-11T00:00:00Z"),
                    DateTimeOffset.Parse("2026-07-12T00:00:00Z")));

            await service.SynchronizeAsync(seedPath, temporaryRoot, false, CancellationToken.None);
            var second = await service.SynchronizeAsync(seedPath, temporaryRoot, false, CancellationToken.None);
            var database = new HuggingFaceCatalogStore().Load(
                Path.Combine(temporaryRoot, "catalog.json"),
                DateTimeOffset.Parse("2026-07-12T00:00:00Z"));

            Assert.AreEqual(1, database.Records.Count);
            Assert.AreEqual("revision-2", database.Records[0].Entry.RevisionSha);
            Assert.AreEqual("revision-1", database.Records[0].PreviousRevisionSha);
            Assert.AreEqual(1, database.Records[0].RevisionUpdateCount);
            Assert.AreEqual(1, second.UpdatedCount);
            StringAssert.Contains(File.ReadAllText(second.ChangesPath), "revision_changed");
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    [TestMethod]
    public async Task SynchronizeAsync_RadarAcceptsOnlyVerifiedModelsAboveEightBillionParameters()
    {
        var temporaryRoot = CreateTemporaryDirectory();
        try
        {
            var seedPath = WriteTestSeed(temporaryRoot, includeRadar: true);
            var handler = new CatalogHttpHandler(revisionChanges: false, includeRadarCandidate: true);
            using var client = new HttpClient(handler);
            using var collector = new HuggingFaceCatalogCollector(client);
            var service = new HuggingFaceCatalogSyncService(
                collector,
                utcNow: () => DateTimeOffset.Parse("2026-07-11T00:00:00Z"));

            var result = await service.SynchronizeAsync(seedPath, temporaryRoot, true, CancellationToken.None);
            var database = new HuggingFaceCatalogStore().Load(
                Path.Combine(temporaryRoot, "catalog.json"),
                DateTimeOffset.Parse("2026-07-11T00:00:00Z"));

            Assert.IsTrue(database.Records.Any(record => record.Entry.RepoId == "new-lab/large-model" && record.IsRadarDiscovery));
            Assert.IsFalse(database.Records.Any(record => record.Entry.RepoId == "new-lab/small-model"));
            Assert.IsTrue(database.Records.Single(record => record.Entry.RepoId == "new-lab/large-model").IsNewAuthor);
            Assert.AreEqual(1, result.RadarAddedCount);
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    [TestMethod]
    public async Task SynchronizeAsync_AllRepositoriesUnavailablePreservesPreviousCatalog()
    {
        var temporaryRoot = CreateTemporaryDirectory();
        try
        {
            var now = DateTimeOffset.Parse("2026-07-11T00:00:00Z");
            var seedPath = WriteTestSeed(temporaryRoot, includeRadar: false);
            var catalogPath = Path.Combine(temporaryRoot, "catalog.json");
            await new HuggingFaceCatalogStore().SaveAsync(
                new HuggingFaceCatalogDatabase
                {
                    CreatedAtUtc = now.AddDays(-1),
                    LastSuccessfulSyncUtc = now.AddDays(-1),
                    Records =
                    [
                        new HuggingFaceCatalogRecord
                        {
                            Entry = new HuggingFaceCatalogEntry
                            {
                                RepoId = "known-lab/seed-model",
                                RevisionSha = "preserved-revision",
                                ParameterCount = 9_000_000_001
                            },
                            IsAvailable = true,
                            FirstSeenUtc = now.AddDays(-1),
                            LastSeenUtc = now.AddDays(-1)
                        }
                    ]
                },
                catalogPath,
                CancellationToken.None);
            using var client = new HttpClient(new AllUnavailableCatalogHttpHandler());
            using var collector = new HuggingFaceCatalogCollector(client);
            var service = new HuggingFaceCatalogSyncService(collector, utcNow: () => now);

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
                service.SynchronizeAsync(seedPath, temporaryRoot, false, CancellationToken.None));
            var preserved = new HuggingFaceCatalogStore().Load(catalogPath, now);

            Assert.AreEqual("preserved-revision", preserved.Records.Single().Entry.RevisionSha);
            Assert.IsTrue(preserved.Records.Single().IsAvailable);
            Assert.AreEqual(now.AddDays(-1), preserved.LastSuccessfulSyncUtc);
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    private static string WriteTestSeed(string root, bool includeRadar)
    {
        var seed = new HuggingFaceCatalogSeed
        {
            Radar = new HuggingFaceRadarSettings
            {
                LookbackDays = 365,
                QueryLimit = 10,
                MaximumNewEntriesPerSync = 10,
                MinimumParameterCountExclusive = 8_000_000_000,
                MinimumDownloads = 100,
                MinimumLikes = 10,
                AutomaticTrendingRankLimit = 3,
                SupportedPipelineTags = ["text-generation"]
            },
            Slots = Enumerable.Range(1, 9).Select(slot => new HuggingFaceCatalogSeedSlot
            {
                Direction = "test_direction",
                Slot = slot,
                LoadLevel = slot <= 3 ? "light" : slot <= 6 ? "optimal" : "extreme",
                RepoId = "known-lab/seed-model",
                Role = "test",
                IsManualException = false
            }).ToList()
        };
        var path = Path.Combine(root, includeRadar ? "seed-radar.json" : "seed.json");
        File.WriteAllText(path, JsonSerializer.Serialize(seed, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
        return path;
    }

    private static Func<DateTimeOffset> CreateClock(params DateTimeOffset[] values)
    {
        var index = -1;
        return () => values[Math.Min(Interlocked.Increment(ref index), values.Length - 1)];
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AIHubCatalogTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FindProjectRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VERSION")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("AI_HUB project root was not found.");
    }

    private sealed class CatalogHttpHandler(bool revisionChanges, bool includeRadarCandidate) : HttpMessageHandler
    {
        private int _seedDetailRequestCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            if (url.Contains("/api/models?", StringComparison.Ordinal))
            {
                var json = includeRadarCandidate
                    ? """
                      [
                        { "id":"new-lab/large-model", "author":"new-lab", "createdAt":"2026-07-01T00:00:00Z", "downloads":5000, "likes":100, "trendingScore":10, "pipeline_tag":"text-generation", "safetensors":{"total":9000000001} },
                        { "id":"new-lab/small-model", "author":"new-lab", "createdAt":"2026-07-01T00:00:00Z", "downloads":50000, "likes":1000, "trendingScore":20, "pipeline_tag":"text-generation", "safetensors":{"total":7000000000} }
                      ]
                      """
                    : "[]";
                return Task.FromResult(JsonResponse(json));
            }

            if (url.Contains("/api/models/known-lab/seed-model", StringComparison.Ordinal))
            {
                var requestNumber = Interlocked.Increment(ref _seedDetailRequestCount);
                var revision = revisionChanges && requestNumber > 1 ? "revision-2" : "revision-1";
                return Task.FromResult(JsonResponse(ModelJson("known-lab/seed-model", "known-lab", revision, 9_000_000_001)));
            }

            if (url.Contains("/api/models/new-lab/large-model", StringComparison.Ordinal))
            {
                return Task.FromResult(JsonResponse(ModelJson("new-lab/large-model", "new-lab", "large-revision", 9_000_000_001)));
            }

            if (url.EndsWith("/README.md", StringComparison.Ordinal))
            {
                return Task.FromResult(TextResponse("# Test model\n\nA test model card description."));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static string ModelJson(string id, string author, string revision, long parameters) => $$"""
            {
              "id":"{{id}}", "author":"{{author}}", "sha":"{{revision}}",
              "createdAt":"2026-07-01T00:00:00Z", "lastModified":"2026-07-10T00:00:00Z",
              "downloads":1000, "likes":50, "pipeline_tag":"text-generation", "library_name":"transformers",
              "gated":false, "private":false, "disabled":false, "tags":["license:apache-2.0"],
              "cardData":{"license":"apache-2.0", "base_model":"owner/base"},
              "config":{"architectures":["TestForCausalLM"], "model_type":"test"},
              "safetensors":{"total":{{parameters}}}
            }
            """;

        private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        private static HttpResponseMessage TextResponse(string text) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(text, Encoding.UTF8, "text/markdown")
        };
    }

    private sealed class AllUnavailableCatalogHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}
