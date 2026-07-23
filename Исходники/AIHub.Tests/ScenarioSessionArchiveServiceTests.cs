using AIHub.Models;
using AIHub.Services;
using System.Text.Json;

namespace AIHub.Tests;

[TestClass]
public sealed class ScenarioSessionArchiveServiceTests
{
    [TestMethod]
    public void SaveLoadAndRename_PreserveCreationAndAdvanceRevision()
    {
        var root = CreateRoot();
        try
        {
            var settings = Settings(root);
            var service = new ScenarioSessionArchiveService();
            var state = new ChoiceScenarioSessionState();
            state.Reset(Step("budget_setup", "Глубина", "budget_4"));
            var session = service.Create(settings, "Режим неопределенности", state.CreateCheckpoint());
            var createdAt = session.CreatedAt;
            var firstRevision = session.Revision;

            service.Rename(settings, session, "Мой проект");
            var loaded = service.Load(settings, session.SessionId);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Мой проект", loaded.CustomTitle);
            Assert.AreEqual(createdAt, loaded.CreatedAt);
            Assert.IsGreaterThan(firstRevision, loaded.Revision);
            Assert.IsGreaterThanOrEqualTo(createdAt, loaded.UpdatedAt);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void Load_UsesPreviousCheckpointWhenCurrentIsCorrupt()
    {
        var root = CreateRoot();
        try
        {
            var settings = Settings(root);
            var service = new ScenarioSessionArchiveService();
            var state = new ChoiceScenarioSessionState();
            state.Reset(Step("budget_setup", "Глубина", "budget_4"));
            var session = service.Create(settings, "Режим неопределенности", state.CreateCheckpoint());
            service.Rename(settings, session, "Резервная версия");
            var directory = service.GetSessionDirectory(settings, session.SessionId);
            File.WriteAllText(Path.Combine(directory, "session.json"), "{broken");

            var loaded = service.Load(settings, session.SessionId);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Режим неопределенности", loaded.DisplayTitle);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void BeginRestoredRun_DetectsUncleanStopAndKeepsSessionId()
    {
        var root = CreateRoot();
        try
        {
            var settings = Settings(root);
            var service = new ScenarioSessionArchiveService();
            var state = new ChoiceScenarioSessionState();
            state.Reset(Step("budget_setup", "Глубина", "budget_4"));
            var session = service.Create(settings, "Режим неопределенности", state.CreateCheckpoint());
            var originalRun = session.CurrentRunId;

            var restoration = service.BeginRestoredRun(settings, session);

            Assert.AreEqual(session.SessionId, restoration.SessionId);
            Assert.AreNotEqual(originalRun, restoration.RunId);
            Assert.AreEqual(ResumableSessionStopKinds.Crash, restoration.PreviousStopKind);
            Assert.IsTrue(restoration.LostUncommittedTurn);
            Assert.AreEqual(1, session.ResumeCount);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void LoadAll_ExposesUnreadableArchiveWithoutDeletingIt()
    {
        var root = CreateRoot();
        try
        {
            var settings = Settings(root);
            var service = new ScenarioSessionArchiveService();
            var state = new ChoiceScenarioSessionState();
            state.Reset(Step("budget_setup", "Глубина", "budget_4"));
            var session = service.Create(settings, "Режим неопределенности", state.CreateCheckpoint());
            service.Rename(settings, session, "Создать резервную ревизию");
            var directory = service.GetSessionDirectory(settings, session.SessionId);
            File.WriteAllText(Path.Combine(directory, "session.json"), "{broken-current");
            File.WriteAllText(Path.Combine(directory, "session.previous.json"), "{broken-previous");

            var loaded = service.LoadAll(settings).Single();

            Assert.AreEqual(session.SessionId, loaded.SessionId);
            Assert.AreEqual(ResumableSessionStatuses.Unavailable, loaded.Status);
            Assert.IsTrue(Directory.Exists(directory));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void Delete_RemovesSelectedArchiveImmediately()
    {
        var root = CreateRoot();
        try
        {
            var settings = Settings(root);
            var service = new ScenarioSessionArchiveService();
            var state = new ChoiceScenarioSessionState();
            state.Reset(Step("budget_setup", "Глубина", "budget_4"));
            var session = service.Create(settings, "Режим неопределенности", state.CreateCheckpoint());

            service.Delete(settings, [session.SessionId]);

            Assert.IsNull(service.Load(settings, session.SessionId));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void Delete_RejectsSessionIdentifierThatEscapesArchiveRoot()
    {
        var root = CreateRoot();
        try
        {
            var service = new ScenarioSessionArchiveService();

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                service.Delete(Settings(root), ["..\\outside"]));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void SaveLoad_PreservesFileManifestAndInternalPath()
    {
        var root = CreateRoot();
        try
        {
            var settings = Settings(root);
            var service = new ScenarioSessionArchiveService();
            var state = new ChoiceScenarioSessionState();
            state.Reset(Step("file_setup", "Файлы", "files_none"));
            var session = service.Create(settings, "Режим неопределенности", state.CreateCheckpoint());
            session.FileManifest = new SessionFileManifest
            {
                Intent = SessionFileIntentStatuses.Selected,
                Files =
                [
                    new SessionFileReference
                    {
                        Id = "file-id",
                        SourcePath = @"C:\UserData\report.docx",
                        DisplayName = "report.docx",
                        Extension = ".docx",
                        Category = SessionFileCategories.Document,
                        IsAvailable = true
                    }
                ]
            };
            service.Save(settings, session);

            var loaded = service.Load(settings, session.SessionId);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(SessionFileIntentStatuses.Selected, loaded.FileManifest.Intent);
            Assert.AreEqual(@"C:\UserData\report.docx", loaded.FileManifest.Files.Single().SourcePath);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void Deserialize_LegacySessionWithoutFileManifest_CreatesEmptyManifest()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "sessionId": "legacy-session",
              "scenarioId": "uncertainty",
              "scenarioTitle": "Режим неопределенности"
            }
            """;

        var session = JsonSerializer.Deserialize<ResumableScenarioSession>(json);

        Assert.IsNotNull(session);
        Assert.IsNotNull(session.FileManifest);
        Assert.AreEqual(SessionFileIntentStatuses.Unknown, session.FileManifest.Intent);
        Assert.IsEmpty(session.FileManifest.Files);
    }

    private static ChoiceScenarioStep Step(string type, string question, string optionId) => new()
    {
        StepType = type,
        Question = question,
        Options = [new ChoiceScenarioOption { Id = optionId, Title = optionId }]
    };

    private static StorageSettings Settings(string root) => new()
    {
        Results = new StorageCategorySettings
        {
            Locations = [new StorageLocationSettings { Path = root }]
        }
    };

    private static string CreateRoot() =>
        Path.Combine(Path.GetTempPath(), "AIHubTests", Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
