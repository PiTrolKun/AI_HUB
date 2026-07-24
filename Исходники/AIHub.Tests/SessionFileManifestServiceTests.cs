using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class SessionFileManifestServiceTests
{
    [TestMethod]
    public void AddFiles_ClassifiesFilesAndSkipsDuplicates()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var document = Path.Combine(root, "report.docx");
            var table = Path.Combine(root, "data.csv");
            File.WriteAllText(document, "document");
            File.WriteAllText(table, "a,b");
            var manifest = new SessionFileManifest();
            var service = new SessionFileManifestService();

            var firstAdded = service.AddFiles(manifest, [document, table]);
            var duplicateAdded = service.AddFiles(manifest, [document]);

            Assert.AreEqual(2, firstAdded);
            Assert.AreEqual(0, duplicateAdded);
            Assert.AreEqual(SessionFileIntentStatuses.Selected, manifest.Intent);
            Assert.AreEqual(SessionFileCategories.Document, manifest.Files[0].Category);
            Assert.AreEqual(SessionFileCategories.Table, manifest.Files[1].Category);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void PromptManifest_DoesNotExposeAbsoluteSourcePath()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "private-notes.txt");
            File.WriteAllText(path, "secret content that must not be read");
            var manifest = new SessionFileManifest();
            var service = new SessionFileManifestService();
            service.AddFiles(manifest, [path]);

            var promptManifest = service.CreatePromptManifest(manifest);
            var json = JsonSerializer.Serialize(promptManifest);

            Assert.IsFalse(json.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains("secret content", StringComparison.Ordinal));
            Assert.IsFalse(promptManifest.ContentAccessAvailable);
            Assert.AreEqual("private-notes.txt", promptManifest.Files.Single().Name);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void ExecutorPromptManifest_EnablesContentAccessWithoutExposingPath()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "executor-input.txt");
            File.WriteAllText(path, "trusted session input");
            var manifest = new SessionFileManifest();
            var service = new SessionFileManifestService();
            service.AddFiles(manifest, [path]);

            var promptManifest = service.CreatePromptManifest(
                manifest,
                contentAccessAvailable: true);
            var json = JsonSerializer.Serialize(promptManifest);

            Assert.IsTrue(promptManifest.ContentAccessAvailable);
            Assert.IsFalse(json.Contains(root, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(json.Contains("trusted session input", StringComparison.Ordinal));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void RefreshAvailability_KeepsMissingCardAndMarksItUnavailable()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "source.pdf");
            File.WriteAllText(path, "pdf");
            var manifest = new SessionFileManifest();
            var service = new SessionFileManifestService();
            service.AddFiles(manifest, [path]);
            File.Delete(path);

            var changed = service.RefreshAvailability(manifest);

            Assert.IsTrue(changed);
            Assert.HasCount(1, manifest.Files);
            Assert.IsFalse(manifest.Files[0].IsAvailable);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void FileCapabilityUpdate_ResolvesInputModalityWithoutReadingContent()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "image.png");
            File.WriteAllText(path, "not actual image content");
            var manifest = new SessionFileManifest();
            var service = new SessionFileManifestService();
            service.AddFiles(manifest, [path]);

            var update = service.CreateCapabilityUpdate(manifest).Single();

            Assert.AreEqual(ChoiceDecisionDimensions.InputModality, update.Dimension);
            Assert.AreEqual(ChoiceDimensionStatuses.Resolved, update.Status);
            CollectionAssert.Contains(update.Values, "file:image");
            Assert.IsTrue(update.Evidence.Contains("contents are unavailable", StringComparison.Ordinal));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void NoFiles_ClearsOldCardsAndMarksInputNotApplicable()
    {
        var manifest = new SessionFileManifest
        {
            Intent = SessionFileIntentStatuses.Selected,
            Files = [new SessionFileReference { Id = "old", DisplayName = "old.txt" }]
        };
        var service = new SessionFileManifestService();

        service.SetNoFilesPlanned(manifest);
        var update = service.CreateCapabilityUpdate(manifest).Single();

        Assert.AreEqual(SessionFileIntentStatuses.None, manifest.Intent);
        Assert.IsEmpty(manifest.Files);
        Assert.AreEqual(ChoiceDimensionStatuses.NotApplicable, update.Status);
    }

    private static string CreateRoot() =>
        Path.Combine(Path.GetTempPath(), "AIHubFileManifestTests", Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
