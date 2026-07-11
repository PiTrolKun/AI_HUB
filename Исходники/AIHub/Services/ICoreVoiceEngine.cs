using AIHub.Models;

namespace AIHub.Services;

public interface ICoreVoiceEngine : IDisposable
{
    bool IsAvailable { get; }

    Task<CoreSpeechPresentationResult> SpeakAsync(
        CoreSpeechRequest request,
        IProgress<CoreSpeechProgress> progress,
        CancellationToken cancellationToken);

    void Cancel();
}

public sealed class CoreVoiceException(string errorCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ErrorCode { get; } = errorCode;
}
