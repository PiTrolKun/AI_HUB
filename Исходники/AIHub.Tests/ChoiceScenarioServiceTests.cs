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
    public void CreateFileSetupStep_PrecedesModelQuestionsAndHasFixedOptions()
    {
        var step = _service.CreateFileSetupStep(key => key);

        Assert.AreEqual(ChoiceScenarioService.FileSetupStepType, step.StepType);
        Assert.IsFalse(step.AllowCustom);
        CollectionAssert.AreEqual(
            new[]
            {
                ChoiceScenarioService.NoFilesOptionId,
                ChoiceScenarioService.SelectFilesOptionId
            },
            step.Options.Select(option => option.Id).ToArray());
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
    public void TryParseStep_AcceptsTrustedExecutorSelection()
    {
        const string json = """
            {
              "stepType":"final_task_card","question":"Готово","coreThought":"Карточка готова.",
              "decisionDimension":"","selectionImpact":[],"profileUpdate":[],"revisitReason":"",
              "options":[],"allowCustom":false,"isFinal":true,"summaryLines":[],
              "taskCard":{
                "goal":"Анализ данных","area":"Технологии","criteria":[],"constraints":[],
                "capabilityProfile":{"dimensions":[{"dimension":"task_type","status":"resolved","values":["data_analysis"],"evidence":"choice"}]},
                "executorSelection":{
                  "installedCandidateId":"installed_1","alternativeCandidateId":"alternative_1","preferredCandidateId":"alternative_1",
                  "installedAssessment":{"advantage":"Готова сразу","limitation":"Менее специализирована","reason":"Универсальная модель"},
                  "alternativeAssessment":{"advantage":"Лучше анализирует","limitation":"Нужна загрузка","reason":"Подходит для анализа"}
                },
                "promptForExecutor":"Уточни данные и проведи анализ"
              }
            }
            """;

        Assert.IsTrue(_service.TryParseStep(json, out var step, out var error), error);
        Assert.AreEqual("alternative_1", step.TaskCard?.ExecutorSelection.PreferredCandidateId);
    }

    [TestMethod]
    public void TryParseStep_RejectsPreferredCandidateOutsideTrustedPair()
    {
        var json = ValidFinalJson().Replace(
            "\"preferredCandidateId\":\"alternative_1\"",
            "\"preferredCandidateId\":\"alternative_9\"",
            StringComparison.Ordinal);

        Assert.IsFalse(_service.TryParseStep(json, out _, out var error));
        StringAssert.Contains(error, "preferredCandidateId");
    }

    [TestMethod]
    public void TryParseStep_AcceptsSingleAlternativeCandidate()
    {
        var json = ValidFinalJson().Replace(
            "\"installedCandidateId\":\"installed_1\"",
            "\"installedCandidateId\":\"\"",
            StringComparison.Ordinal);

        Assert.IsTrue(_service.TryParseStep(json, out _, out var error), error);
    }

    [TestMethod]
    public void TryParseStep_RejectsWhenBothCandidateIdsAreMissing()
    {
        var json = ValidFinalJson()
            .Replace(
                "\"installedCandidateId\":\"installed_1\"",
                "\"installedCandidateId\":\"\"",
                StringComparison.Ordinal)
            .Replace(
                "\"alternativeCandidateId\":\"alternative_1\"",
                "\"alternativeCandidateId\":\"\"",
                StringComparison.Ordinal);

        Assert.IsFalse(_service.TryParseStep(json, out _, out var error));
        StringAssert.Contains(error, "at least one trusted candidate");
    }

    [TestMethod]
    public void TryParseStep_DoesNotDependOnFreeFormCandidateAssessment()
    {
        var json = ValidFinalJson().Replace(
            "\"limitation\":\"Менее специализирована\"",
            "\"limitation\":\"\"",
            StringComparison.Ordinal);

        Assert.IsTrue(_service.TryParseStep(json, out _, out var error), error);
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
    public void ResponseSchema_UsesTrustedSelectionInsteadOfModelFacts()
    {
        var schema = ChoiceScenarioJsonContract.CreateResponseFormat().ToJsonString();

        StringAssert.Contains(schema, "executorSelection");
        Assert.IsFalse(schema.Contains("executorCandidates", StringComparison.Ordinal));
        Assert.IsFalse(schema.Contains("recommendedExecutor", StringComparison.Ordinal));
        Assert.IsFalse(schema.Contains("installedAssessment", StringComparison.Ordinal));
        Assert.IsFalse(schema.Contains("alternativeAssessment", StringComparison.Ordinal));
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
    public void BuildUserPrompt_DescribesFileUpdateAsContextNotAnswer()
    {
        var budget = new ChoiceScenarioStepBudget { Mode = "normal", MaximumSteps = 10 };
        var prompt = _service.BuildUserPrompt(
            [],
            requestFinal: false,
            mustReturnFinal: false,
            new UserContextSnapshot(),
            "inventory",
            "ru",
            budget,
            stepsUsed: 0,
            stepsRemaining: 10,
            capabilityProfile: new ChoiceCapabilityProfile(),
            fileManifest: new SessionFilePromptManifest
            {
                Intent = SessionFileIntentStatuses.Selected,
                FileCount = 1,
                ContentAccessAvailable = false,
                Files =
                [
                    new SessionFilePromptItem
                    {
                        Id = "safe-id",
                        Name = "report.docx",
                        Extension = ".docx",
                        Category = SessionFileCategories.Document,
                        IsAvailable = true
                    }
                ]
            },
            requestTrigger: "file_manifest_updated");

        StringAssert.Contains(prompt, "не отвечал на текущий вопрос");
        StringAssert.Contains(prompt, "contentAccessAvailable");
        StringAssert.Contains(prompt, "report.docx");
        StringAssert.Contains(prompt, "часть задачи, отдельный пример/эталон или поясняющий контекст");
        StringAssert.Contains(prompt, "пример/эталон; поясняющий материал; не учитывать");
        Assert.IsFalse(prompt.Contains(@":\\", StringComparison.Ordinal));
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
    public void TryParseStep_DoesNotRequireCoreToDeclareWebTools()
    {
        Assert.IsTrue(_service.TryParseStep(ValidFinalJson(), out var step, out var error), error);
        Assert.IsFalse(step.TaskCard?.NeedsWeb);
        Assert.AreEqual(0, step.TaskCard?.RequiredTools.Count);
    }

    [TestMethod]
    public void TryParseStep_AcceptsOfflineCapabilityWithoutTechnicalFields()
    {
        var json = ValidFinalJson()
            .Replace("[\"research\"]", "[\"no_external_data\"]", StringComparison.Ordinal);

        Assert.IsTrue(_service.TryParseStep(json, out _, out var error), error);
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
            "capabilityProfile":{"dimensions":[
              {"dimension":"task_type","status":"resolved","values":["research"],"evidence":"choice"},
              {"dimension":"tool_requirements","status":"resolved","values":["research"],"evidence":"choice"}
            ]},
            "executorSelection":{
              "installedCandidateId":"installed_1","alternativeCandidateId":"alternative_1","preferredCandidateId":"alternative_1",
              "installedAssessment":{"advantage":"Готова сразу","limitation":"Менее специализирована","reason":"Универсальная модель"},
              "alternativeAssessment":{"advantage":"Лучше исследует","limitation":"Нужна загрузка","reason":"Подходит для исследования"}
            },
            "promptForExecutor":"Проведи исследование"
          }
        }
        """;
}
