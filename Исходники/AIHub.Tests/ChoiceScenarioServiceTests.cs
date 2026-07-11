using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ChoiceScenarioServiceTests
{
    private readonly ChoiceScenarioService _service = new();

    [TestMethod]
    public void CreateBudgetStep_ProvidesFixedDepthOptions()
    {
        var step = _service.CreateBudgetStep(key => key);

        CollectionAssert.AreEqual(
            new[] { "budget_4", "budget_10", "budget_20", "budget_auto" },
            step.Options.Select(option => option.Id).ToArray());
        Assert.IsFalse(step.AllowCustom);
        Assert.AreEqual("budget_setup", step.StepType);
    }

    [TestMethod]
    public void TryParseStep_AcceptsValidQuestionStep()
    {
        const string json = """
            {
              "stepType": "question_step",
              "question": "Что выбираем?",
              "coreThought": "Уточняю предмет выбора.",
              "decisionDimension": "task_type",
              "selectionImpact": ["model_class"],
              "profileUpdate": [],
              "revisitReason": "",
              "options": [
                {
                  "id": "work",
                  "title": "Работа",
                  "description": "Выбор, связанный с работой.",
                  "isRecommended": false,
                  "recommendationReason": "",
                  "profileEffects": [{"dimension":"task_type","status":"resolved","values":["compare"],"evidence":"user choice"}]
                }
              ],
              "allowCustom": true,
              "isFinal": false,
              "summaryLines": [],
              "taskCard": null
            }
            """;

        var success = _service.TryParseStep(json, out var step, out var error);

        Assert.IsTrue(success, error);
        Assert.AreEqual("Что выбираем?", step.Question);
    }

    [TestMethod]
    public void TryParseStep_NormalizesSafeSelectionImpactAlias()
    {
        var json = ValidQuestionJson().Replace("model_class", "context_volume", StringComparison.Ordinal);

        Assert.IsTrue(_service.TryParseStep(json, out var step, out var error), error);
        CollectionAssert.AreEqual(new[] { "context_window" }, step.SelectionImpact);
    }

    [TestMethod]
    public void TryParseStep_NormalizesSpecialistExecutorRoleAlias()
    {
        const string json = """
            {
              "stepType":"final_task_card","question":"Готово","coreThought":"Карточка готова.",
              "decisionDimension":"","selectionImpact":[],"profileUpdate":[],"revisitReason":"",
              "options":[],"allowCustom":false,"isFinal":true,"summaryLines":[],
              "taskCard":{
                "goal":"Анализ данных","area":"Технологии","criteria":[],"constraints":[],
                "needsWeb":false,"requiredTools":[],
                "capabilityProfile":{"dimensions":[{"dimension":"task_type","status":"resolved","values":["data_analysis"],"evidence":"choice"}]},
                "executorRole":"data_analyst","executorCapabilityClass":"above_8b",
                "recommendedExecutor":"lab/AnalysisModel-20B","executorStatus":"not_installed",
                "executorReason":"Подходит для анализа","promptForExecutor":"Уточни данные и проведи анализ"
              }
            }
            """;

        Assert.IsTrue(_service.TryParseStep(json, out var step, out var error), error);
        Assert.AreEqual("specialist_model", step.TaskCard?.ExecutorRole);
    }

    [TestMethod]
    public void TryParseStep_RejectsMarkdownWrappedJson()
    {
        const string json = """
            ```json
            {"stepType":"question_step","question":"Q","coreThought":"T","options":[],"allowCustom":true,"isFinal":false,"summaryLines":[],"taskCard":null}
            ```
            """;

        Assert.IsFalse(_service.TryParseStep(json, out _, out _));
    }

    [TestMethod]
    public void TryParseStep_RejectsDuplicateOptionIds()
    {
        const string json = """
            {
              "stepType":"question_step",
              "question":"Q",
              "coreThought":"T",
              "decisionDimension":"task_type",
              "selectionImpact":["model_class"],
              "profileUpdate":[],
              "revisitReason":"",
              "options":[
                {"id":"same","title":"One","description":"","isRecommended":false,"recommendationReason":"","profileEffects":[{"dimension":"task_type","status":"resolved","values":["one"],"evidence":"one"}]},
                {"id":"same","title":"Two","description":"","isRecommended":false,"recommendationReason":"","profileEffects":[{"dimension":"task_type","status":"resolved","values":["two"],"evidence":"two"}]}
              ],
              "allowCustom":true,
              "isFinal":false,
              "summaryLines":[],
              "taskCard":null
            }
            """;

        Assert.IsFalse(_service.TryParseStep(json, out _, out var error));
        StringAssert.Contains(error, "unique");
    }

    [TestMethod]
    public void TryParseStep_RejectsToolAsExecutor()
    {
        const string json = """
            {
              "stepType":"final_task_card",
              "question":"Готово",
              "coreThought":"Карточка подготовлена.",
              "decisionDimension":"",
              "selectionImpact":[],
              "profileUpdate":[],
              "revisitReason":"",
              "options":[],
              "allowCustom":false,
              "isFinal":true,
              "summaryLines":[],
              "taskCard":{
                "goal":"Сделать выбор",
                "area":"Работа",
                "criteria":[],
                "constraints":[],
                "needsWeb":true,
                "requiredTools":["web_research"],
                "capabilityProfile":{"dimensions":[{"dimension":"task_type","status":"resolved","values":["research"],"evidence":"choice"}]},
                "executorRole":"general_worker",
                "executorCapabilityClass":"above_8b",
                "recommendedExecutor":"web_research",
                "executorStatus":"available",
                "executorReason":"Свежие данные",
                "promptForExecutor":"Сравни варианты"
              }
            }
            """;

        Assert.IsFalse(_service.TryParseStep(json, out _, out var error));
        StringAssert.Contains(error, "not a tool");
    }

    [TestMethod]
    public void TryParseStep_IgnoresTaskCardAttachedToQuestionStep()
    {
        const string json = """
            {
              "stepType":"question_step",
              "question":"Что уточнить?",
              "coreThought":"Продолжаю сужение.",
              "decisionDimension":"task_type",
              "selectionImpact":["model_class"],
              "profileUpdate":[],
              "revisitReason":"",
              "options":[{"id":"one","title":"Вариант","description":"","isRecommended":false,"recommendationReason":"","profileEffects":[{"dimension":"task_type","status":"resolved","values":["one"],"evidence":"choice"}]}],
              "allowCustom":true,
              "isFinal":false,
              "summaryLines":[],
              "taskCard":{
                "goal":"Лишняя карточка",
                "area":"Тест",
                "criteria":[],
                "constraints":[],
                "needsWeb":false,
                "requiredTools":[],
                "capabilityProfile":{"dimensions":[{"dimension":"task_type","status":"resolved","values":["one"],"evidence":"choice"}]},
                "executorRole":"general_worker",
                "executorCapabilityClass":"above_8b",
                "recommendedExecutor":"Qwen3 14B",
                "executorStatus":"available",
                "executorReason":"test",
                "promptForExecutor":"test"
              }
            }
            """;

        Assert.IsTrue(_service.TryParseStep(json, out var step, out var error), error);
        Assert.IsNull(step.TaskCard);
    }

    [TestMethod]
    public void BuildUserPrompt_ContainsStepBudgetAndHardFinalRule()
    {
        var budget = new ChoiceScenarioStepBudget { Mode = "quick", MaximumSteps = 4 };

        var prompt = _service.BuildUserPrompt(
            [],
            requestFinal: false,
            mustReturnFinal: true,
            new UserContextSnapshot(),
            "inventory",
            "ru",
            budget,
            stepsUsed: 4,
            stepsRemaining: 0,
            capabilityProfile: new ChoiceCapabilityProfile());

        StringAssert.Contains(prompt, "maximum_substantive_steps: 4");
        StringAssert.Contains(prompt, "Новый question_step запрещён");
    }

    [TestMethod]
    public void TryParseStep_RejectsUnknownDecisionDimension()
    {
        var json = ValidQuestionJson().Replace("task_type", "subject_detail", StringComparison.Ordinal);

        Assert.IsFalse(_service.TryParseStep(json, out _, out var error));
        StringAssert.Contains(error, "decisionDimension");
    }

    [TestMethod]
    public void TryParseStep_RejectsEmptySelectionImpact()
    {
        var json = ValidQuestionJson().Replace("[\"model_class\"]", "[]", StringComparison.Ordinal);

        Assert.IsFalse(_service.TryParseStep(json, out _, out var error));
        StringAssert.Contains(error, "selectionImpact");
    }

    [TestMethod]
    public void TryParseStep_RejectsWebToolsWhenNeedsWebIsFalse()
    {
        var json = ValidFinalJson()
            .Replace("\"needsWeb\":true", "\"needsWeb\":false", StringComparison.Ordinal);

        Assert.IsFalse(_service.TryParseStep(json, out _, out var error));
        StringAssert.Contains(error, "needsWeb=false");
    }

    [TestMethod]
    public void TryParseStep_RejectsWebWhenProfileForbidsExternalData()
    {
        var json = ValidFinalJson()
            .Replace("[\"research\"]", "[\"no_external_data\"]", StringComparison.Ordinal);

        Assert.IsFalse(_service.TryParseStep(json, out _, out var error));
        StringAssert.Contains(error, "forbids external data");
    }

    [TestMethod]
    public void ValidateProductivity_RejectsResolvedDimensionWithoutRevisitReason()
    {
        Assert.IsTrue(_service.TryParseStep(ValidQuestionJson(), out var step, out var parseError), parseError);
        var profile = new ChoiceCapabilityProfile
        {
            Dimensions =
            [
                new ChoiceCapabilityDimension
                {
                    Dimension = ChoiceDecisionDimensions.TaskType,
                    Status = ChoiceDimensionStatuses.Resolved,
                    Values = ["research"],
                    Evidence = "previous answer"
                }
            ]
        };

        Assert.IsFalse(_service.ValidateProductivity(step, profile, out var error));
        StringAssert.Contains(error, "already resolved");
    }

    [TestMethod]
    public void ValidateProductivity_AcceptsQuestionThatChangesExecutorProfile()
    {
        Assert.IsTrue(_service.TryParseStep(ValidQuestionJson(), out var step, out var parseError), parseError);

        Assert.IsTrue(_service.ValidateProductivity(step, new ChoiceCapabilityProfile(), out var error), error);
    }

    [TestMethod]
    public void ValidateProductivity_RejectsNarrowSubjectMatterInterview()
    {
        Assert.IsTrue(_service.TryParseStep(ValidQuestionJson(), out var step, out var parseError), parseError);
        step.Question = "Какой именно аспект государственного управления вас интересует?";

        Assert.IsFalse(_service.ValidateProductivity(step, new ChoiceCapabilityProfile(), out var error));
        StringAssert.Contains(error, "subject matter");
    }

    [TestMethod]
    public void ValidateProductivity_RequiresTaskTypeBeforeOtherDimensions()
    {
        var json = ValidQuestionJson()
            .Replace("task_type", "input_modality", StringComparison.Ordinal)
            .Replace("model_class", "multimodal_required", StringComparison.Ordinal);
        Assert.IsTrue(_service.TryParseStep(json, out var step, out var parseError), parseError);

        Assert.IsFalse(_service.ValidateProductivity(step, new ChoiceCapabilityProfile(), out var error));
        StringAssert.Contains(error, "task_type is unknown");
    }

    private static string ValidQuestionJson() => """
        {
          "stepType":"question_step",
          "question":"Что должна сделать модель?",
          "coreThought":"Уточняю операцию, влияющую на класс модели.",
          "decisionDimension":"task_type",
          "selectionImpact":["model_class"],
          "profileUpdate":[],
          "revisitReason":"",
          "options":[
            {"id":"explain","title":"Объяснить","description":"","isRecommended":false,"recommendationReason":"","profileEffects":[{"dimension":"task_type","status":"resolved","values":["explain"],"evidence":"selected explain"}]},
            {"id":"research","title":"Исследовать","description":"","isRecommended":false,"recommendationReason":"","profileEffects":[{"dimension":"task_type","status":"resolved","values":["research"],"evidence":"selected research"}]}
          ],
          "allowCustom":true,
          "isFinal":false,
          "summaryLines":[],
          "taskCard":null
        }
        """;

    private static string ValidFinalJson() => """
        {
          "stepType":"final_task_card","question":"Готово","coreThought":"Карточка готова.",
          "decisionDimension":"","selectionImpact":[],"profileUpdate":[],"revisitReason":"",
          "options":[],"allowCustom":false,"isFinal":true,"summaryLines":[],
          "taskCard":{
            "goal":"Исследовать вопрос","area":"Знания","criteria":[],"constraints":[],
            "needsWeb":true,"requiredTools":["web_research"],
            "capabilityProfile":{"dimensions":[
              {"dimension":"task_type","status":"resolved","values":["research"],"evidence":"choice"},
              {"dimension":"tool_requirements","status":"resolved","values":["research"],"evidence":"choice"}
            ]},
            "executorRole":"general_worker","executorCapabilityClass":"above_8b",
            "recommendedExecutor":"lab/ResearchModel-20B","executorStatus":"not_installed",
            "executorReason":"Подходит для исследования","promptForExecutor":"Проведи исследование"
          }
        }
        """;
}
