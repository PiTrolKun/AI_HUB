using AIHub.Models;

namespace AIHub.Services;

public interface ISingleImageLiteraryPipeline : IDisposable
{
    string PipelineId { get; }

    Task PrepareAsync(
        StorageSettings storageSettings,
        ImageAnalysisLiterarySession? session,
        bool prepareCoreConcurrently,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        CancellationToken cancellationToken);

    Task<ImageAnalysisLiteraryResult> CreateAsync(
        ImageAnalysisFilePassport passport,
        ImageAnalysisLiterarySettings settings,
        StorageSettings storageSettings,
        ImageAnalysisLiterarySession session,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        IProgress<ModelStreamChunk>? streamProgress,
        Action<ImageAnalysisPipelineCheckpoint>? checkpointReady,
        CancellationToken cancellationToken);

    Task<string> ReviseAsync(
        ImageAnalysisLiterarySession session,
        string changeRequest,
        StorageSettings storageSettings,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        IProgress<ModelStreamChunk>? streamProgress,
        CancellationToken cancellationToken);

    void Stop();
}

public sealed class LegacySingleImageLiteraryPipeline : ISingleImageLiteraryPipeline
{
    private readonly ImageAnalysisLiteraryService _service;

    public LegacySingleImageLiteraryPipeline(ImageAnalysisLiteraryService service)
    {
        _service = service;
    }

    public string PipelineId => ImageAnalysisPipelineIds.Legacy;

    public Task PrepareAsync(
        StorageSettings storageSettings,
        ImageAnalysisLiterarySession? session,
        bool prepareCoreConcurrently,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        CancellationToken cancellationToken) =>
        _service.PrepareAsync(
            storageSettings,
            prepareCoreConcurrently,
            log,
            progress,
            cancellationToken);

    public Task<ImageAnalysisLiteraryResult> CreateAsync(
        ImageAnalysisFilePassport passport,
        ImageAnalysisLiterarySettings settings,
        StorageSettings storageSettings,
        ImageAnalysisLiterarySession session,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        IProgress<ModelStreamChunk>? streamProgress,
        Action<ImageAnalysisPipelineCheckpoint>? checkpointReady,
        CancellationToken cancellationToken) =>
        _service.CreateAsync(
            passport,
            settings,
            storageSettings,
            session.VisualReport,
            log,
            progress,
            streamProgress,
            report => checkpointReady?.Invoke(new ImageAnalysisPipelineCheckpoint(report, [])),
            cancellationToken);

    public Task<string> ReviseAsync(
        ImageAnalysisLiterarySession session,
        string changeRequest,
        StorageSettings storageSettings,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        IProgress<ModelStreamChunk>? streamProgress,
        CancellationToken cancellationToken) =>
        _service.ReviseAsync(
            session,
            changeRequest,
            storageSettings,
            log,
            progress,
            streamProgress,
            cancellationToken);

    public void Stop() => _service.Stop();

    public void Dispose() => _service.Dispose();
}

public interface IOmniSpeechPipeline
{
    bool IsOmniReady { get; }

    Task<OmniSpeechGenerationResult> SpeakAsync(
        string text,
        string speaker,
        int volume,
        int ratePercent,
        IProgress<OmniSpeechProgress>? progress,
        CancellationToken cancellationToken);
}

public interface IHeavyResourceMonitoringPipeline
{
    Task<ImageAnalysisHeavyResourceStatus> CaptureResourceStatusAsync(
        CancellationToken cancellationToken);
}

public sealed record OmniSpeechProgress(string Stage);

public sealed record OmniSpeechGenerationResult(
    bool Completed,
    string AudioPath,
    long GenerationMilliseconds,
    long TimeToFirstAudioMilliseconds,
    string Error = "");
