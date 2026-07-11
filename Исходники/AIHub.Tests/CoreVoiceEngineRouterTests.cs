using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class CoreVoiceEngineRouterTests
{
    [TestMethod]
    public async Task SpeakAsync_UsesEspeakByDefault()
    {
        using var espeak = new FakeVoiceEngine(true);
        using var rhVoice = new FakeVoiceEngine(true);
        using var router = new CoreVoiceEngineRouter(espeak, rhVoice);

        await router.SpeakAsync(CreateRequest(CoreVoiceSettings.EspeakProvider), new Progress<CoreSpeechProgress>(), CancellationToken.None);

        Assert.AreEqual(1, espeak.SpeakCalls);
        Assert.AreEqual(0, rhVoice.SpeakCalls);
    }

    [TestMethod]
    public async Task SpeakAsync_UsesRhVoiceWhenSelectedAndAvailable()
    {
        using var espeak = new FakeVoiceEngine(true);
        using var rhVoice = new FakeVoiceEngine(true);
        using var router = new CoreVoiceEngineRouter(espeak, rhVoice);

        await router.SpeakAsync(CreateRequest(CoreVoiceSettings.RhVoiceProvider), new Progress<CoreSpeechProgress>(), CancellationToken.None);

        Assert.AreEqual(0, espeak.SpeakCalls);
        Assert.AreEqual(1, rhVoice.SpeakCalls);
    }

    [TestMethod]
    public async Task SpeakAsync_FallsBackToEspeakWhenRhVoiceIsUnavailable()
    {
        using var espeak = new FakeVoiceEngine(true);
        using var rhVoice = new FakeVoiceEngine(false);
        using var router = new CoreVoiceEngineRouter(espeak, rhVoice);

        await router.SpeakAsync(CreateRequest(CoreVoiceSettings.RhVoiceProvider), new Progress<CoreSpeechProgress>(), CancellationToken.None);

        Assert.AreEqual(1, espeak.SpeakCalls);
        Assert.AreEqual(0, rhVoice.SpeakCalls);
    }

    private static CoreSpeechRequest CreateRequest(string provider) =>
        new(
            [new CoreSpeechSegment("question", "Тестовый вопрос")],
            "ru",
            new CoreVoiceSettings { Provider = provider },
            "router_test");

    private sealed class FakeVoiceEngine(bool isAvailable) : ICoreVoiceEngine
    {
        public bool IsAvailable { get; } = isAvailable;

        public int SpeakCalls { get; private set; }

        public Task<CoreSpeechPresentationResult> SpeakAsync(
            CoreSpeechRequest request,
            IProgress<CoreSpeechProgress> progress,
            CancellationToken cancellationToken)
        {
            SpeakCalls++;
            return Task.FromResult(new CoreSpeechPresentationResult(true, false, true));
        }

        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }
}
