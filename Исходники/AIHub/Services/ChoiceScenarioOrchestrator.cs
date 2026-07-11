using AIHub.Models;
using System.Text.Json;

namespace AIHub.Services;

public sealed class ChoiceScenarioOrchestrator
{
    private const int MaxToolRounds = 6;
    private const int MaxToolRequests = 8;
    private const int MaxRepairAttempts = 2;

    private readonly ChoiceScenarioService _scenarioService;
    private readonly ToolGateway _toolGateway;

    public ChoiceScenarioOrchestrator(ChoiceScenarioService scenarioService)
    {
        _scenarioService = scenarioService;
        _toolGateway = new ToolGateway();
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
        ChoiceCapabilityProfile capabilityProfile)
    {
        var messages = new List<StructuredChatMessage>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };
        var tools = ScenarioToolCatalog.CreateDefinitions();
        var toolRequests = 0;
        var calledTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toolEvidence = new List<string>();
        var catalogHasCandidates = false;
        var catalogNeedsLiveSearch = false;
        var liveModelSearchUnavailable = false;
        var rawResponse = string.Empty;
        var finalVerificationRequired = requestFinal;
        var rejectedExecutors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var computerPassport = new ComputerPassportService().EnsurePassport();

        for (var round = 1; round <= MaxToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var requiredToolName = finalVerificationRequired
                ? GetMissingFinalTool(calledTools, catalogHasCandidates)
                : null;
            var response = await runtime.GenerateWithToolsAsync(
                model,
                messages,
                tools,
                _ => { },
                cancellationToken,
                CoreInteractionMode.ScenarioPlanner,
                requiredToolName);
            rawResponse = response.Content;
            sessionLog.Write("scenario_core_structured_response", new
            {
                Round = round,
                response.FinishReason,
                response.Content,
                response.ToolCalls
            });

            if (!response.HasToolCalls)
            {
                finalVerificationRequired |= IsFinalIntent(response.Content);
                if (finalVerificationRequired && GetMissingFinalTool(calledTools, catalogHasCandidates) is { } missingTool)
                {
                    messages.Add(new StructuredChatMessage { Role = "assistant", Content = response.Content });
                    messages.Add(new StructuredChatMessage
                    {
                        Role = "user",
                        Content = BuildMissingToolInstruction(missingTool, workloadMode)
                    });
                    sessionLog.Write("scenario_final_verification_required", new
                    {
                        HasInventory = calledTools.Contains("inventory"),
                        HasCatalogSearch = calledTools.Contains("model_catalog_search"),
                        HasCatalogCandidates = catalogHasCandidates,
                        HasLiveModelSearch = calledTools.Contains("hf_find_model")
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

                string command;
                string toolResult;
                try
                {
                    command = ScenarioToolCatalog.BuildCommand(toolCall);
                    calledTools.Add(toolCall.Function.Name);
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
                    if (string.Equals(toolCall.Function.Name, "model_catalog_search", StringComparison.OrdinalIgnoreCase))
                    {
                        catalogHasCandidates = !HasNoModelCandidates(toolResult)
                            && !toolResult.Contains("Tool error.", StringComparison.OrdinalIgnoreCase);
                        catalogNeedsLiveSearch = !catalogHasCandidates;
                    }
                    if (string.Equals(toolCall.Function.Name, "hf_find_model", StringComparison.OrdinalIgnoreCase)
                        && HasNoModelCandidates(toolResult))
                    {
                        sessionLog.Write("scenario_model_search_empty", new
                        {
                            OriginalCommand = command,
                            Reason = "The core must independently reformulate model discovery without a program-selected family."
                        });
                        toolResult = string.Join(
                            Environment.NewLine,
                            toolResult,
                            string.Empty,
                            "AI HUB policy: no candidate was found. Independently choose a different model family or a broader capability-based repository query using the task requirements. The program does not prescribe a family or publisher.");
                    }

                    if (string.Equals(toolCall.Function.Name, "hf_find_model", StringComparison.OrdinalIgnoreCase)
                        && toolResult.Contains("Tool error.", StringComparison.OrdinalIgnoreCase))
                    {
                        liveModelSearchUnavailable = true;
                    }
                }
                catch (Exception ex)
                {
                    command = toolCall.Function.Name;
                    toolResult = $"Tool error: {ex.Message}";
                    sessionLog.Write("scenario_tool_error", new
                    {
                        toolCall.Id,
                        toolCall.Function.Name,
                        ErrorType = ex.GetType().FullName,
                        ex.Message
                    });
                    if (string.Equals(toolCall.Function.Name, "hf_find_model", StringComparison.OrdinalIgnoreCase))
                    {
                        liveModelSearchUnavailable = true;
                    }
                    if (string.Equals(toolCall.Function.Name, "model_catalog_search", StringComparison.OrdinalIgnoreCase))
                    {
                        catalogNeedsLiveSearch = true;
                    }
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
                toolEvidence.Add($"{toolCall.Function.Name}: {LimitForPrompt(toolResult)}");
            }

            if (toolRequests > MaxToolRequests)
            {
                break;
            }
        }

        if (_scenarioService.TryParseStep(rawResponse, out var step, out var error)
            && _scenarioService.ValidateProductivity(step, capabilityProfile, out error)
            && ValidateFinalEvidence(
                step,
                calledTools,
                toolEvidence,
                catalogNeedsLiveSearch && liveModelSearchUnavailable,
                catalogHasCandidates,
                workloadMode,
                model.Name,
                computerPassport,
                mustReturnFinal,
                out error))
        {
            return new ChoiceScenarioGenerationResult { Step = step, RawResponse = rawResponse };
        }

        RememberRejectedExecutor(step, error, rejectedExecutors);

        for (var attempt = 1; attempt <= MaxRepairAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sessionLog.Write("scenario_json_repair_request", new { Attempt = attempt, Error = error });
            rawResponse = await runtime.GenerateScenarioJsonAsync(
                model,
                systemPrompt,
                userPrompt
                    + Environment.NewLine + Environment.NewLine
                    + "Результаты уже выполненных инструментов:" + Environment.NewLine
                    + string.Join(Environment.NewLine + Environment.NewLine, toolEvidence)
                    + Environment.NewLine + Environment.NewLine
                    + BuildExecutorRepairInstruction(workloadMode, error, rejectedExecutors)
                    + Environment.NewLine + Environment.NewLine
                    + _scenarioService.BuildRepairPrompt(rawResponse, error),
                _ => { },
                cancellationToken);
            sessionLog.Write("scenario_json_repair_response", new { Attempt = attempt, Text = rawResponse });
            if (_scenarioService.TryParseStep(rawResponse, out step, out error)
                && _scenarioService.ValidateProductivity(step, capabilityProfile, out error)
                && ValidateFinalEvidence(
                    step,
                    calledTools,
                    toolEvidence,
                    catalogNeedsLiveSearch && liveModelSearchUnavailable,
                    catalogHasCandidates,
                    workloadMode,
                    model.Name,
                    computerPassport,
                    mustReturnFinal,
                    out error))
            {
                return new ChoiceScenarioGenerationResult
                {
                    Step = step,
                    RawResponse = rawResponse,
                    RepairAttempts = attempt
                };
            }


            RememberRejectedExecutor(step, error, rejectedExecutors);
        }

        return new ChoiceScenarioGenerationResult
        {
            RawResponse = rawResponse,
            Error = error,
            RepairAttempts = MaxRepairAttempts
        };
    }

    private static string LimitForPrompt(string value) =>
        value.Length <= 12_000 ? value : value[..12_000] + Environment.NewLine + "[truncated]";

    private static bool ValidateFinalEvidence(
        ChoiceScenarioStep step,
        IReadOnlySet<string> calledTools,
        IReadOnlyList<string> toolEvidence,
        bool modelSearchUnavailable,
        bool catalogHasCandidates,
        string workloadMode,
        string currentCoreName,
        ComputerPassport computerPassport,
        bool mustReturnFinal,
        out string error)
    {
        error = string.Empty;
        if (!step.IsFinal)
        {
            if (mustReturnFinal)
            {
                error = "The step budget is exhausted; final_task_card is required.";
                return false;
            }

            return true;
        }

        if (!calledTools.Contains("inventory") || !calledTools.Contains("model_catalog_search"))
        {
            error = "Final task card requires inventory and model_catalog_search evidence.";
            return false;
        }

        if (!catalogHasCandidates && !calledTools.Contains("hf_find_model"))
        {
            error = "The local catalog returned no candidates; hf_find_model evidence is required.";
            return false;
        }

        if (step.TaskCard is null)
        {
            error = "Final task card is missing.";
            return false;
        }

        ChoiceModelCandidateSelector.TryGetCatalogCandidate(
            step.TaskCard.RecommendedExecutor,
            toolEvidence,
            out var catalogCandidate);
        var hasCatalogCandidate = !string.IsNullOrWhiteSpace(catalogCandidate.RepoId);
        if (!ChoiceExecutorPolicy.Validate(
                step.TaskCard,
                workloadMode,
                modelSearchUnavailable,
                currentCoreName,
                hasCatalogCandidate ? catalogCandidate : null,
                out error))
        {
            return false;
        }

        if (!modelSearchUnavailable
            && !ChoiceModelCandidateSelector.IsVerifiedChoice(step.TaskCard.RecommendedExecutor, toolEvidence))
        {
            error = "The selected executor must exactly match a repository or model file returned by model_catalog_search or hf_find_model. AI HUB will not substitute a model.";
            return false;
        }

        long? parameterCount = hasCatalogCandidate
            ? catalogCandidate.ParameterCount
            : ChoiceModelCandidateSelector.TryGetVerifiedParameterCount(
                step.TaskCard.RecommendedExecutor,
                toolEvidence,
                out var verifiedParameterCount)
                ? verifiedParameterCount
                : ModelHardwareCompatibilityService.TryReadParameterCountFromName(step.TaskCard.RecommendedExecutor);
        var hardware = ModelHardwareCompatibilityService.Assess(parameterCount, computerPassport, workloadMode);
        if (hardware.IsCompatible == false)
        {
            error = $"The selected executor does not fit the current PC: {hardware.Reason}";
            return false;
        }

        if (hardware.IsCompatible is null
            && !string.Equals(workloadMode, UserWorkloadModes.Light, StringComparison.OrdinalIgnoreCase))
        {
            error = "The selected executor has insufficient metadata for mandatory PC compatibility verification.";
            return false;
        }

        return true;
    }

    private static string? GetMissingFinalTool(IReadOnlySet<string> calledTools, bool catalogHasCandidates)
    {
        if (!calledTools.Contains("inventory"))
        {
            return "inventory";
        }

        if (!calledTools.Contains("model_catalog_search"))
        {
            return "model_catalog_search";
        }

        return !catalogHasCandidates && !calledTools.Contains("hf_find_model") ? "hf_find_model" : null;
    }

    private static string BuildMissingToolInstruction(string toolName, string workloadMode)
    {
        var powerRule = string.Equals(workloadMode, UserWorkloadModes.Light, StringComparison.OrdinalIgnoreCase)
            ? "Профиль разрешает лёгкий режим, но всё равно сравни доступные варианты."
            : "Профиль не в лёгком режиме: ищи рабочую модель строго мощнее 8B; текущее 8B-ядро является только планировщиком и поисковиком.";
        return $"Перед финальной карточкой обязательно вызови {toolName} через structured tool call. {powerRule} Ничего не скачивай.";
    }

    private static bool IsFinalIntent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            return root.TryGetProperty("stepType", out var stepType)
                && string.Equals(stepType.GetString(), "final_task_card", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return content.Contains("final_task_card", StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool HasNoModelCandidates(string toolResult) =>
        toolResult.Contains("Candidates found: 0", StringComparison.OrdinalIgnoreCase);

    private static string BuildExecutorRepairInstruction(
        string workloadMode,
        string error,
        IReadOnlySet<string> rejectedExecutors)
    {
        var rejectionRule = rejectedExecutors.Count == 0
            ? string.Empty
            : Environment.NewLine
                + "ЗАПРЕЩЕНО повторно возвращать уже отклонённых исполнителей: "
                + string.Join(", ", rejectedExecutors)
                + ". Выбери другой точный repo/model из evidence; не меняй только роль или описание прежней модели.";
        if (string.Equals(workloadMode, UserWorkloadModes.Light, StringComparison.OrdinalIgnoreCase))
        {
            return "Исправляя исполнителя, используй подтверждённого кандидата из результатов model_catalog_search или hf_find_model. current_core допустим только как core_fallback при реальной ошибке обоих поисковых каналов."
                + rejectionRule;
        }

        return string.Join(
            Environment.NewLine,
            "Жёсткое исправление политики исполнителя:",
            "- режим профиля не light; core_fallback запрещён;",
            "- current_core и любая модель 8B или слабее запрещены как рабочий исполнитель;",
            "- самостоятельно сравни назначение найденных моделей с задачей; AI HUB не задаёт семейство или издателя;",
            "- выбери точный repo/model кандидата мощнее 8B из фактических результатов model_catalog_search или hf_find_model;",
            "- executorRole должен быть general_worker или specialist_model;",
            "- executorCapabilityClass должен быть above_8b;",
            "- если модель найдена в интернете, но отсутствует в inventory, executorStatus должен быть not_installed;",
            $"- текущая ошибка: {error}")
            + rejectionRule;
    }

    private static void RememberRejectedExecutor(
        ChoiceScenarioStep step,
        string error,
        ISet<string> rejectedExecutors)
    {
        if (step.TaskCard is null || string.IsNullOrWhiteSpace(step.TaskCard.RecommendedExecutor))
        {
            return;
        }

        string[] executorErrorMarkers =
        [
            "current core", "current_core", "same family", "newer generation", "stronger than 8b",
            "above-8b", "8b or less", "selected executor", "repository or model file",
            "current pc", "hardware", "compatibility verification", "does not fit"
        ];
        if (executorErrorMarkers.Any(marker => error.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            rejectedExecutors.Add(step.TaskCard.RecommendedExecutor.Trim());
        }
    }

}
