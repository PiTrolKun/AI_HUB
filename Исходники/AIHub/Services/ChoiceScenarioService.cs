using System.Text.Json;
using System.Text.Json.Serialization;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ChoiceScenarioService
{
    private static readonly HashSet<string> AllowedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "web_search", "web_research", "web_read", "inventory", "model_catalog_search", "hf_find_model", "hf_model_files"
    };

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public ChoiceScenarioStep CreateBudgetStep(Func<string, string> localize) => new()
    {
        StepType = "budget_setup",
        Question = localize("ChoiceScenario.BudgetQuestion"),
        CoreThought = localize("ChoiceScenario.BudgetThought"),
        AllowCustom = false,
        Options =
        [
            new() { Id = "budget_4", Title = localize("ChoiceScenario.Budget.Quick"), Description = localize("ChoiceScenario.Budget.QuickDescription") },
            new() { Id = "budget_10", Title = localize("ChoiceScenario.Budget.Normal"), Description = localize("ChoiceScenario.Budget.NormalDescription"), IsRecommended = true, RecommendationReason = localize("ChoiceScenario.Budget.NormalRecommendation") },
            new() { Id = "budget_20", Title = localize("ChoiceScenario.Budget.Detailed"), Description = localize("ChoiceScenario.Budget.DetailedDescription") },
            new() { Id = "budget_auto", Title = localize("ChoiceScenario.Budget.Automatic"), Description = localize("ChoiceScenario.Budget.AutomaticDescription") }
        ]
    };

    public ChoiceScenarioStep CreateDomainStartStep(Func<string, string> localize) => new()
    {
        StepType = "start_fixed_step",
        Question = localize("ChoiceScenario.StartQuestion"),
        CoreThought = localize("ChoiceScenario.StartThought"),
        DecisionDimension = ChoiceDecisionDimensions.DomainSpecialization,
        SelectionImpact = ["specialization", "model_class"],
        AllowCustom = true,
        Options =
        [
            DomainOption("knowledge", localize("ChoiceScenario.Start.Knowledge"), localize("ChoiceScenario.Start.KnowledgeDescription")),
            DomainOption("things", localize("ChoiceScenario.Start.Things"), localize("ChoiceScenario.Start.ThingsDescription")),
            DomainOption("life", localize("ChoiceScenario.Start.Life"), localize("ChoiceScenario.Start.LifeDescription")),
            DomainOption("technology", localize("ChoiceScenario.Start.Technology"), localize("ChoiceScenario.Start.TechnologyDescription")),
            DomainOption("people", localize("ChoiceScenario.Start.People"), localize("ChoiceScenario.Start.PeopleDescription")),
            DomainOption("goals", localize("ChoiceScenario.Start.Goals"), localize("ChoiceScenario.Start.GoalsDescription"), true, localize("ChoiceScenario.Start.GoalsRecommendation"))
        ]
    };

    public ChoiceScenarioStep CreateFallbackStep(
        IReadOnlyList<ChoiceScenarioAnswer> answers,
        Func<string, string> localize)
    {
        var area = answers.FirstOrDefault()?.OptionTitle ?? localize("ChoiceScenario.NotSelected");
        return new ChoiceScenarioStep
        {
            StepType = "question_step",
            Question = localize("ChoiceScenario.FallbackQuestion"),
            CoreThought = localize("ChoiceScenario.FallbackThought"),
            DecisionDimension = ChoiceDecisionDimensions.TaskType,
            SelectionImpact = ["model_class", "reasoning_strength", "tool_set"],
            AllowCustom = true,
            SummaryLines = [string.Format(System.Globalization.CultureInfo.CurrentCulture, localize("ChoiceScenario.AreaLine"), area)],
            Options =
            [
                TaskTypeOption("find_information", localize("ChoiceScenario.Fallback.Find"), localize("ChoiceScenario.Fallback.FindDescription")),
                TaskTypeOption("explain", localize("ChoiceScenario.Fallback.Explain"), localize("ChoiceScenario.Fallback.ExplainDescription")),
                TaskTypeOption("compare", localize("ChoiceScenario.Fallback.Compare"), localize("ChoiceScenario.Fallback.CompareDescription")),
                TaskTypeOption("create", localize("ChoiceScenario.Fallback.Create"), localize("ChoiceScenario.Fallback.CreateDescription"))
            ]
        };
    }

    public string BuildSystemPrompt() => ChoiceScenarioPromptBuilder.BuildSystemPrompt();

    public string BuildUserPrompt(
        IReadOnlyList<ChoiceScenarioAnswer> answers,
        bool requestFinal,
        bool mustReturnFinal,
        UserContextSnapshot userContext,
        string inventorySummary,
        string languageCode,
        ChoiceScenarioStepBudget stepBudget,
        int stepsUsed,
        int stepsRemaining,
        ChoiceCapabilityProfile capabilityProfile) => ChoiceScenarioPromptBuilder.BuildUserPrompt(
            answers,
            requestFinal,
            mustReturnFinal,
            userContext,
            inventorySummary,
            languageCode,
            stepBudget,
            stepsUsed,
            stepsRemaining,
            capabilityProfile);

    public bool TryParseStep(string jsonText, out ChoiceScenarioStep step, out string error)
    {
        step = new ChoiceScenarioStep();
        error = string.Empty;
        var trimmed = jsonText.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            error = "JSON object was not found.";
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ChoiceScenarioStep>(trimmed, _jsonOptions);
            if (parsed is null)
            {
                error = "JSON parsed to null.";
                return false;
            }

            if (!ValidateStep(parsed, out error))
            {
                return false;
            }

            step = parsed;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public string BuildRepairPrompt(string invalidResponse, string validationError) => string.Join(
        Environment.NewLine,
        "Предыдущий ответ не прошёл строгую проверку JSON-контракта.",
        $"Ошибка: {validationError}",
        "Не продолжай предметное углубление. Уточни ещё неизвестное измерение, которое реально меняет модель, инструменты, backend, контекст или режим выполнения.",
        "Исправь структуру и верни только один JSON-объект без markdown и пояснений.",
        "Предыдущий ответ:",
        invalidResponse);

    public bool ValidateProductivity(
        ChoiceScenarioStep step,
        ChoiceCapabilityProfile currentProfile,
        out string error)
    {
        error = string.Empty;
        if (step.IsFinal)
        {
            return ValidateFinalProfile(step, currentProfile, out error);
        }

        foreach (var update in step.ProfileUpdate)
        {
            var existing = currentProfile.Dimensions.FirstOrDefault(item => string.Equals(
                item.Dimension,
                update.Dimension,
                StringComparison.OrdinalIgnoreCase));
            if (existing is not null
                && existing.Status is ChoiceDimensionStatuses.Resolved or ChoiceDimensionStatuses.NotApplicable
                && !SameCapabilityState(existing, update)
                && string.IsNullOrWhiteSpace(step.RevisitReason))
            {
                error = $"profileUpdate cannot overwrite resolved dimension '{update.Dimension}' without revisitReason.";
                return false;
            }
        }

        var profileAfterUpdate = currentProfile.Clone();
        profileAfterUpdate.Apply(step.ProfileUpdate);
        if (profileAfterUpdate.GetStatus(ChoiceDecisionDimensions.TaskType) == ChoiceDimensionStatuses.Unknown
            && !string.Equals(step.DecisionDimension, ChoiceDecisionDimensions.TaskType, StringComparison.OrdinalIgnoreCase))
        {
            error = "task_type is unknown and must be resolved before other executor dimensions.";
            return false;
        }

        if (LooksLikeNarrowSubjectMatterQuestion(step.Question))
        {
            error = "Question narrows subject matter without establishing an executor requirement.";
            return false;
        }

        var status = profileAfterUpdate.GetStatus(step.DecisionDimension);
        if (status is ChoiceDimensionStatuses.Resolved or ChoiceDimensionStatuses.NotApplicable
            && string.IsNullOrWhiteSpace(step.RevisitReason))
        {
            error = $"Decision dimension '{step.DecisionDimension}' is already {status}; choose another unresolved executor dimension.";
            return false;
        }

        var signatures = step.Options
            .Select(option => string.Join(
                '|',
                option.ProfileEffects
                    .OrderBy(effect => effect.Dimension, StringComparer.OrdinalIgnoreCase)
                    .Select(effect => $"{effect.Dimension}:{effect.Status}:{string.Join(',', effect.Values.Order(StringComparer.OrdinalIgnoreCase))}")))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        if (step.Options.Count > 1 && signatures < 2)
        {
            error = "Question options do not produce different capability profile outcomes.";
            return false;
        }

        return true;
    }

    private static bool ValidateStep(ChoiceScenarioStep step, out string error)
    {
        error = string.Empty;
        NormalizeStep(step);
        if (string.IsNullOrWhiteSpace(step.Question) || string.IsNullOrWhiteSpace(step.CoreThought))
        {
            error = "Question or coreThought is empty.";
            return false;
        }

        if (step.StepType is not ("question_step" or "final_task_card"))
        {
            error = "Unknown stepType.";
            return false;
        }

        return step.StepType == "question_step"
            ? ValidateQuestionStep(step, out error)
            : ValidateFinalStep(step, out error);
    }

    private static bool ValidateQuestionStep(ChoiceScenarioStep step, out string error)
    {
        error = string.Empty;
        step.TaskCard = null;
        if (!ChoiceDecisionDimensions.IsKnown(step.DecisionDimension))
        {
            error = "Unknown or empty decisionDimension.";
            return false;
        }

        if (step.SelectionImpact.Count == 0
            || step.SelectionImpact.Any(impact => !ChoiceSelectionImpacts.IsKnown(impact)))
        {
            error = "selectionImpact must contain known executor-selection effects.";
            return false;
        }

        if (!ValidateCapabilityDimensions(step.ProfileUpdate, true, out error))
        {
            return false;
        }

        if (step.IsFinal || !step.AllowCustom || step.Options.Count is < 1 or > 6)
        {
            error = "Question step must be non-final, allow custom input, and contain 1-6 options.";
            return false;
        }

        if (step.Options.Count(option => option.IsRecommended) > 1)
        {
            error = "Only one option may be recommended.";
            return false;
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in step.Options)
        {
            NormalizeOption(option);
            if (string.IsNullOrWhiteSpace(option.Id) || !ids.Add(option.Id))
            {
                error = "Option ids must be non-empty and unique.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(option.Title) || option.Title.Length > 32)
            {
                error = "Option title must contain 1-32 characters.";
                return false;
            }
            if (option.Description.Length > 140 || option.RecommendationReason.Length > 120)
            {
                error = "Option description or recommendation reason is too long.";
                return false;
            }
            if (option.IsRecommended && string.IsNullOrWhiteSpace(option.RecommendationReason))
            {
                error = "Recommended option must explain the recommendation.";
                return false;
            }
            if (!ValidateCapabilityDimensions(option.ProfileEffects, false, out error))
            {
                return false;
            }
            if (!option.ProfileEffects.Any(effect => string.Equals(
                effect.Dimension,
                step.DecisionDimension,
                StringComparison.OrdinalIgnoreCase)))
            {
                error = "Every option must change the current decisionDimension.";
                return false;
            }
        }

        return true;
    }

    private static bool ValidateFinalStep(ChoiceScenarioStep step, out string error)
    {
        error = string.Empty;
        if (!step.IsFinal || step.Options.Count != 0 || step.AllowCustom || step.TaskCard is null)
        {
            error = "Final step must be final, have no options/custom input, and contain taskCard.";
            return false;
        }
        if (!string.IsNullOrWhiteSpace(step.DecisionDimension)
            || step.SelectionImpact.Count != 0
            || !ValidateCapabilityDimensions(step.ProfileUpdate, true, out error))
        {
            error = string.IsNullOrWhiteSpace(error)
                ? "Final step must not ask a decision dimension or declare selection impacts."
                : error;
            return false;
        }

        var card = step.TaskCard;
        NormalizeCard(card);
        if (string.IsNullOrWhiteSpace(card.Goal)
            || string.IsNullOrWhiteSpace(card.Area)
            || string.IsNullOrWhiteSpace(card.RecommendedExecutor)
            || string.IsNullOrWhiteSpace(card.ExecutorStatus)
            || string.IsNullOrWhiteSpace(card.ExecutorReason)
            || string.IsNullOrWhiteSpace(card.PromptForExecutor))
        {
            error = "Final task card has empty required fields.";
            return false;
        }
        if (!ValidateCapabilityDimensions(card.CapabilityProfile.Dimensions, false, out error))
        {
            return false;
        }
        if (card.ExecutorRole is not ("general_worker" or "specialist_model" or "core_fallback"))
        {
            error = "executorRole must be general_worker, specialist_model, or core_fallback.";
            return false;
        }
        if (!ChoiceExecutorPolicy.IsKnownCapabilityClass(card.ExecutorCapabilityClass))
        {
            error = "Unknown executorCapabilityClass.";
            return false;
        }
        if (LooksLikeTool(card.RecommendedExecutor))
        {
            error = "recommendedExecutor must be a model, not a tool.";
            return false;
        }
        if (card.RequiredTools.Any(tool => !AllowedTools.Contains(tool)))
        {
            error = "requiredTools contains an unavailable or forbidden tool.";
            return false;
        }
        var hasWebTool = card.RequiredTools.Any(tool =>
            tool.StartsWith("web_", StringComparison.OrdinalIgnoreCase));
        if (card.NeedsWeb && !hasWebTool)
        {
            error = "needsWeb=true requires at least one web tool.";
            return false;
        }
        if (!card.NeedsWeb && hasWebTool)
        {
            error = "needsWeb=false cannot include web tools in requiredTools.";
            return false;
        }

        var explicitlyForbidsExternalData = card.CapabilityProfile.Dimensions.Any(dimension =>
            string.Equals(dimension.Dimension, ChoiceDecisionDimensions.ToolRequirements, StringComparison.OrdinalIgnoreCase)
            && dimension.Values.Any(value =>
                string.Equals(value, "no_external_data", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "offline_only", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "local_only", StringComparison.OrdinalIgnoreCase)));
        if (explicitlyForbidsExternalData && (card.NeedsWeb || hasWebTool))
        {
            error = "The capability profile forbids external data but the final card requests web access.";
            return false;
        }

        return true;
    }

    private static bool ValidateFinalProfile(
        ChoiceScenarioStep step,
        ChoiceCapabilityProfile currentProfile,
        out string error)
    {
        error = string.Empty;
        if (step.TaskCard is null)
        {
            error = "Final task card is missing.";
            return false;
        }
        var taskType = step.TaskCard.CapabilityProfile.Dimensions.FirstOrDefault(item =>
            string.Equals(item.Dimension, ChoiceDecisionDimensions.TaskType, StringComparison.OrdinalIgnoreCase));
        if (taskType is null || taskType.Status == ChoiceDimensionStatuses.Unknown)
        {
            error = "Final capability profile must identify the task_type before selecting an executor.";
            return false;
        }
        foreach (var resolved in currentProfile.Dimensions.Where(item =>
            item.Status is ChoiceDimensionStatuses.Resolved or ChoiceDimensionStatuses.NotApplicable))
        {
            if (!step.TaskCard.CapabilityProfile.Dimensions.Any(item => string.Equals(
                item.Dimension,
                resolved.Dimension,
                StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Final capability profile lost resolved dimension '{resolved.Dimension}'.";
                return false;
            }
        }

        return true;
    }

    private static bool ValidateCapabilityDimensions(
        IReadOnlyList<ChoiceCapabilityDimension> dimensions,
        bool allowEmpty,
        out string error)
    {
        error = string.Empty;
        if (!allowEmpty && dimensions.Count == 0)
        {
            error = "Capability profile effects must not be empty.";
            return false;
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in dimensions)
        {
            NormalizeDimension(item);
            if (!ChoiceDecisionDimensions.IsKnown(item.Dimension)
                || !ChoiceDimensionStatuses.IsKnown(item.Status)
                || !seen.Add(item.Dimension))
            {
                error = "Capability profile contains an unknown or duplicate dimension/status.";
                return false;
            }
            if (item.Status != ChoiceDimensionStatuses.NotApplicable && item.Values.Count == 0)
            {
                error = "Applicable capability dimensions require at least one value.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(item.Evidence))
            {
                error = "Capability profile dimensions require short evidence.";
                return false;
            }
        }

        return true;
    }

    private static void NormalizeStep(ChoiceScenarioStep step)
    {
        step.Options ??= [];
        step.SummaryLines ??= [];
        step.SelectionImpact ??= [];
        step.SelectionImpact = step.SelectionImpact
            .Select(NormalizeSelectionImpact)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        step.ProfileUpdate ??= [];
        step.DecisionDimension ??= string.Empty;
        step.RevisitReason ??= string.Empty;
    }

    private static void NormalizeOption(ChoiceScenarioOption option)
    {
        option.Id ??= string.Empty;
        option.Title ??= string.Empty;
        option.Description ??= string.Empty;
        option.RecommendationReason ??= string.Empty;
        option.ProfileEffects ??= [];
    }

    private static void NormalizeCard(ChoiceTaskCard card)
    {
        card.Goal ??= string.Empty;
        card.Area ??= string.Empty;
        card.ExecutorRole = NormalizeExecutorRole(card.ExecutorRole);
        card.ExecutorCapabilityClass ??= string.Empty;
        card.RecommendedExecutor ??= string.Empty;
        card.ExecutorStatus ??= string.Empty;
        card.ExecutorReason ??= string.Empty;
        card.PromptForExecutor ??= string.Empty;
        card.Criteria ??= [];
        card.Constraints ??= [];
        card.RequiredTools ??= [];
        card.CapabilityProfile ??= new ChoiceCapabilityProfile();
        card.CapabilityProfile.Dimensions ??= [];
    }

    private static string NormalizeSelectionImpact(string value) => value?.Trim().ToLowerInvariant() switch
    {
        "context_volume" => "context_window",
        "executorrole" or "executor_role" => "model_class",
        _ => value?.Trim() ?? string.Empty
    };

    private static string NormalizeExecutorRole(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "data_analyst" or "domain_specialist" or "research_specialist" or "code_specialist" or "specialist" => "specialist_model",
        "generalist" or "general_model" or "worker" => "general_worker",
        _ => value?.Trim() ?? string.Empty
    };

    private static void NormalizeDimension(ChoiceCapabilityDimension item)
    {
        item.Dimension ??= string.Empty;
        item.Status ??= string.Empty;
        item.Values ??= [];
        item.Evidence ??= string.Empty;
    }

    private static bool SameCapabilityState(ChoiceCapabilityDimension left, ChoiceCapabilityDimension right) =>
        string.Equals(left.Status, right.Status, StringComparison.OrdinalIgnoreCase)
        && left.Values.Order(StringComparer.OrdinalIgnoreCase).SequenceEqual(
            right.Values.Order(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

    private static bool LooksLikeTool(string value) => value.Trim().ToLowerInvariant() is
        "web_search" or "web_research" or "web_read" or "inventory" or "model_catalog_search" or "hf_find_model" or "hf_model_files";

    private static bool LooksLikeNarrowSubjectMatterQuestion(string question)
    {
        var normalized = string.Join(
            ' ',
            question.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        string[] blockedFragments =
        [
            "какой именно аспект", "какая именно часть", "какую именно часть", "какие последствия",
            "какая отрасль", "какая реформа", "какие группы населения", "which specific aspect",
            "which exact part", "what consequences", "which reform"
        ];
        return blockedFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }

    private static ChoiceScenarioOption DomainOption(
        string id,
        string title,
        string description,
        bool isRecommended = false,
        string recommendationReason = "") => new()
        {
            Id = id,
            Title = title,
            Description = description,
            IsRecommended = isRecommended,
            RecommendationReason = recommendationReason,
            ProfileEffects = [DimensionEffect(ChoiceDecisionDimensions.DomainSpecialization, ChoiceDimensionStatuses.Provisional, id, $"Selected broad domain: {title}")]
        };

    private static ChoiceScenarioOption TaskTypeOption(string id, string title, string description) => new()
    {
        Id = id,
        Title = title,
        Description = description,
        ProfileEffects = [DimensionEffect(ChoiceDecisionDimensions.TaskType, ChoiceDimensionStatuses.Resolved, id, $"Selected task type: {title}")]
    };

    private static ChoiceCapabilityDimension DimensionEffect(
        string dimension,
        string status,
        string value,
        string evidence) => new()
        {
            Dimension = dimension,
            Status = status,
            Values = [value],
            Evidence = evidence
        };
}
