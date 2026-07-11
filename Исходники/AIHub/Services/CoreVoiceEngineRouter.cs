using AIHub.Models;

namespace AIHub.Services;

public sealed class CoreVoiceEngineRouter(
    ICoreVoiceEngine espeakEngine,
    ICoreVoiceEngine rhVoiceEngine) : ICoreVoiceEngine
{
    private readonly ICoreVoiceEngine _espeakEngine = espeakEngine;
    private readonly ICoreVoiceEngine _rhVoiceEngine = rhVoiceEngine;

    public bool IsAvailable => _espeakEngine.IsAvailable || _rhVoiceEngine.IsAvailable;

    public bool IsRhVoiceAvailable => _rhVoiceEngine.IsAvailable;

    public Task<CoreSpeechPresentationResult> SpeakAsync(
        CoreSpeechRequest request,
        IProgress<CoreSpeechProgress> progress,
        CancellationToken cancellationToken)
    {
        var engine = string.Equals(
                request.Settings.Provider,
                CoreVoiceSettings.RhVoiceProvider,
                StringComparison.OrdinalIgnoreCase)
            && _rhVoiceEngine.IsAvailable
                ? _rhVoiceEngine
                : _espeakEngine;
        return engine.SpeakAsync(request, progress, cancellationToken);
    }

    public void Cancel()
    {
        _espeakEngine.Cancel();
        _rhVoiceEngine.Cancel();
    }

    public void Dispose()
    {
        _espeakEngine.Dispose();
        _rhVoiceEngine.Dispose();
    }
}
