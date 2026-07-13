using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutorWorkflowService : IDisposable
{
    private readonly ExecutorModelArtifactResolver _resolver = new(new HuggingFaceExecutorArtifactSource());
    private readonly ExecutorModelInstaller _installer = new();
    private readonly UserContextService _userContextService;
    private ExecutorSessionService? _session;
    private ISessionEventLog? _sessionLog;
    private bool _disposed;

    public ExecutorWorkflowService(UserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    public Task<ExecutorModelArtifact> ResolveAsync(
        string requestedModel,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _resolver.ResolveAsync(requestedModel, storageSettings, cancellationToken);
    }

    public async Task<ExecutorModelArtifact> InstallAsync(
        ExecutorModelArtifact artifact,
        StorageSettings storageSettings,
        IProgress<ExecutorDownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var downloaded = await _installer.InstallAsync(artifact, storageSettings, progress, cancellationToken);
        using var runtime = new LlamaServerRuntimeService(_userContextService);
        var model = new DebugModelInfo
        {
            Name = downloaded.RepoId,
            Path = downloaded.InstalledPath,
            SizeBytes = downloaded.SizeBytes,
            Role = "executor",
            Status = "runtime_validation",
            Format = "gguf",
            IsRunnable = false
        };
        try
        {
            await runtime.ProbeModelAsync(model, _ => { }, cancellationToken);
            progress.Report(new ExecutorDownloadProgress(
                downloaded.SizeBytes,
                downloaded.SizeBytes,
                0,
                "installed"));
            return _installer.MarkRuntimeVerified(downloaded);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _installer.MarkRuntimeIncompatible(downloaded, ex);
            throw new InvalidOperationException(
                $"The downloaded model failed the {LlamaBackendPaths.DisplayName} launch check and was not registered as runnable: {ex.Message}",
                ex);
        }
    }

    public async Task<ExecutorTurnResult> ExecuteAsync(
        ExecutorModelArtifact artifact,
        ExecutorHandoffPackage handoff,
        StorageSettings storageSettings,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop("replaced");
        _sessionLog = ScenarioSessionLog.CreateUncertaintyExecutor(storageSettings);
        _session = new ExecutorSessionService(_userContextService);
        return await _session.ExecuteAsync(
            artifact,
            handoff,
            storageSettings,
            _sessionLog,
            streamProgress,
            cancellationToken);
    }

    public Task<ExecutorTurnResult> ContinueAsync(
        string clarificationAnswer,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _session?.ContinueAsync(clarificationAnswer, streamProgress, cancellationToken)
            ?? throw new InvalidOperationException("Executor session is not active.");
    }

    public void Write(string eventType, object? payload = null) => _sessionLog?.Write(eventType, payload);

    public void Stop(string reason)
    {
        _sessionLog?.Write("executor_session_end", new { Reason = reason });
        _sessionLog?.Dispose();
        _sessionLog = null;
        _session?.Dispose();
        _session = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop("disposed");
        _installer.Dispose();
        _disposed = true;
    }
}
