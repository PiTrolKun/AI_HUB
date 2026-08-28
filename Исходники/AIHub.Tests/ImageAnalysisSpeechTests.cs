using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ImageAnalysisSpeechTests
{
    [TestMethod]
    public void SpeechMode_CyclesThroughAllThreeStates()
    {
        Assert.AreEqual(ImageAnalysisSpeechModes.Kokoro, ImageAnalysisSpeechModes.Next(ImageAnalysisSpeechModes.Off));
        Assert.AreEqual(ImageAnalysisSpeechModes.Programmatic, ImageAnalysisSpeechModes.Next(ImageAnalysisSpeechModes.Kokoro));
        Assert.AreEqual(ImageAnalysisSpeechModes.Off, ImageAnalysisSpeechModes.Next(ImageAnalysisSpeechModes.Programmatic));
        Assert.AreEqual(ImageAnalysisSpeechModes.Off, ImageAnalysisSpeechModes.Normalize("unknown"));
    }

    [TestMethod]
    public void SpeechSettings_KeepIndependentValuesForBothEngines()
    {
        var settings = new ImageAnalysisSpeechSettings
        {
            Mode = ImageAnalysisSpeechModes.Kokoro,
            KokoroVolume = 85,
            KokoroRatePercent = 110,
            ProgrammaticVolume = 55,
            ProgrammaticRatePercent = 90
        };

        Assert.AreEqual(85, settings.GetActiveVolume());
        Assert.AreEqual(110, settings.GetActiveRatePercent());

        settings.Mode = ImageAnalysisSpeechModes.Programmatic;

        Assert.AreEqual(55, settings.GetActiveVolume());
        Assert.AreEqual(90, settings.GetActiveRatePercent());
    }

    [TestMethod]
    public void SpeechSettings_ClampPersistedValuesToSupportedRanges()
    {
        var settings = new ImageAnalysisSpeechSettings
        {
            KokoroVolume = 500,
            KokoroRatePercent = 10,
            ProgrammaticVolume = -1,
            ProgrammaticRatePercent = 500
        };

        Assert.AreEqual(100, settings.KokoroVolume);
        Assert.AreEqual(70, settings.KokoroRatePercent);
        Assert.AreEqual(0, settings.ProgrammaticVolume);
        Assert.AreEqual(160, settings.ProgrammaticRatePercent);
    }

    [TestMethod]
    public void SpeechText_UsesEveryFullFindingAndUncertainty()
    {
        var summary = new ImageAnalysisReviewSummary
        {
            Items =
            [
                "Первый объект виден полностью.",
                "Второй объект находится на заднем плане.",
                "Третья деталь не должна быть обрезана многоточием... внутри исходной фразы."
            ],
            Uncertainties =
            [
                "Точное назначение предмета неизвестно.",
                "Надпись читается не полностью."
            ]
        };

        var segments = ImageAnalysisSpeechTextService.BuildSegments(summary);
        var text = ImageAnalysisSpeechTextService.BuildPlainText(summary);

        Assert.HasCount(5, segments);
        StringAssert.Contains(text, summary.Items[2]);
        StringAssert.Contains(text, summary.Uncertainties[1]);
        Assert.AreEqual("finding-1", segments[0].Id);
        Assert.AreEqual("uncertainty-2", segments[^1].Id);
    }

    [TestMethod]
    public void SpeechText_FingerprintChangesWithUnderlyingSummary()
    {
        var first = new ImageAnalysisReviewSummary { Items = ["Кошка на столе."] };
        var second = new ImageAnalysisReviewSummary { Items = ["Кошка под столом."] };

        Assert.AreNotEqual(
            ImageAnalysisSpeechTextService.CreateFingerprint(first),
            ImageAnalysisSpeechTextService.CreateFingerprint(second));
    }

    [TestMethod]
    [DataRow(ImageAnalysisSpeechModes.Kokoro)]
    [DataRow(ImageAnalysisSpeechModes.Programmatic)]
    public void SummaryReveal_WaitsForEnabledSpeech(string mode)
    {
        var summary = new ImageAnalysisReviewSummary { Items = ["Кошка на столе."] };

        Assert.IsTrue(ImageAnalysisSpeechTextService.ShouldDelaySummaryReveal(mode, summary));
    }

    [TestMethod]
    public void SummaryReveal_DoesNotWaitWhenSpeechIsOffOrSummaryIsEmpty()
    {
        var summary = new ImageAnalysisReviewSummary { Items = ["Кошка на столе."] };

        Assert.IsFalse(ImageAnalysisSpeechTextService.ShouldDelaySummaryReveal(
            ImageAnalysisSpeechModes.Off,
            summary));
        Assert.IsFalse(ImageAnalysisSpeechTextService.ShouldDelaySummaryReveal(
            ImageAnalysisSpeechModes.Kokoro,
            new ImageAnalysisReviewSummary()));
    }

    [TestMethod]
    public void MemoryPolicy_UsesAvailableMemoryWithoutSubtractingResidentModelsAgain()
    {
        const long gigabyte = 1_000_000_000L;
        var decision = ImageAnalysisSpeechMemoryPolicy.Evaluate(
            availableBytes: 8 * gigabyte,
            totalBytes: 16 * gigabyte,
            expectedRuntimeBytes: 2_600_000_000L,
            pendingAllocationBytes: gigabyte);

        Assert.IsTrue(decision.HasEnoughMemory);
        Assert.AreEqual(5_200_000_000L, decision.RequiredBytes);
        Assert.AreEqual(8 * gigabyte, decision.AvailableBytes);
    }

    [TestMethod]
    public void MemoryPolicy_RejectsWarmupWhenSafetyReserveWouldBeConsumed()
    {
        const long gigabyte = 1_000_000_000L;
        var decision = ImageAnalysisSpeechMemoryPolicy.Evaluate(
            availableBytes: 4 * gigabyte,
            totalBytes: 16 * gigabyte,
            expectedRuntimeBytes: 2_600_000_000L,
            pendingAllocationBytes: gigabyte);

        Assert.IsFalse(decision.HasEnoughMemory);
        Assert.AreEqual(1_600_000_000L, decision.SafetyReserveBytes);
    }

    [TestMethod]
    public void Catalog_SelectsOnlyTheActiveLanguageVoice()
    {
        Assert.AreEqual(
            ManagedModelCatalog.KokoroRussianArtifactId,
            ManagedModelCatalog.ResolveKokoroArtifactId("ru-RU"));
        Assert.AreEqual(
            ManagedModelCatalog.KokoroEnglishArtifactId,
            ManagedModelCatalog.ResolveKokoroArtifactId("en-US"));
    }

    [TestMethod]
    public void Catalog_KokoroCardsArePinnedToExactArtifacts()
    {
        var english = ManagedModelCatalog.CreateKokoroEnglish("C:\\Models");
        var russian = ManagedModelCatalog.CreateKokoroRussian("C:\\Models");

        Assert.AreEqual(ManagedModelRoles.Speech, english.Role);
        Assert.AreEqual(ManagedModelRoles.Speech, russian.Role);
        Assert.AreEqual(3, english.Files.Count);
        Assert.AreEqual(54, russian.Files.Count);
        Assert.IsTrue(english.Files.All(file => file.Sha256.Length == 64 && file.SizeBytes > 0));
        Assert.IsTrue(russian.Files.All(file => file.Sha256.Length == 64 && file.SizeBytes > 0));
        Assert.AreEqual(russian.Files.Count, russian.Files.Select(file => file.RelativePath).Distinct(StringComparer.Ordinal).Count());
        StringAssert.Contains(english.Files.Single(file => file.RelativePath.Contains('/')).SourceUrl, "voices/af_heart.pt");
        StringAssert.Contains(russian.Files.Single(file => file.RelativePath.EndsWith("sveta.pt", StringComparison.Ordinal)).SourceUrl, "voices/sveta.pt");
        Assert.IsTrue(russian.Files.Any(file => file.RelativePath == "espeak-data/ru_dict"));
        Assert.IsTrue(russian.Files.Any(file => file.RelativePath == "ruaccent/nn/nn_omograph/turbo3.1/model.onnx"));
    }
}
