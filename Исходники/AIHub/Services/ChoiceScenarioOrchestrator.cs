using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ChoiceScenarioOrchestrator
{
    private const int MaxToolRounds = 6;
    private const int MaxToolRequests = 8;
    private const int MaxRepairAttempts = 3;

    private static readonly HashSet<string> CoreWebTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "web_search", "web_research", "web_read"
    };

    private readonly ChoiceScenarioService _scenarioService;
    private readonly ChoiceExecutorCandidatePoolService _candidatePoolService = new();
    private readonly ToolGateway _toolGateway = new();

    public ChoiceScenarioOrchestrator(ChoiceScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
    }

    public async Task<ChoiceScenarioGenerationResult> GenerateAsync(
        LlamaServerRuntimeService runtime,
        DebugModelInfo model,
        string systemPrompt,
        string userPrompt,
        StorageSettings storageSettings,
        ISessionEventLog sessionLog,
        CancellationToken cancellationToken,
        bool requestFinal,
        bool mustReturnFinal,
        string workloadMode,
        ChoiceCapabilityProfile capabilityProfile,
        IProgress<ModelStreamChunk>? streamProgress = null)
    {
        var messages = new List<StructuredChatMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };
        var tools = ScenarioToolCatalog.CreateDefinitions()
            .Where(tool => CoreWebTools.Contains(tool.Function.Name))
            .ToList();
        var executedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toolRequests = 0;
        var rawResponse = string.Empty;
        var finalRequested = requestFinal || mustReturnFinal;
        var computerPassport = new ComputerPassportService().EnsurePassport();
        ChoiceExecutorCandidatePool? candidatePool = null;

        for (var round = 1; round <= MaxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (finalRequested && candidatePool is null)
            {
                candidatePool = await BuildCandidatePoolAsync(
                    storageSettings,
                    capabilityProfile,
                    workloadMode,
                    model.Name,
                    computerPassport,
                    sessionLog,
                    cancellationToken);
                if (!candidatePool.HasValidPair)
                {
                    return CreatePoolError(candidatePool);
                }

                messages.Add(new StructuredChatMessage
                {
                    Role = "user",
                    Content = ChoiceExecutorCandidatePoolService.BuildSelectionPrompt(candidatePool)
                });
            }

            var response = await runtime.GenerateWithToolsAsync(
                model,
                messages,
                finalRequested ? [] : tools,
                _ => { },
                cancellationToken,
                CoreInteractionMode.ScenarioPlanner,
                requiredToolName: null,
                streamProgress);
            rawResponse = response.Content;
            sessionLog.Write("scenario_core_structured_response", new
            {
                Round = round,
                response.FinishReason,
                response.Content,
                response.ToolCalls,
                FinalRequested = finalRequested,
                HasTrustedCandidatePool = candidatePool is not null
            });

            if (!response.HasToolCalls)
            {
                if (!finalRequested && IsFinalIntent(response.Content))
                {
                    finalRequested = true;
                    messages.Add(new StructuredChatMessage
                    {
                        Role = "assistant",
                        Content = response.Content
                    });
                    messages.Add(new StructuredChatMessage
                    {
                        Role = "user",
                        Content = "Это был предварительный финал без доверенного пула. Программа сейчас проверит кандидатов; после получения пула верни новый final_task_card только с его идентификаторами."
                    });
                    continue;
                }

                break;
            }

            messages.Add(new StructuredChatMessage
            {
                Role = "assistant",
                Content = string.IsNullOrWhiteSpace(response.Content) ? null : response.Content,
                ToolCalls = response.ToolCalls
            });

            foreach (var toolCall in response.ToolCalls)
            {
                if (++toolRequests > MaxToolRequests)
                {
                    rawResponse = string.Empty;
                    sessionLog.Write("scenario_tool_limit_reached", new { MaxToolRequests });
                    break;
                }

                var command = toolCall.Function.Name;
                string toolResult;
                try
                {
                    command = ScenarioToolCatalog.BuildCommand(toolCall);
                    if (!CoreWebTools.Contains(toolCall.Function.Name))
                    {
                        toolResult = "Tool call rejected: model inventory and catalog selection are handled by AI HUB.";
                    }
                    else if (!executedCommands.Add(command))
                    {
                        toolResult = "Duplicate tool call blocked. Use the previous result or continue with a different query.";
                        sessionLog.Write("scenario_duplicate_tool_call_blocked", new
                        {
                            toolCall.Id,
                            toolCall.Function.Name,
                            Command = command
                        });
                    }
                    else
                    {
                        sessionLog.Write("scenario_tool_call", new
                        {
                            toolCall.Id,
                            toolCall.Function.Name,
                            toolCall.Function.Arguments,
                            Command = command
                        });
                        toolResult = await _toolGateway.ExecuteAsync(
                            command,
                            storageSettings,
                            sessionLog,
                            cancellationToken,
                            downloadProgress: null,
                            currentCoreName: model.Name);
                    }
                }
                catch (Exception ex)
                {
                    toolResult = $"Tool error: {ex.Message}";
                    sessionLog.Write("scenario_tool_error", new
                    {
                        toolCall.Id,
                        toolCall.Function.Name,
                        ErrorType = ex.GetType().FullName,
                        ex.Message
                    });
                }

                messages.Add(new StructuredChatMessage
                {
                    Role = "tool",
                    ToolCallId = toolCall.Id,
                    Name = toolCall.Function.Name,
                    Content = ToolMessageFormatter.WrapToolResult(
                        toolCall.Function.Name,
                        command,
                        LimitForPrompt(toolResult))
                });
            }

            if (toolRequests > MaxToolRequests)
            {
                break;
            }
        }

        if (IsFinalIntent(rawResponse) && candidatePool is null)
        {
            candidatePool = await BuildCandidatePoolAsync(
                storageSettings,
                capabilityProfile,
                workloadMode,
                model.Name,
                computerPassport,
                sessionLog,
                cancellationToken);
            if (!candidatePool.HasValidPair)
            {
                return CreatePoolError(candidatePool);
            }
        }

        if (TryValidateResult(
                rawResponse,
                capabilityProfile,
                candidatePool,
                workloadMode,
                model.Name,
                computerPassport,
                mustReturnFinal,
                out var step,
                out var error))
        {
            return new ChoiceScenarioGenerationResult { Step = step, RawResponse = rawResponse };
        }

        for (var attempt = 1; attempt <= MaxRepairAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sessionLog.Write("scenario_json_repair_request", new { Attempt = attempt, Error = error });
            var repairContext = new List<string>
            {
                userPrompt,
                _scenarioService.BuildRepairPrompt(rawResponse, error)
            };
            if (candidatePool is not null)
            {
                repairContext.Insert(1, ChoiceExecutorCandidatePoolService.BuildSelectionPrompt(candidatePool));
            }

            rawResponse = await runtime.GenerateScenarioJsonAsync(
                model,
                systemPrompt,
                string.Join(Environment.NewLine + Environment.NewLine, repairContext),
                _ => { },
                cancellationToken);
            sessionLog.Write("scenario_json_repair_response", new { Attempt = attempt, Text = rawResponse });

            if (TryValidateResult(
                    rawResponse,
                    capabilityProfile,
                    candidatePool,
                    workloadMode,
                    model.Name,
                    computerPassport,
                    mustReturnFinal,
                    out step,
                    out error))
            {
                return new ChoiceScenarioGenerationResult
                {
                    Step = step,
                    RawResponse = rawResponse,
                    RepairAttempts = attempt
                };
            }
        }

        return new ChoiceScenarioGenerationResult
        {
            RawResponse = rawResponse,
            Error = error,
            RepairAttempts = MaxRepairAttempts
        };
    }

    private async Task<ChoiceExecutorCandidatePool> BuildCandidatePoolAsync(
        StorageSettings storageSettings,
        ChoiceCapabilityProfile capabilityProfile,
        string workloadMode,
        string currentCoreName,
        ComputerPassport computerPassport,
        ISessionEventLog sessionLog,
        CancellationToken cancellationToken)
    {
        var pool = await _candidatePoolService.BuildAsync(
            storageSettings,
            capabilityProfile,
            workloadMode,
            currentCoreName,
            computerPassport,
            sessionLog,
            cancellationToken);
        sessionLog.Write("scenario_trusted_candidate_pool", pool);
        return pool;
    }

    private bool TryValidateResult(
        string rawResponse,
        ChoiceCapabilityProfile capabilityProfile,
        ChoiceExecutorCandidatePool? candidatePool,
        string workloadMode,
        string currentCoreName,
        ComputerPassport computerPassport,
        bool mustReturnFinal,
        out ChoiceScenarioStep step,
        out string error)
    {
        if (!_scenarioService.TryParseStep(rawResponse, out step, out error)
            || !_scenarioService.ValidateProductivity(step, capabilityProfile, out error))
        {
            return false;
        }

        if (!step.IsFinal)
        {
            if (mustReturnFinal)
            {
                error = "The step budget is exhausted; final_task_card is required.";
                return false;
            }

            return true;
        }

        if (step.TaskCard is null || candidatePool is null)
        {
            error = "Final task card requires a trusted executor candidate pool.";
            return false;
        }

        return ChoiceExecutorCandidatePoolService.TryApplySelection(step.TaskCard, candidatePool, out error)
            && ChoiceExecutorPairValidator.Validate(
                step.TaskCard,
                candidatePool,
                workloadMode,
                currentCoreName,
                computerPassport,
                out error);
    }

    private static ChoiceScenarioGenerationResult CreatePoolError(ChoiceExecutorCandidatePool pool)
    {
        var error = pool.InstalledCandidates.Count == 0
            ? "AI HUB не нашёл установленную и запускаемую модель-исполнителя, подходящую режиму нагрузки."
            : "AI HUB не нашёл загружаемую альтернативу из другого семейства, совместимую с текущим ПК.";
        return new ChoiceScenarioGenerationResult { Error = error };
    }

    private static string LimitForPrompt(string value) =>
        value.Length <= 12_000 ? value : value[..12_000] + Environment.NewLine + "[truncated]";

    private static bool IsFinalIntent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.TryGetProperty("stepType", out var stepType)
                && string.Equals(stepType.GetString(), "final_task_card", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return content.Contains("final_task_card", StringComparison.OrdinalIgnoreCase);
        }
    }
}
