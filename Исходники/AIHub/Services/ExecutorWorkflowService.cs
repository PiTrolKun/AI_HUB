using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutorWorkflowService : IDisposable
{
    private const int MaximumToolRequestsPerTurn = 3;
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

    public event EventHandler<SessionKnowledgeTreeChangedEventArgs>? KnowledgeTreeChanged;

    public event EventHandler? CheckpointChanged;

    public bool BriefConfirmed => _session?.BriefConfirmed ?? false;

    public IReadOnlyList<ExecutorResultSnapshot> Snapshots =>
        _session?.Snapshots ?? [];

    public SessionKnowledgeTreeSnapshot? KnowledgeTreeSnapshot =>
        _session?.KnowledgeTree.GetSnapshot();

    public string ActiveLogPath => _sessionLog?.FilePath ?? string.Empty;

    public ExecutorSessionCheckpoint? CreateCheckpoint() =>
        _session?.CreateCheckpoint();

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
        _sessionLog = ScenarioSessionLog.CreateUncertaintyExecutor(
            storageSettings,
            handoff.ParentCoreSessionId,
            handoff.ParentRunId);
        _session = new ExecutorSessionService(_userContextService);
        _session.KnowledgeTree.Changed += SessionKnowledgeTree_Changed;
        var turn = await _session.ExecuteAsync(
            artifact,
            handoff,
            storageSettings,
            _sessionLog,
            streamProgress,
            cancellationToken);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return turn;
    }

    public ExecutorTurnResult Restore(
        ExecutorSessionCheckpoint checkpoint,
        ExecutorModelArtifact installedArtifact,
        StorageSettings storageSettings,
        SessionRestorationContext restoration)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Stop("replaced_by_restored_session");
        _sessionLog = ScenarioSessionLog.CreateUncertaintyExecutor(
            storageSettings,
            restoration.SessionId,
            restoration.RunId);
        _session = new ExecutorSessionService(_userContextService);
        _session.KnowledgeTree.Changed += SessionKnowledgeTree_Changed;
        var turn = _session.Restore(
            checkpoint,
            installedArtifact,
            storageSettings,
            _sessionLog,
            restoration);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return turn;
    }

    public async Task<ExecutorTurnResult> ContinueAsync(
        string userResponse,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var session = _session ?? throw new InvalidOperationException("Executor session is not active.");
        var turn = await session.ContinueAsync(userResponse, streamProgress, cancellationToken);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return turn;
    }

    public async Task<ExecutorTurnResult> ContinueAndRunAsync(
        string userResponse,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var session = _session ?? throw new InvalidOperationException("Executor session is not active.");
        var turn = await session.ContinueAsync(userResponse, streamProgress, cancellationToken);
        turn = await ResolveRequestedToolsAsync(session, turn, streamProgress, cancellationToken);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return turn;
    }

    public async Task<ExecutorTurnResult> UpdateFileManifestAsync(
        SessionFilePromptManifest fileManifest,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var session = _session ?? throw new InvalidOperationException("Executor session is not active.");
        var turn = await session.UpdateFileManifestAsync(
            fileManifest,
            streamProgress,
            cancellationToken);
        turn = await ResolveRequestedToolsAsync(session, turn, streamProgress, cancellationToken);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return turn;
    }

    public async Task<ExecutorTurnResult> ConfirmBriefAndRunAsync(
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var session = _session ?? throw new InvalidOperationException("Executor session is not active.");
        var turn = await session.ConfirmBriefAsync(streamProgress, cancellationToken);
        turn = await ResolveRequestedToolsAsync(session, turn, streamProgress, cancellationToken);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return turn;
    }

    public async Task<ExecutorResultSnapshot> CreateResultSnapshotAsync(
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var session = _session ?? throw new InvalidOperationException("Executor session is not active.");
        var snapshot = await session.CreateResultSnapshotAsync(streamProgress, cancellationToken);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return snapshot;
    }

    public async Task<ExecutorTurnResult> ContinueAfterCapabilityRequestAsync(
        string capability,
        string resultCode,
        string details,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var session = _session ?? throw new InvalidOperationException("Executor session is not active.");
        var turn = await session.ContinueAfterCapabilityRequestAsync(
            capability,
            resultCode,
            details,
            streamProgress,
            cancellationToken);
        turn = await ResolveRequestedToolsAsync(session, turn, streamProgress, cancellationToken);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return turn;
    }

    public async Task<ExecutorResultSnapshot> CreateFinalResultAsync(
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var session = _session ?? throw new InvalidOperationException("Executor session is not active.");
        var snapshot = await session.CreateFinalResultAsync(streamProgress, cancellationToken);
        CheckpointChanged?.Invoke(this, EventArgs.Empty);
        return snapshot;
    }

    public void Write(string eventType, object? payload = null) => _sessionLog?.Write(eventType, payload);

    private async Task<ExecutorTurnResult> ResolveRequestedToolsAsync(
        ExecutorSessionService session,
        ExecutorTurnResult turn,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        for (var request = 1; request <= MaximumToolRequestsPerTurn; request++)
        {
            if (turn.Action != ExecutorTurnActions.RequestTool)
            {
                return turn;
            }

            _sessionLog?.Write("executor_tool_request", new
            {
                Request = request,
                Stage = turn.StageId,
                Tools = turn.RequestedTools
            });
            try
            {
                turn = await session.EnableRequestedToolsAndContinueAsync(
                    turn.RequestedTools,
                    streamProgress,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _sessionLog?.Write("executor_tool_request_failed", new
                {
                    Request = request,
                    Stage = session.CurrentStageId,
                    ex.Message,
                    ErrorType = ex.GetType().FullName
                });
                return session.CreateToolSafetyPause(
                    $"tool_request_failed:{ex.GetType().Name}");
            }
        }

        return session.CreateToolSafetyPause("tool_request_limit");
    }

    public void Stop(string reason)
    {
        _sessionLog?.Write("executor_session_end", new
        {
            Reason = reason,
            Stage = _session?.CurrentStageId,
            LastStatus = _session?.LastTurnStatus
        });
        _sessionLog?.Dispose();
        _sessionLog = null;
        if (_session is not null)
        {
            _session.KnowledgeTree.Changed -= SessionKnowledgeTree_Changed;
        }

        _session?.Dispose();
        _session = null;
    }

    private void SessionKnowledgeTree_Changed(
        object? sender,
        SessionKnowledgeTreeChangedEventArgs e) =>
        KnowledgeTreeChanged?.Invoke(this, e);

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
