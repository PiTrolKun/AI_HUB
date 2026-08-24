using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ImageAnalysisLiteraryScenarioTests
{
    [TestMethod]
    public void VisionPrompt_IsEnglishAndKeepsTheVisualReportInternal()
    {
        var prompt = ImageAnalysisLiteraryPromptBuilder.BuildVisionPrompt(
            new ImageAnalysisLiterarySettings());

        StringAssert.Contains(prompt, "Answer only in English");
        StringAssert.Contains(prompt, "MAIN SUBJECTS");
        Assert.IsFalse(prompt.Any(character => character is >= '\u0400' and <= '\u04FF'));
    }

    [TestMethod]
    public void InitialPrompt_ContainsIndependentSettingsAndVisualEvidence()
    {
        var settings = new ImageAnalysisLiterarySettings
        {
            Accuracy = ImageAnalysisAccuracyModes.Strict,
            Style = ImageAnalysisLiteraryStyles.Dramatic,
            Length = ImageAnalysisTextLengths.Brief,
            Form = ImageAnalysisTextForms.Continuous,
            Wishes = "Не описывать фон подробно"
        };

        var prompt = ImageAnalysisLiteraryPromptBuilder.BuildInitialUserPrompt(
            settings,
            "На переднем плане видна чёрная кошка рядом со свечами.");

        StringAssert.Contains(prompt, "строго придерживаться");
        StringAssert.Contains(prompt, "драматический");
        StringAssert.Contains(prompt, "1–2");
        StringAssert.Contains(prompt, "без отдельного заголовка");
        StringAssert.Contains(prompt, settings.Wishes);
        StringAssert.Contains(prompt, "чёрная кошка");
    }

    [TestMethod]
    public void RevisionPrompt_RequestsACompleteNewVersion()
    {
        var prompt = ImageAnalysisLiteraryPromptBuilder.BuildRevisionUserPrompt(
            new ImageAnalysisLiterarySettings(),
            "Видна горная долина и озеро.",
            "Тихое озеро лежит между горами.",
            "Сделай начало тревожнее.");

        StringAssert.Contains(prompt, "Сделай начало тревожнее");
        StringAssert.Contains(prompt, "Тихое озеро");
        StringAssert.Contains(prompt, "новую полную версию");
    }

    [TestMethod]
    public void NormalizeModelText_DoesNotExposeAJsonEnvelope()
    {
        var normalized = ImageAnalysisLiteraryService.NormalizeModelText(
            "{\"description\":\"Готовое описание.\",\"debug\":true}");

        Assert.AreEqual("Готовое описание.", normalized);
    }

    [TestMethod]
    public void CoreResultParser_SeparatesDescriptionFromCompactReviewSummary()
    {
        var result = ImageAnalysisCoreResultParser.Parse("""
            ```json
            {
              "description": "**Ночной кот**\n\nЧёрная кошка наблюдает за зелёным дымом.",
              "review_items": [
                "Чёрная кошка — справа",
                "Чаша с зелёным дымом — внизу",
                "Горящие свечи — слева"
              ],
              "uncertainties": ["Назначение чаши неясно"]
            }
            ```
            """);

        StringAssert.Contains(result.Description, "Ночной кот");
        Assert.AreEqual(3, result.Summary.Items.Count);
        Assert.AreEqual("Чёрная кошка — справа", result.Summary.Items[0]);
        Assert.AreEqual(1, result.Summary.Uncertainties.Count);
        Assert.IsFalse(result.Description.Contains("review_items", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CoreResultParser_PlainTextFallbackNeverUsesVisualReportData()
    {
        var result = ImageAnalysisCoreResultParser.Parse("Готовое описание без JSON.");

        Assert.AreEqual("Готовое описание без JSON.", result.Description);
        Assert.AreEqual(0, result.Summary.Items.Count);
        Assert.AreEqual(0, result.Summary.Uncertainties.Count);
    }

    [TestMethod]
    public void CoreResultParser_RejectsAnIncompleteJsonEnvelope()
    {
        Assert.ThrowsExactly<InvalidDataException>(() =>
            ImageAnalysisCoreResultParser.Parse("{\"description\":\"Оборванный текст"));
    }

    [TestMethod]
    public void ObservationExtractor_BuildsCompactItemsWithoutServiceMarkup()
    {
        var observations = ImageAnalysisObservationExtractor.Extract("""
            <think>
            analysis
            1. Главный объект — чёрная кошка справа от кадра.
            - Перед животным виден котёл с ярким зелёным дымом.
            - Слева расположены несколько горящих свечей.
            </think>
            """);

        Assert.AreEqual(3, observations.Count);
        StringAssert.Contains(observations[0], "чёрная кошка");
        StringAssert.Contains(observations[1], "зелёным дымом");
        Assert.IsFalse(observations.Any(item => item.Contains("think", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task SessionStore_PersistsVersionsAndCreatesBackupOnlyOnRequest()
    {
        var root = Path.Combine(Path.GetTempPath(), "AIHubImageAnalysisTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var sourcePath = Path.Combine(root, "source.jpg");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3, 4]);
            var storage = new StorageSettings
            {
                Results = new StorageCategorySettings
                {
                    Locations = [new StorageLocationSettings { Path = root }]
                }
            };
            var session = new ImageAnalysisLiterarySession
            {
                CurrentStep = ImageAnalysisLiterarySteps.Result,
                Status = ImageAnalysisLiteraryStatuses.ResultReady,
                File = new ImageAnalysisFilePassport
                {
                    SourcePath = sourcePath,
                    DisplayName = "source.jpg",
                    Extension = ".jpg",
                    SizeBytes = 4
                },
                Versions =
                [
                    new ImageAnalysisLiteraryVersion
                    {
                        Number = 1,
                        Text = "Литературное описание."
                    }
                ],
                Observations = ["Чёрная кошка", "Зелёный дым"],
                ReviewSummary = new ImageAnalysisReviewSummary
                {
                    Items = ["Чёрная кошка — справа", "Зелёный дым — внизу"],
                    Uncertainties = ["Назначение чаши неясно"]
                },
                Events =
                [
                    new ImageAnalysisEventEntry
                    {
                        Code = ImageAnalysisEventCodes.VisionCompleted,
                        Role = ManagedModelRoles.Vision,
                        Status = ImageAnalysisEventStatuses.Completed
                    }
                ]
            };
            session.SelectedVersionId = session.Versions[0].VersionId;
            var store = new ImageAnalysisSessionStore();

            store.Save(session, storage);
            var loaded = store.Load(session.SessionId, storage);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.Versions.Count);
            Assert.AreEqual(2, loaded.Observations.Count);
            Assert.AreEqual(2, loaded.ReviewSummary.Items.Count);
            Assert.AreEqual(1, loaded.ReviewSummary.Uncertainties.Count);
            Assert.AreEqual(ImageAnalysisEventCodes.VisionCompleted, loaded.Events.Single().Code);
            Assert.IsTrue(string.IsNullOrWhiteSpace(loaded.InternalImageCopyPath));
            Assert.IsFalse(Directory.Exists(Path.Combine(
                store.GetProjectsDirectory(storage),
                session.SessionId,
                "Backup")));

            await store.CreateInternalBackupAsync(session, storage, CancellationToken.None);

            Assert.IsTrue(File.Exists(session.InternalImageCopyPath));
            Assert.IsTrue(File.Exists(session.InternalDescriptionCopyPath));
            StringAssert.Contains(
                await File.ReadAllTextAsync(session.InternalDescriptionCopyPath),
                "Литературное описание.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task SessionStore_LoadsLegacySessionWithoutShowingLegacyObservationsAsSummary()
    {
        var root = Path.Combine(Path.GetTempPath(), "AIHubImageAnalysisLegacyTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var storage = new StorageSettings
            {
                Results = new StorageCategorySettings
                {
                    Locations = [new StorageLocationSettings { Path = root }]
                }
            };
            var store = new ImageAnalysisSessionStore();
            const string sessionId = "legacy-session";
            var directory = Path.Combine(store.GetProjectsDirectory(storage), sessionId);
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(
                Path.Combine(directory, "session.json"),
                """
                {
                  "schemaVersion": 1,
                  "sessionId": "legacy-session",
                  "observations": ["Сырой фрагмент старого отчёта"],
                  "events": [],
                  "versions": [],
                  "exportedFiles": []
                }
                """);

            var loaded = store.Load(sessionId, storage);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.Observations.Count);
            Assert.AreEqual(0, loaded.ReviewSummary.Items.Count);
            Assert.AreEqual(0, loaded.ReviewSummary.Uncertainties.Count);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
