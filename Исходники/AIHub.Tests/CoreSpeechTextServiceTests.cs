using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class CoreSpeechTextServiceTests
{
    [TestMethod]
    public void Compose_PreservesSegmentOffsetsAndNaturalPause()
    {
        var composition = CoreSpeechTextService.Compose(
        [
            new CoreSpeechSegment("coreThought", "Уточняю задачу"),
            new CoreSpeechSegment("question", "Что требуется выбрать?")
        ]);

        Assert.AreEqual("Уточняю задачу. Что требуется выбрать?", composition.Text);
        Assert.AreEqual(0, composition.Segments[0].Start);
        Assert.AreEqual("Что требуется выбрать?", composition.Text[composition.Segments[1].Start..]);
    }

    [TestMethod]
    public void MapVisibleCharacters_RevealsSegmentsIndependently()
    {
        var composition = CoreSpeechTextService.Compose(
        [
            new CoreSpeechSegment("coreThought", "Тест"),
            new CoreSpeechSegment("question", "Вопрос?")
        ]);

        var visible = CoreSpeechTextService.MapVisibleCharacters(composition, 8);

        Assert.AreEqual(4, visible["coreThought"]);
        Assert.AreEqual(2, visible["question"]);
    }

    [TestMethod]
    public void NativePositionMapping_HandlesCyrillicAndSurrogatePairs()
    {
        const string text = "Я😀Б";

        Assert.AreEqual(0, CoreSpeechTextService.NativeCharacterPositionToUtf16Index(text, 1));
        Assert.AreEqual(1, CoreSpeechTextService.NativeCharacterPositionToUtf16Index(text, 2));
        Assert.AreEqual(3, CoreSpeechTextService.NativeCharacterPositionToUtf16Index(text, 3));
        Assert.AreEqual(4, CoreSpeechTextService.NativeCharacterPositionToUtf16Index(text, 4));
    }

    [TestMethod]
    public void EstimatedTimeline_IsMonotonicAndFinishesWithFullText()
    {
        const string text = "Первое слово, второе слово и вопрос?";

        var cues = SpeechTimelineBuilder.BuildEstimated(text, 155);

        Assert.IsTrue(cues.Count > 1);
        Assert.AreEqual(text.Length, cues[^1].VisibleCharacters);
        for (var index = 1; index < cues.Count; index++)
        {
            Assert.IsTrue(cues[index].TimeMilliseconds > cues[index - 1].TimeMilliseconds);
            Assert.IsTrue(cues[index].VisibleCharacters >= cues[index - 1].VisibleCharacters);
        }
    }

    [TestMethod]
    public void ShortTextTimeline_RevealsSingleWord()
    {
        const string text = "Готово";

        var cues = SpeechTimelineBuilder.BuildEstimated(text, 155);

        Assert.AreEqual(1, cues.Count);
        Assert.AreEqual(text.Length, cues[0].VisibleCharacters);
        Assert.IsTrue(cues[0].TimeMilliseconds > 0);
    }
}
