using System.IO;
using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutorSessionService : IDisposable
{
    private const int MaxToolRounds = 6;
    private const int MaxContractRepairAttempts = 2;
    private readonly LlamaServerRuntimeService _runtime;
    private readonly ToolGateway _toolGateway = new();
    private readonly List<StructuredChatMessage> _messages = [];
    private DebugModelInfo? _model;
    private string _systemPrompt = string.Empty;
    private List<StructuredToolDefinition> _tools = [];
    private StorageSettings? _storageSettings;
    private ISessionEventLog? _sessionLog;
    private ExecutorTurnResult? _lastTurn;
    private string _currentStageId = ExecutorStageIds.TaskDefinition;
    private bool _disposed;

    public ExecutorSessionService(UserContextService userContextService)
    {
        _runtime = new LlamaServerRuntimeService(userContextService);
    }

    public string CurrentStageId => _currentStageId;

    public string LastTurnStatus => _lastTurn?.Status ?? "not_started";

    public async Task<ExecutorTurnResult> ExecuteAsync(
        ExecutorModelArtifact artifact,
        ExecutorHandoffPackage handoff,
        StorageSettings storageSettings,
        ISessionEventLog sessionLog,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!artifact.IsInstalled || !File.Exists(artifact.InstalledPath))
        {
            throw new FileNotFoundException("The executor model is not installed.", artifact.InstalledPath);
        }

        _model = new DebugModelInfo
        {
            Name = artifact.RepoId,
            Path = artifact.InstalledPath,
            SizeBytes = artifact.SizeBytes,
            Role = "executor",
            Status = "installed",
            Format = "gguf",
            IsRunnable = true
        };
        _systemPrompt = BuildSystemPrompt(handoff);
        _storageSettings = storageSettings;
        _sessionLog = sessionLog;
        _currentStageId = ExecutorStageIds.TaskDefinition;
        _lastTurn = null;
        _messages.Clear();
        _messages.Add(new StructuredChatMessage
        {
            Role = "user",
            Content = BuildUserPrompt(handoff, _currentStageId)
        });
        sessionLog.Write("executor_session_start", new
        {
            Model = artifact.RepoId,
            artifact.FileName,
            artifact.Quantization,
            InitialStage = _currentStageId,
            Handoff = handoff
        });

        _tools = handoff.NeedsWeb
            ? ScenarioToolCatalog.CreateDefinitions()
                .Where(tool => tool.Function.Name is "web_search" or "web_research" or "web_read")
                .ToList()
            : [];
        return await RunLoopAsync(streamProgress, cancellationToken);
    }

    public async Task<ExecutorTurnResult> ContinueAsync(
        string userResponse,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_model is null || _storageSettings is null || _sessionLog is null)
        {
            throw new InvalidOperationException("Executor session has not been started.");
        }

        var responseMessageIndex = _messages.Count;
        _messages.Add(new StructuredChatMessage
        {
            Role = "user",
            Content = string.Join(
                Environment.NewLine,
                "[AI_HUB_USER_RESPONSE]",
                $"Current stage: {_currentStageId}",
                "User selected or entered this response:",
                userResponse.Trim(),
                "Continue only the current stage. Do not change stages unless AI HUB sends an explicit stage change.")
        });
        _sessionLog.Write("executor_user_response", new
        {
            Stage = _currentStageId,
            Response = userResponse
        });
        try
        {
            return await RunLoopAsync(streamProgress, cancellationToken);
        }
        catch
        {
            if (_messages.Count > responseMessageIndex)
            {
                _messages.RemoveRange(responseMessageIndex, _messages.Count - responseMessageIndex);
            }

            _sessionLog.Write("executor_user_response_failed", new
            {
                Stage = _currentStageId,
                Response = userResponse
            });
            throw;
        }
    }

    public async Task<ExecutorTurnResult> TransitionStageAsync(
        string targetStageId,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_model is null || _storageSettings is null || _sessionLog is null)
        {
            throw new InvalidOperationException("Executor session has not been started.");
        }

        if (!ExecutorStageFlow.IsKnown(targetStageId)
            || !ExecutorStageFlow.AreAdjacent(_currentStageId, targetStageId))
        {
            throw new InvalidOperationException("The requested executor stage transition is not allowed.");
        }

        var previousStageId = _currentStageId;
        var checkpoint = BuildStageCheckpoint(_lastTurn);
        var transitionMessageIndex = _messages.Count;
        var controlMessage = new StructuredChatMessage
        {
            Role = "user",
            Content = string.Join(
                Environment.NewLine,
                "[AI_HUB_STAGE_CHANGE]",
                $"The user explicitly moved the session from {previousStageId} to {targetStageId}.",
                $"Previous stage checkpoint: {checkpoint}",
                BuildStageInstruction(targetStageId),
                "A stage change is a program control action, not a request to end the session.")
        };
        _messages.Add(controlMessage);
        _currentStageId = targetStageId;
        _sessionLog.Write("executor_stage_transition", new
        {
            From = previousStageId,
            To = targetStageId,
            Checkpoint = checkpoint,
            InitiatedBy = "user"
        });

        try
        {
            return await RunLoopAsync(streamProgress, cancellationToken);
        }
        catch
        {
            if (_messages.Count > transitionMessageIndex)
            {
                _messages.RemoveRange(transitionMessageIndex, _messages.Count - transitionMessageIndex);
            }

            _currentStageId = previousStageId;
            _sessionLog.Write("executor_stage_transition_failed", new
            {
                From = previousStageId,
                To = targetStageId
            });
            throw;
        }
    }

    private async Task<ExecutorTurnResult> RunLoopAsync(
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        var model = _model ?? throw new InvalidOperationException("Executor model is unavailable.");
        var storageSettings = _storageSettings ?? throw new InvalidOperationException("Executor storage is unavailable.");
        var sessionLog = _sessionLog ?? throw new InvalidOperationException("Executor log is unavailable.");
        for (var round = 1; round <= MaxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await CompactIfNeededAsync(model, _systemPrompt, sessionLog, streamProgress, cancellationToken);
            var response = await _runtime.GenerateExternalWithToolsAsync(
                model,
                _systemPrompt,
                _messages,
                _tools,
                message => sessionLog.Write("executor_runtime", new { Message = message }),
                streamProgress,
                cancellationToken,
                ExecutorJsonContract.CreateResponseFormat());
            sessionLog.Write("executor_model_response", new
            {
                Round = round,
                response.Content,
                response.FinishReason,
                response.ToolCalls
            });
            if (!response.HasToolCalls)
            {
                _messages.Add(new StructuredChatMessage { Role = "assistant", Content = response.Content });
                if (ExecutorResultParser.TryReadTurn(response.Content, out var turn)
                    && IsTurnAllowedInCurrentStage(turn))
                {
                    return AcceptTurn(turn, sessionLog, "executor_turn");
                }

                sessionLog.Write("executor_contract_repair_requested", new { RawResponse = response.Content });
                for (var attempt = 1; attempt <= MaxContractRepairAttempts; attempt++)
                {
                    _messages.Add(new StructuredChatMessage
                    {
                        Role = "user",
                        Content = BuildContractRepairMessage(_currentStageId)
                    });
                    var repaired = await _runtime.GenerateExternalWithToolsAsync(
                        model,
                        _systemPrompt,
                        _messages,
                        [],
                        message => sessionLog.Write("executor_runtime", new { Message = message }),
                        streamProgress,
                        cancellationToken,
                        ExecutorJsonContract.CreateResponseFormat());
                    _messages.Add(new StructuredChatMessage { Role = "assistant", Content = repaired.Content });
                    sessionLog.Write("executor_contract_repair_response", new
                    {
                        Attempt = attempt,
                        repaired.Content,
                        repaired.FinishReason
                    });
                    if (ExecutorResultParser.TryReadTurn(repaired.Content, out turn)
                        && IsTurnAllowedInCurrentStage(turn))
                    {
                        return AcceptTurn(turn, sessionLog, "executor_contract_repaired");
                    }
                }

                sessionLog.Write("executor_contract_repair_failed", new
                {
                    Attempts = MaxContractRepairAttempts,
                    Stage = _currentStageId
                });
                throw new InvalidOperationException("Executor returned an invalid structured turn after repair attempts.");
            }

            _messages.Add(new StructuredChatMessage
            {
                Role = "assistant",
                Content = response.Content,
                ToolCalls = response.ToolCalls
            });
            foreach (var toolCall in response.ToolCalls)
            {
                var command = ScenarioToolCatalog.BuildCommand(toolCall);
                var result = await _toolGateway.ExecuteAsync(command, storageSettings, sessionLog, cancellationToken);
                _messages.Add(new StructuredChatMessage
                {
                    Role = "tool",
                    ToolCallId = toolCall.Id,
                    Name = toolCall.Function.Name,
                    Content = ToolMessageFormatter.WrapToolResult(toolCall.Function.Name, command, result)
                });
            }
        }

        throw new InvalidOperationException("Executor exceeded the allowed tool rounds without producing a result.");
    }

    private ExecutorTurnResult AcceptTurn(
        ExecutorTurnResult turn,
        ISessionEventLog sessionLog,
        string eventType)
    {
        turn.StageId = _currentStageId;
        _lastTurn = turn;
        sessionLog.Write(eventType, turn);
        return turn;
    }

    private bool IsTurnAllowedInCurrentStage(ExecutorTurnResult turn)
    {
        if (turn.Status == ExecutorTurnStatuses.ResultReady)
        {
            return _currentStageId == ExecutorStageIds.ResultAssembly;
        }

        if (turn.Status == ExecutorTurnStatuses.StageReady)
        {
            return ExecutorStageFlow.GetNext(_currentStageId) is not null;
        }

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _runtime.Dispose();
    }

    private async Task CompactIfNeededAsync(
        DebugModelInfo model,
        string systemPrompt,
        ISessionEventLog sessionLog,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        var budget = ExecutorContextBudgetManager.Measure(
            _messages.Select(message => message.Content ?? string.Empty),
            CoreContextRuntimeLimits.CurrentBackendContextLimit);
        sessionLog.Write("executor_context_budget", budget);
        if (!budget.ShouldCompact || _messages.Count < 3)
        {
            return;
        }

        var transcript = string.Join(Environment.NewLine, _messages.Select(message => $"[{message.Role}] {message.Content}"));
        var checkpoint = await _runtime.GenerateExecutorAsync(
            model,
            string.Join(
                Environment.NewLine,
                "Create a compact, factual execution checkpoint.",
                $"The current user-controlled stage is {_currentStageId}; do not change it.",
                "Preserve the goal, decisions, confirmed data, evidence, sources, tool results, current result versions, unfinished actions and risks.",
                "Keep provisional core hypotheses marked as provisional. Do not solve the task again."),
            transcript,
            message => sessionLog.Write("executor_runtime", new { Message = message }),
            streamProgress,
            cancellationToken);
        _messages.Clear();
        _messages.Add(new StructuredChatMessage
        {
            Role = "user",
            Content = string.Join(
                Environment.NewLine,
                "Continue the original session from this verified checkpoint:",
                checkpoint,
                BuildStageInstruction(_currentStageId),
                "The session remains open until the user presses the program's Finish session button.")
        });
        sessionLog.Write("executor_context_checkpoint", new
        {
            Stage = _currentStageId,
            Checkpoint = checkpoint
        });
    }

    private static string BuildSystemPrompt(ExecutorHandoffPackage handoff) =>
        string.Join(
            Environment.NewLine,
            "You are the selected AI executor in the Uncertainty scenario, not the AI HUB core.",
            "The core selected your capability class. It did not discover the user's exact subject and did not prepare a final task.",
            "Treat programFacts as authoritative, userSignals as raw user input, and coreHypotheses only as provisional background that may be wrong.",
            "The work has four user-controlled stages: task_definition, solution_method, data_collection, result_assembly.",
            "Never change the stage yourself. Work only inside the current stage named by AI HUB.",
            "Only an explicit [AI_HUB_STAGE_CHANGE] message changes the stage. You may recommend a transition with status stage_ready.",
            "Ask one decision at a time. Offer 2-6 short options and allow a custom answer. You may ask as many non-repeating steps as genuinely needed.",
            "Statuses: working continues the current stage; stage_ready recommends a user-controlled transition; result_ready delivers a current result version; blocked explains what prevents progress.",
            "result_ready is allowed only in result_assembly and must put the actual completed work in result.",
            "A result is never a status label, an intention, or a promise to start. Put status labels only in status.",
            "The session never ends from your response. After result_ready, accept corrections and produce further versions until the user ends the session in AI HUB.",
            "Keep result empty for working and stage_ready. Update stageSummary every turn with a compact factual checkpoint of confirmed information.",
            "Use only tools exposed by the application. Never claim that an unavailable tool was used.",
            "Every non-tool response must follow the JSON response schema. Never return final_result, completed, session_ended, or another terminal status.",
            "Keep thought to one short user-facing sentence. It is not hidden chain-of-thought.",
            $"Answer language: {handoff.LanguageCode}.");

    private static string BuildUserPrompt(ExecutorHandoffPackage handoff, string stageId)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Handoff package with provenance:");
        builder.AppendLine(JsonSerializer.Serialize(handoff));
        builder.AppendLine();
        builder.AppendLine("Important: Goal and Executor prompt are provisional core hypotheses, not a confirmed user request.");
        builder.AppendLine(BuildStageInstruction(stageId));
        builder.AppendLine("Begin with status working. Ask the first broad question for this stage.");
        return builder.ToString();
    }

    private static string BuildStageInstruction(string stageId) => stageId switch
    {
        ExecutorStageIds.TaskDefinition =>
            "Current stage task_definition: clarify the real subject, desired outcome, audience, constraints and success criteria. Do not choose a solution method yet.",
        ExecutorStageIds.SolutionMethod =>
            "Current stage solution_method: compare suitable approaches, explain meaningful tradeoffs and help the user choose one. Do not gather detailed source data yet.",
        ExecutorStageIds.DataCollection =>
            "Current stage data_collection: identify confirmed information and missing inputs, then collect what the chosen method needs. Do not assemble the final result yet.",
        ExecutorStageIds.ResultAssembly =>
            "Current stage result_assembly: create and revise actual result versions from confirmed data. result_ready must contain the work itself and never closes the session.",
        _ => throw new ArgumentOutOfRangeException(nameof(stageId), stageId, "Unknown executor stage.")
    };

    private static string BuildContractRepairMessage(string stageId) =>
        string.Join(
            Environment.NewLine,
            "[AI_HUB_CONTRACT_REPAIR]",
            "Your previous response was rejected because its structure or meaning was invalid.",
            BuildStageInstruction(stageId),
            "Return JSON only using working, stage_ready, result_ready or blocked.",
            "If status is result_ready, result must contain the actual completed work, not result_ready, final_result, a promise, or a description of future work.",
            "Keep the session open. Do not change stages and do not claim that the session ended.");

    private static string BuildStageCheckpoint(ExecutorTurnResult? turn)
    {
        if (turn is null)
        {
            return "No executor turn has been completed in this stage yet.";
        }

        if (!string.IsNullOrWhiteSpace(turn.StageSummary))
        {
            return Shorten(turn.StageSummary, 1200);
        }

        var parts = new[] { turn.Thought, turn.Question, turn.Result }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim());
        var fallback = string.Join(" ", parts);
        return string.IsNullOrWhiteSpace(fallback)
            ? "The user changed the stage before a summary was available."
            : Shorten(fallback, 1200);
    }

    private static string Shorten(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters
            ? value
            : value[..maximumCharacters] + "...";
}
