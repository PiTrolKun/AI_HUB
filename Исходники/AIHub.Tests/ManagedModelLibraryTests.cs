using System.Security.Cryptography;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ManagedModelLibraryTests
{
    [TestMethod]
    public void Store_PersistsCardAndMergesConsumersWithoutDuplicates()
    {
        var root = CreateRoot();
        try
        {
            var store = new ManagedModelLibraryStore(root);
            var card = CreateCard(Path.Combine(root, "models"));
            card.Consumers.Add(new ManagedModelConsumer { Id = "sandbox", DisplayName = "Песочница" });
            store.Upsert(card);

            var update = CreateCard(Path.Combine(root, "models"));
            update.Consumers.Add(new ManagedModelConsumer { Id = "sandbox", DisplayName = "Sandbox" });
            update.Consumers.Add(new ManagedModelConsumer { Id = "scenario", DisplayName = "Сценарий" });
            store.Upsert(update);

            var loaded = store.Load(card.ModelArtifactId);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("org/example", loaded.RepositoryId);
            Assert.AreEqual(2, loaded.Consumers.Count);
            Assert.IsTrue(loaded.Consumers.Any(item => item.Id == "scenario"));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void Store_CorruptEntryDoesNotHideValidEntries()
    {
        var root = CreateRoot();
        try
        {
            var store = new ManagedModelLibraryStore(root);
            store.Upsert(CreateCard(Path.Combine(root, "models")));
            var entries = Path.Combine(root, "Entries");
            File.WriteAllText(Path.Combine(entries, "broken.json"), "{broken");

            var loaded = store.LoadAll();

            Assert.HasCount(1, loaded);
            Assert.AreEqual("test-artifact", loaded[0].ModelArtifactId);
            Assert.IsTrue(File.Exists(Path.Combine(root, "events.jsonl")));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void RegisterDynamicArtifact_PreservesSearchProvenance()
    {
        var root = CreateRoot();
        try
        {
            var store = new ManagedModelLibraryStore(root);
            var card = CreateCard(Path.Combine(root, "models"));
            card.ModelArtifactId = string.Empty;

            var registered = store.RegisterDynamicArtifact(
                card,
                "sandbox",
                "session-42",
                "vision, Q4_K_M, <= 12 GB",
                "closest verified candidate",
                "0.0.86-dev");

            var loaded = store.Load(registered.ModelArtifactId);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(ManagedModelOrigins.Sandbox, loaded.Origin);
            Assert.AreEqual(ManagedModelStatuses.AwaitingConfirmation, loaded.Status);
            Assert.AreEqual("session-42", loaded.Discovery.SessionId);
            Assert.AreEqual("vision, Q4_K_M, <= 12 GB", loaded.Discovery.SearchSnapshot);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void StableId_DistinguishesDifferentQuantizations()
    {
        var first = CreateCard("C:\\models");
        first.ModelArtifactId = string.Empty;
        var second = CreateCard("C:\\models");
        second.ModelArtifactId = string.Empty;
        second.Files[0].RelativePath = "model-Q8_0.gguf";
        second.Files[0].Sha256 = new string('b', 64);

        Assert.AreNotEqual(
            ManagedModelLibraryStore.CreateStableId(first),
            ManagedModelLibraryStore.CreateStableId(second));
    }

    internal static ManagedModelArtifactCard CreateCard(string modelsRoot, byte[]? payload = null)
    {
        payload ??= [1, 2, 3, 4];
        return new ManagedModelArtifactCard
        {
            ModelArtifactId = "test-artifact",
            Family = "Example",
            DisplayName = "Example Q4_K_M",
            Role = ManagedModelRoles.Executor,
            Provider = "Hugging Face",
            RepositoryId = "org/example",
            Revision = "0123456789abcdef",
            Format = "GGUF",
            Quantization = "Q4_K_M",
            License = "MIT",
            IsManaged = true,
            CanRemoveFiles = true,
            ModelsRoot = modelsRoot,
            InstallDirectory = Path.Combine(modelsRoot, "Executors", "Example"),
            Files =
            [
                new ManagedModelArtifactFile
                {
                    RelativePath = "model-Q4_K_M.gguf",
                    SourceUrl = "https://unit.test/model-Q4_K_M.gguf",
                    SizeBytes = payload.Length,
                    Sha256 = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(),
                    Purpose = "main_model"
                }
            ]
        };
    }

    internal static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "aihub-model-library-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    internal static void DeleteRoot(string root)
    {
        var full = Path.GetFullPath(root);
        var temp = Path.GetFullPath(Path.GetTempPath());
        if (full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) && Directory.Exists(full))
        {
            Directory.Delete(full, recursive: true);
        }
    }
}
