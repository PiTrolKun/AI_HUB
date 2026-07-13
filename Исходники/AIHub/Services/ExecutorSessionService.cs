using System.IO;
using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutorSessionService : IDisposable
{
    private const int MaxToolRounds = 6;
    private readonly LlamaServerRuntimeService _runtime;
    private readonly ToolGateway _toolGateway = new();
    private readonly List<StructuredChatMessage> _messages = [];
    private DebugModelInfo? _model;
    private string _systemPrompt = string.Empty;
    private List<StructuredToolDefinition> _tools = [];
    private StorageSettings? _storageSettings;
    private ISessionEventLog? _sessionLog;
    private bool _disposed;

    public ExecutorSessionService(UserContextService userContextService)
    {
        _runtime = new LlamaServerRuntimeService(userContextService);
    }

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
        _messages.Clear();
        _messages.Add(new StructuredChatMessage { Role = "user", Content = BuildUserPrompt(handoff) });
        sessionLog.Write("executor_session_start", new
        {
            Model = artifact.RepoId,
            artifact.FileName,
            artifact.Quantization,
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
        string clarificationAnswer,
        IProgress<ModelStreamChunk> streamProgress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_model is null || _storageSettings is null || _sessionLog is null)
        {
            throw new InvalidOperationException("Executor session has not been started.");
        }

        _messages.Add(new StructuredChatMessage
        {
            Role = "user",
            Content = "User selected or entered this answer to your previous question: " + clarificationAnswer.Trim()
        });
        _sessionLog.Write("executor_clarification_answer", new { Answer = clarificationAnswer });
        return await RunLoopAsync(streamProgress, cancellationToken);
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
                if (ExecutorResultParser.TryReadTurn(response.Content, out var turn))
                {
                    sessionLog.Write("executor_turn", turn);
                    return turn;
                }

                sessionLog.Write("executor_contract_repair_requested", new { RawResponse = response.Content });
                _messages.Add(new StructuredChatMessage
                {
                    Role = "user",
                    Content = "Your previous response broke the required JSON contract. Return the same intended turn again as JSON only. Do not add markdown or commentary. A question is clarification_step, never final_result."
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
                if (ExecutorResultParser.TryReadTurn(repaired.Content, out turn))
                {
                    sessionLog.Write("executor_contract_repaired", turn);
                    return turn;
                }

                sessionLog.Write("executor_contract_repair_failed", new { repaired.Content });
                throw new InvalidOperationException("Executor returned an invalid structured turn after repair.");
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
            "Create a compact, factual execution checkpoint. Preserve the goal, decisions, evidence, sources, tool results, artifacts, current plan, unfinished actions and risks. Do not solve the task again.",
            transcript,
            message => sessionLog.Write("executor_runtime", new { Message = message }),
            streamProgress,
            cancellationToken);
        _messages.Clear();
        _messages.Add(new StructuredChatMessage
        {
            Role = "user",
            Content = "Continue the original task from this verified checkpoint:\n" + checkpoint
        });
        sessionLog.Write("executor_context_checkpoint", new { Checkpoint = checkpoint });
    }

    private static string BuildSystemPrompt(ExecutorHandoffPackage handoff) =>
        string.Join(
            Environment.NewLine,
            "You are the selected AI executor in the Uncertainty scenario, not the AI HUB core.",
            "The core selected your capability class. It did not discover the user's exact subject and did not prepare a final task.",
            "Treat programFacts as authoritative, userSignals as raw user input, and coreHypotheses only as provisional background that may be wrong.",
            "Start from the broad suggested direction and narrow the user's actual task through short multiple-choice questions.",
            "Ask one decision at a time. Offer 2-6 short options and allow a custom answer whenever useful. You may ask as many non-repeating steps as genuinely needed.",
            "Do not perform the task until the subject and requested outcome are sufficiently clear.",
            "When ready, perform the task deeply and return an explicit final_result.",
            "Use only tools exposed by the application. Never claim that an unavailable tool was used.",
            "Every non-tool response must follow the JSON response schema. A prose question is never a final result.",
            "Keep thought to one short user-facing sentence. It is not hidden chain-of-thought.",
            $"Answer language: {handoff.LanguageCode}.");

    private static string BuildUserPrompt(ExecutorHandoffPackage handoff)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Handoff package with provenance:");
        builder.AppendLine(JsonSerializer.Serialize(handoff));
        builder.AppendLine();
        builder.AppendLine("Important: Goal and Executor prompt are provisional core hypotheses, not a confirmed user request.");
        builder.AppendLine("Begin by returning the first broad clarification_step unless the raw user signals already contain a concrete subject and outcome.");
        return builder.ToString();
    }
}
