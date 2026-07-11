using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class HuggingFaceCatalogStartupTests
{
    [TestMethod]
    public async Task SynchronizeIfDueAsync_SkipsFreshCatalog()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var now = DateTimeOffset.Parse("2026-07-11T12:00:00Z");
            await WriteCatalogAsync(root, now.AddHours(-2));
            var seedPath = Path.Combine(root, "seed.json");
            File.WriteAllText(seedPath, "{}");
            var calls = 0;
            var service = new HuggingFaceCatalogStartupService(
                seedPath,
                root,
                TimeSpan.FromHours(24),
                () => now,
                (_, _, _, _) =>
                {
                    calls++;
                    return Task.FromResult(new HuggingFaceCatalogSyncResult());
                });

            var result = await service.SynchronizeIfDueAsync(CancellationToken.None);

            Assert.AreEqual("skipped_fresh", result.Status);
            Assert.AreEqual(0, calls);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task SynchronizeIfDueAsync_RefreshesStaleCatalog()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var now = DateTimeOffset.Parse("2026-07-11T12:00:00Z");
            await WriteCatalogAsync(root, now.AddDays(-2));
            var seedPath = Path.Combine(root, "seed.json");
            File.WriteAllText(seedPath, "{}");
            var calls = 0;
            var service = new HuggingFaceCatalogStartupService(
                seedPath,
                root,
                TimeSpan.FromHours(24),
                () => now,
                (_, _, includeRadar, _) =>
                {
                    calls++;
                    Assert.IsTrue(includeRadar);
                    return Task.FromResult(new HuggingFaceCatalogSyncResult
                    {
                        TrackedRepositoryCount = 94,
                        UpdatedCount = 2,
                        AddedCount = 1
                    });
                });

            var result = await service.SynchronizeIfDueAsync(CancellationToken.None);

            Assert.AreEqual("updated", result.Status);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(94, result.TrackedRepositoryCount);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task SynchronizeIfDueAsync_PreservesCatalogWhenNetworkFails()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var now = DateTimeOffset.Parse("2026-07-11T12:00:00Z");
            var previousSync = now.AddDays(-2);
            await WriteCatalogAsync(root, previousSync);
            var seedPath = Path.Combine(root, "seed.json");
            File.WriteAllText(seedPath, "{}");
            var service = new HuggingFaceCatalogStartupService(
                seedPath,
                root,
                TimeSpan.FromHours(24),
                () => now,
                (_, _, _, _) => throw new HttpRequestException("offline"));

            var result = await service.SynchronizeIfDueAsync(CancellationToken.None);
            var stored = new HuggingFaceCatalogStore().Load(Path.Combine(root, "catalog.json"), now);

            Assert.AreEqual("failed_preserved", result.Status);
            Assert.AreEqual(previousSync, stored.LastSuccessfulSyncUtc);
            Assert.AreEqual(1, stored.Records.Count);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static async Task WriteCatalogAsync(string root, DateTimeOffset lastSync)
    {
        var database = new HuggingFaceCatalogDatabase
        {
            CreatedAtUtc = lastSync,
            LastSuccessfulSyncUtc = lastSync,
            Records =
            [
                new HuggingFaceCatalogRecord
                {
                    Entry = new HuggingFaceCatalogEntry { RepoId = "lab/model-20B", ParameterCount = 20_000_000_000 },
                    IsAvailable = true,
                    FirstSeenUtc = lastSync,
                    LastSeenUtc = lastSync
                }
            ]
        };
        await new HuggingFaceCatalogStore().SaveAsync(
            database,
            Path.Combine(root, "catalog.json"),
            CancellationToken.None);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AIHubCatalogStartupTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
