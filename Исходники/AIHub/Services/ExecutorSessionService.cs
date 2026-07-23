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
    private readonly List<ExecutorResultSnapshot> _snapshots = [];
    private DebugModelInfo? _model;
    private string _systemPrompt = string.Empty;
    private string _languageCode = "ru";
    private List<StructuredToolDefinition> _tools = [];
    private StorageSettings? _storageSettings;
    private ISessionEventLog? _sessionLog;
    private ExecutorTurnResult? _lastTurn;
    private string _currentStageId = ExecutorStageIds.TaskDefinition;
    private bool _briefConfirmed;
    private int _snapshotVersion;
    private bool _disposed;

    public ExecutorSessionService(UserContextService userContextService)
    {
        _runtime = new LlamaServerRuntimeService(userContextService);
    }

    public string CurrentStageId => _currentStageId;

    public string LastTurnStatus => _lastTurn?.Status ?? "not_started";

    public bool BriefConfirmed => _briefConfirmed;

    public IReadOnlyList<ExecutorResultSnapshot> Snapshots => _snapshots;

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
        _languageCode = handoff.LanguageCode;
        _storageSettings = storageSettings;
        _sessionLog = sessionLog;
        _currentStageId = ExecutorStageIds.TaskDefinition;
        _lastTurn = null;
        _briefConfirmed = false;
        _snapshotVersion = 0;
        _snapshots.Clear();
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
                _briefConfirmed
                    ? "Use this answer to improve the current result summary, then ask exactly one next useful practical question."
                    : "Use this answer only to improve the technical task definition.",
                "Do not change stages. Do not create a full result unless AI HUB sends a separate result snapshot request.")
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

    public async Task<ExecutorTurnResult> ConfirmBriefAsync(
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        EnsureActive();
        if (_briefConfirmed
            || _currentStageId != ExecutorStageIds.TaskDefinition
            || _lastTurn?.Status != ExecutorTurnStatuses.StageReady
            || _lastTurn.Action != ExecutorTurnActions.ConfirmBrief)
        {
            throw new InvalidOperationException("The executor brief is not ready for confirmation.");
        }

        var checkpoint = BuildStageCheckpoint(_lastTurn);
        _briefConfirmed = true;
        _sessionLog!.Write("executor_brief_confirmed", new
        {
            Stage = _currentStageId,
            Brief = checkpoint
        });
        try
        {
            return await TransitionStageInternalAsync(
                ExecutorStageIds.PracticalClarification,
                "brief_confirmation",
                checkpoint,
                streamProgress,
                cancellationToken);
        }
        catch
        {
            _briefConfirmed = false;
            _sessionLog.Write("executor_brief_confirmation_rolled_back", new
            {
                Stage = _currentStageId
            });
            throw;
        }
    }

    public async Task<ExecutorTurnResult> EnableRequestedToolsAndContinueAsync(
        IReadOnlyCollection<string> requestedTools,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        EnsureActive();
        var allowedNames = requestedTools
            .Where(name => name is "web_search" or "web_research" or "web_read")
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var enabled = ScenarioToolCatalog.CreateDefinitions()
            .Where(tool => allowedNames.Contains(tool.Function.Name))
            .ToList();
        foreach (var tool in enabled)
        {
            if (_tools.All(existing => existing.Function.Name != tool.Function.Name))
            {
                _tools.Add(tool);
            }
        }

        _sessionLog!.Write("executor_tools_enabled", new
        {
            Stage = _currentStageId,
            Requested = requestedTools,
            Enabled = enabled.Select(tool => tool.Function.Name).ToArray()
        });
        if (enabled.Count == 0)
        {
            return CreateSafetyPause(
                "The requested tool is unavailable.",
                "Укажите, как продолжить без недоступного инструмента.",
                "Explain how to continue without the unavailable tool.");
        }

        var messageIndex = _messages.Count;
        _messages.Add(new StructuredChatMessage
        {
            Role = "user",
            Content = string.Join(
                Environment.NewLine,
                "[AI_HUB_TOOLS_ENABLED]",
                $"Current stage: {_currentStageId}",
                $"Enabled safe tools: {string.Join(", ", enabled.Select(tool => tool.Function.Name))}.",
                "Use a tool only when it is genuinely required.",
                "After tool use, update currentResultSummary and return exactly one practical question to the user.")
        });
        try
        {
            return await RunLoopAsync(streamProgress, cancellationToken);
        }
        catch
        {
            if (_messages.Count > messageIndex)
            {
                _messages.RemoveRange(messageIndex, _messages.Count - messageIndex);
            }

            throw;
        }
    }

    private async Task<ExecutorTurnResult> TransitionStageInternalAsync(
        string targetStageId,
        string initiatedBy,
        string? checkpointOverride,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        EnsureActive();

        if (_briefConfirmed is false
            || _currentStageId != ExecutorStageIds.TaskDefinition
            || targetStageId != ExecutorStageIds.PracticalClarification)
        {
            throw new InvalidOperationException("The requested executor stage transition is not allowed.");
        }

        var previousStageId = _currentStageId;
        var checkpoint = checkpointOverride ?? BuildStageCheckpoint(_lastTurn);
        var transitionMessageIndex = _messages.Count;
        var controlMessage = new StructuredChatMessage
        {
            Role = "user",
            Content = string.Join(
                Environment.NewLine,
                "[AI_HUB_STAGE_CHANGE]",
                $"The user confirmed the technical brief. AI HUB moved the session from {previousStageId} to {targetStageId}.",
                $"Previous stage checkpoint: {checkpoint}",
                BuildStageInstruction(targetStageId),
                "Ask the first practical question and create the first compact currentResultSummary.",
                "This is the final persistent stage. Never request another stage change.")
        };
        _messages.Add(controlMessage);
        _currentStageId = targetStageId;
        _sessionLog!.Write("executor_stage_transition", new
        {
            From = previousStageId,
            To = targetStageId,
            Checkpoint = checkpoint,
            InitiatedBy = initiatedBy
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

    public async Task<ExecutorResultSnapshot> CreateResultSnapshotAsync(
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        EnsureActive();
        if (!_briefConfirmed)
        {
            throw new InvalidOperationException("The executor brief has not been confirmed.");
        }

        var model = _model!;
        var sessionLog = _sessionLog!;
        await CompactIfNeededAsync(model, _systemPrompt, sessionLog, streamProgress, cancellationToken);
        var request = new StructuredChatMessage
        {
            Role = "user",
            Content = string.Join(
                Environment.NewLine,
                "[AI_HUB_RESULT_SNAPSHOT_REQUEST]",
                $"Current stage: {_currentStageId}",
                $"Latest compact available result: {_lastTurn?.CurrentResultSummary ?? string.Empty}",
                "Create the best useful full document available right now from all confirmed data and current work.",
                "Include important assumptions, limitations and sources when present.",
                "Do not ask a question, do not end the session and do not describe future work.",
                "Return Markdown only. Start with one level-one heading.")
        };
        var snapshotMessages = _messages
            .Select(message => new StructuredChatMessage
            {
                Role = message.Role,
                Content = message.Content,
                Name = message.Name,
                ToolCallId = message.ToolCallId,
                ToolCalls = message.ToolCalls
            })
            .Append(request)
            .ToList();
        sessionLog.Write("executor_snapshot_requested", new
        {
            Stage = _currentStageId,
            NextVersion = _snapshotVersion + 1
        });
        var response = await _runtime.GenerateExternalWithToolsAsync(
            model,
            BuildSnapshotSystemPrompt(_languageCode),
            snapshotMessages,
            [],
            message => sessionLog.Write("executor_runtime", new { Message = message }),
            streamProgress,
            cancellationToken);
        var markdown = response.Content.Trim();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            throw new InvalidOperationException("Executor returned an empty result snapshot.");
        }

        var version = ++_snapshotVersion;
        var snapshot = new ExecutorResultSnapshot
        {
            Id = $"snapshot_{version}_{Guid.NewGuid():N}",
            Version = version,
            CreatedAt = DateTimeOffset.Now,
            StageId = _currentStageId,
            Title = ExtractSnapshotTitle(markdown, version, _languageCode),
            Markdown = markdown
        };
        _snapshots.Add(snapshot);
        _messages.Add(request);
        _messages.Add(new StructuredChatMessage
        {
            Role = "assistant",
            Content = string.Join(
                Environment.NewLine,
                $"[AI_HUB_RESULT_SNAPSHOT id={snapshot.Id} version={snapshot.Version}]",
                snapshot.Markdown)
        });
        sessionLog.Write("executor_snapshot_saved", snapshot);
        return snapshot;
    }

    private ExecutorTurnResult CreateSafetyPause(
        string reason,
        string russianQuestion,
        string englishQuestion)
    {
        var russian = _languageCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        var turn = new ExecutorTurnResult
        {
            Status = ExecutorTurnStatuses.Working,
            Action = ExecutorTurnActions.AskUser,
            StageId = _currentStageId,
            StageSummary = BuildStageCheckpoint(_lastTurn),
            Thought = russian
                ? "Для продолжения требуется решение пользователя."
                : "A user decision is required before work can continue.",
            Question = russian ? russianQuestion : englishQuestion,
            Options = [],
            AllowCustom = true,
            CurrentResultSummary = _lastTurn?.CurrentResultSummary ?? string.Empty,
            Warnings = [reason]
        };
        _lastTurn = turn;
        _sessionLog?.Write("executor_safety_pause", new
        {
            Stage = _currentStageId,
            Reason = reason
        });
        return turn;
    }

    internal ExecutorTurnResult CreateToolSafetyPause(string reason) =>
        CreateSafetyPause(
            reason,
            "Инструмент не смог завершить запрос. Уточните, как продолжить без него.",
            "The tool could not complete the request. Clarify how to continue without it.");

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
                if (ExecutorResultParser.TryReadTurn(response.Content, out var turn))
                {
                    if (IsTurnAllowedInCurrentStage(turn))
                    {
                        return AcceptTurn(turn, sessionLog, "executor_turn");
                    }
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
                    if (ExecutorResultParser.TryReadTurn(repaired.Content, out turn))
                    {
                        if (IsTurnAllowedInCurrentStage(turn))
                        {
                            return AcceptTurn(turn, sessionLog, "executor_contract_repaired");
                        }
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
        turn.CurrentResultSummary = ExecutorResultSummaryPolicy.Clamp(turn.CurrentResultSummary);
        _lastTurn = turn;
        sessionLog.Write(eventType, turn);
        if (!string.IsNullOrWhiteSpace(turn.CurrentResultSummary))
        {
            sessionLog.Write("executor_result_summary_updated", new
            {
                Stage = _currentStageId,
                Summary = turn.CurrentResultSummary
            });
        }

        return turn;
    }

    private bool IsTurnAllowedInCurrentStage(ExecutorTurnResult turn)
    {
        if (turn.Action == ExecutorTurnActions.ConfirmBrief)
        {
            return !_briefConfirmed
                && _currentStageId == ExecutorStageIds.TaskDefinition
                && turn.Status == ExecutorTurnStatuses.StageReady;
        }

        if (!_briefConfirmed)
        {
            return _currentStageId == ExecutorStageIds.TaskDefinition
                && turn.Status is ExecutorTurnStatuses.Working
                    or ExecutorTurnStatuses.Blocked
                && turn.Action is ExecutorTurnActions.AskUser
                    or ExecutorTurnActions.Blocked;
        }

        if (_currentStageId != ExecutorStageIds.PracticalClarification
            || turn.Status == ExecutorTurnStatuses.StageReady)
        {
            return false;
        }

        if (turn.Action == ExecutorTurnActions.RequestTool)
        {
            return turn.Status == ExecutorTurnStatuses.Working
                && turn.RequestedTools.Count > 0;
        }

        if (turn.Action == ExecutorTurnActions.AskUser)
        {
            return turn.Status == ExecutorTurnStatuses.Working
                && !string.IsNullOrWhiteSpace(turn.CurrentResultSummary);
        }

        return turn.Status == ExecutorTurnStatuses.Blocked
            && turn.Action == ExecutorTurnActions.Blocked;
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
                $"The task brief is {(_briefConfirmed ? "confirmed" : "not confirmed")}.",
                "Preserve the goal, decisions, confirmed data, evidence, sources, tool results, full result snapshots, current result versions, the latest currentResultSummary, unfinished questions and risks.",
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
                $"The task brief is {(_briefConfirmed ? "confirmed" : "not confirmed")}.",
                $"Latest compact available result: {_lastTurn?.CurrentResultSummary ?? string.Empty}",
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
            "The work has exactly two AI HUB controlled stages: task_definition and practical_clarification.",
            "Never change the stage yourself. Work only inside the current stage named by AI HUB.",
            "Only an explicit [AI_HUB_STAGE_CHANGE] message changes task_definition to practical_clarification. practical_clarification is permanent until the user ends the session.",
            "Ask exactly one useful decision at a time. Do not run a silent autonomous content loop and do not decide that enough information has been collected.",
            "When asking, offer 2-6 short options and allow a custom answer. Offer equivalents of Not important, Decide yourself, or Skip only when they are genuinely safe.",
            "Never offer stage names, transition commands, result commands or session commands as answer options.",
            "Actions: ask_user waits for the user; confirm_brief asks AI HUB to show the completed technical brief; request_tool asks for safe web tools; blocked explains why work cannot continue.",
            "Use confirm_brief only with status stage_ready in task_definition. Put the complete actionable task formulation in stageSummary and leave options empty.",
            "After task confirmation, every ordinary response must return working + ask_user, one practical question, and an updated currentResultSummary.",
            $"currentResultSummary is a concise retelling of the useful answer available right now, not a fact inventory. Keep it within {ExecutorResultSummaryPolicy.MaximumCharacters} characters.",
            "Prioritize the present answer, key recommendations, important caveats and gaps that materially affect it. Never write a promise to prepare the answer later.",
            "Use request_tool only after the task is confirmed and list only web_search, web_research or web_read in requestedTools.",
            "Fill missingCriticalInputs and assumptions honestly; do not invent a precise readiness percentage.",
            "Statuses: working asks the next question or requests a tool; stage_ready is only for the first brief confirmation; blocked explains what prevents progress.",
            "The session never ends from your response. A full result is created only by a separate AI HUB snapshot request initiated by the user.",
            "Keep result empty for working and stage_ready. Update stageSummary every turn with a compact factual checkpoint of confirmed information.",
            "Use only tools exposed by the application. Never claim that an unavailable tool was used.",
            "Every non-tool response must follow the JSON response schema. Never return continue_work, present_result, result_ready, final_result, completed, session_ended, or another terminal status.",
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
        builder.AppendLine("Begin with status working and action ask_user. Ask the first broad technical question. Keep currentResultSummary empty until the brief is confirmed.");
        return builder.ToString();
    }

    private static string BuildStageInstruction(string stageId) => stageId switch
    {
        ExecutorStageIds.TaskDefinition =>
            "Current stage task_definition: clarify the real subject, actionable desired outcome, audience, constraints and success criteria. Ask only critical non-repeating technical questions. Keep currentResultSummary empty. When the task is actionable, return status stage_ready and action confirm_brief with the complete brief in stageSummary, no transition options, and no result.",
        ExecutorStageIds.PracticalClarification =>
            $"Current stage practical_clarification: stay in this stage. Improve the answer through practical questions about approach, missing data, preferences, edge cases, risks and validation. Every ordinary turn must return working + ask_user with exactly one useful question and an updated currentResultSummary of at most {ExecutorResultSummaryPolicy.MaximumCharacters} characters. Do not prepare a full document and never declare the work finished.",
        _ => throw new ArgumentOutOfRangeException(nameof(stageId), stageId, "Unknown executor stage.")
    };

    private string BuildContractRepairMessage(string stageId) =>
        string.Join(
            Environment.NewLine,
            "[AI_HUB_CONTRACT_REPAIR]",
            "Your previous response was rejected because its structure or meaning was invalid.",
            BuildStageInstruction(stageId),
            "Return JSON only using working, stage_ready or blocked.",
            "Return one action: ask_user, confirm_brief, request_tool or blocked.",
            _briefConfirmed
                ? $"The task brief is already confirmed. Return working + ask_user and include a useful currentResultSummary of at most {ExecutorResultSummaryPolicy.MaximumCharacters} characters."
                : "The task brief is not confirmed. Use confirm_brief only when task_definition is actionable.",
            "Never put stage transitions or result commands into options.",
            "Never return continue_work, present_result or result_ready.",
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

        var parts = new[] { turn.CurrentResultSummary, turn.Thought, turn.Question, turn.Result }
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

    private static string BuildSnapshotSystemPrompt(string languageCode) =>
        string.Join(
            Environment.NewLine,
            "You are producing an on-demand preliminary document from an active executor session.",
            "Use all confirmed facts, decisions, tool results, the latest currentResultSummary and current work available in the conversation.",
            "Do not ask questions, do not output JSON, do not end the session and do not claim unsupported facts.",
            "If important information is missing, state the limitation and still provide the most useful current document.",
            "Return clean Markdown with one level-one title, useful sections, paragraphs, lists and code blocks when appropriate.",
            "The document may be long. Do not replace it with a plan or a promise.",
            "Creating this document does not complete the session. The executor will continue practical clarification afterwards.",
            $"Document language: {languageCode}.");

    private static string ExtractSnapshotTitle(string markdown, int version, string languageCode)
    {
        var title = markdown
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title[2..].Trim();
        }

        return languageCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
            ? $"Текущий результат {version}"
            : $"Current result {version}";
    }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_model is null || _storageSettings is null || _sessionLog is null)
        {
            throw new InvalidOperationException("Executor session has not been started.");
        }
    }
}
