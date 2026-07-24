using AIHub.Models;

namespace AIHub.Tests;

[TestClass]
public sealed class ChoiceScenarioSessionStateTests
{
    [TestMethod]
    public void Back_RestoresExactPreviousStepAndRemovesAnswer()
    {
        var start = Step("start_fixed_step", "Начало", "one");
        var next = Step("question_step", "Следующий вопрос", "two");
        var state = new ChoiceScenarioSessionState();
        state.Reset(start);
        Assert.IsTrue(state.TryAddAnswer(start.Options[0]));
        state.AddStep(next);

        var success = state.TryGoBack(out var restored);

        Assert.IsTrue(success);
        Assert.AreSame(start, restored);
        Assert.AreEqual(0, state.Answers.Count);
    }

    [TestMethod]
    public void Fingerprint_DetectsRepeatedQuestionAndOptions()
    {
        var first = Step("question_step", "Что важнее?", "price");
        var repeated = Step("question_step", "  что   важнее? ", "price");
        var state = new ChoiceScenarioSessionState();
        state.Reset(first);

        Assert.AreEqual(1, state.GetFingerprintCount(repeated));
    }

    [TestMethod]
    public void Back_FromManualFinal_DoesNotRemovePreviousAnswer()
    {
        var start = Step("start_fixed_step", "Начало", "one");
        var question = Step("question_step", "Уточнение", "two");
        var final = new ChoiceScenarioStep
        {
            StepType = "final_task_card",
            IsFinal = true,
            Question = "Финал"
        };
        var state = new ChoiceScenarioSessionState();
        state.Reset(start);
        state.TryAddAnswer(start.Options[0]);
        state.AddStep(question);
        state.AddStep(final, consumedAnswer: false);

        state.TryGoBack(out var restored);

        Assert.AreSame(question, restored);
        Assert.AreEqual(1, state.Answers.Count);
    }

    [TestMethod]
    public void SemanticLoop_DetectsRepeatedQuestionPattern()
    {
        var state = new ChoiceScenarioSessionState();
        state.Reset(Step("start_fixed_step", "Начало", "one"));
        state.AddStep(Step("question_step", "Какой конкретный аспект технологий?", "a"));
        state.AddStep(Step("question_step", "Какой конкретный аспект будущего?", "b"));
        state.AddStep(Step("question_step", "Какой конкретный аспект системы?", "c"));

        Assert.IsTrue(state.IsSemanticLoop(Step("question_step", "Какой конкретный аспект проекта?", "d")));
    }

    [TestMethod]
    public void StepBudget_CountsOnlySubstantiveAnswers()
    {
        var state = new ChoiceScenarioSessionState();
        state.Reset(Step("budget_setup", "Глубина", "budget_4"));
        ChoiceScenarioStepBudget.TryCreate("budget_4", out var budget);
        state.ConfigureStepBudget(budget);
        state.AddStep(Step("start_fixed_step", "Область", "knowledge"), consumedAnswer: false);

        state.TryAddAnswer(state.CurrentStep!.Options[0]);

        Assert.AreEqual(1, state.SubstantiveStepsUsed);
        Assert.AreEqual(3, state.StepsRemaining);
        Assert.IsFalse(state.IsStepBudgetExhausted);
    }

    [TestMethod]
    public void AutomaticBudget_HasSafetyLimit()
    {
        Assert.IsTrue(ChoiceScenarioStepBudget.TryCreate("budget_auto", out var budget));
        Assert.IsTrue(budget.IsAutomatic);
        Assert.AreEqual(ChoiceScenarioStepBudget.AutomaticSafetyLimit, budget.MaximumSteps);
    }

    [TestMethod]
    public void SelectedOption_AppliesCapabilityProfileAndBackRestoresSnapshot()
    {
        var start = Step("start_fixed_step", "Область", "knowledge");
        start.DecisionDimension = ChoiceDecisionDimensions.DomainSpecialization;
        start.Options[0].ProfileEffects =
        [
            new ChoiceCapabilityDimension
            {
                Dimension = ChoiceDecisionDimensions.DomainSpecialization,
                Status = ChoiceDimensionStatuses.Provisional,
                Values = ["knowledge"],
                Evidence = "selection"
            }
        ];
        var state = new ChoiceScenarioSessionState();
        state.Reset(start);

        state.TryAddAnswer(start.Options[0]);
        state.AddStep(Step("question_step", "Операция", "research"));
        Assert.AreEqual(ChoiceDimensionStatuses.Provisional, state.CapabilityProfile.GetStatus(ChoiceDecisionDimensions.DomainSpecialization));

        state.TryGoBack(out _);

        Assert.AreEqual(ChoiceDimensionStatuses.Unknown, state.CapabilityProfile.GetStatus(ChoiceDecisionDimensions.DomainSpecialization));
        Assert.AreEqual(0, state.Answers.Count);
    }

    [TestMethod]
    public void SubjectMatterOverreach_BlocksSecondConsecutiveDomainQuestion()
    {
        var state = new ChoiceScenarioSessionState();
        state.Reset(Step("start_fixed_step", "Область", "knowledge"));
        var first = Step("question_step", "Какая широкая специализация?", "medicine");
        first.DecisionDimension = ChoiceDecisionDimensions.DomainSpecialization;
        state.AddStep(first);
        var second = Step("question_step", "Какой раздел медицины?", "therapy");
        second.DecisionDimension = ChoiceDecisionDimensions.DomainSpecialization;

        Assert.IsTrue(state.IsSubjectMatterOverreach(second));
    }

    [TestMethod]
    public void Checkpoint_RestoresCurrentStepAnswersBudgetAndBackHistory()
    {
        var state = new ChoiceScenarioSessionState();
        var start = Step("budget_setup", "Глубина", "budget_4");
        state.Reset(start);
        ChoiceScenarioStepBudget.TryCreate("budget_4", out var budget);
        state.ConfigureStepBudget(budget);
        var domain = Step("start_fixed_step", "Область", "knowledge");
        state.AddStep(domain, consumedAnswer: false);
        state.TryAddAnswer(domain.Options[0]);
        var next = Step("question_step", "Тип работы", "research");
        state.AddStep(next);

        var restored = new ChoiceScenarioSessionState();
        restored.Restore(state.CreateCheckpoint());

        Assert.AreEqual("Тип работы", restored.CurrentStep?.Question);
        Assert.AreEqual(1, restored.Answers.Count);
        Assert.AreEqual(4, restored.StepBudget?.MaximumSteps);
        Assert.IsTrue(restored.TryGoBack(out var previous));
        Assert.AreEqual("Область", previous?.Question);
        Assert.AreEqual(0, restored.Answers.Count);
    }

    [TestMethod]
    public void TrustedFileProfileUpdate_DoesNotConsumeAnswerAndSurvivesBack()
    {
        var state = new ChoiceScenarioSessionState();
        state.Reset(Step("question_step", "Текущий вопрос", "one"));
        state.ApplyTrustedProfileUpdate(
        [
            new ChoiceCapabilityDimension
            {
                Dimension = ChoiceDecisionDimensions.InputModality,
                Status = ChoiceDimensionStatuses.Resolved,
                Values = ["file:document"],
                Evidence = "trusted file manifest"
            }
        ]);
        state.AddStep(Step("question_step", "Новый вопрос", "two"), consumedAnswer: false);

        Assert.AreEqual(0, state.SubstantiveStepsUsed);
        Assert.IsTrue(state.TryGoBack(out _));
        Assert.AreEqual(
            ChoiceDimensionStatuses.Resolved,
            state.CapabilityProfile.GetStatus(ChoiceDecisionDimensions.InputModality));
        Assert.AreEqual(0, state.Answers.Count);
    }

    [TestMethod]
    public void CustomAnswer_ResolvesCurrentDecisionDimension()
    {
        var step = Step("question_step", "Какую операцию выполнить?", "known");
        step.DecisionDimension = ChoiceDecisionDimensions.TaskType;
        var state = new ChoiceScenarioSessionState();
        state.Reset(step);

        Assert.IsTrue(state.TryAddAnswer(new ChoiceScenarioOption
        {
            Id = "custom",
            Title = "Повышение разрешения"
        }));

        var dimension = state.CapabilityProfile.Dimensions.Single();
        Assert.AreEqual(ChoiceDecisionDimensions.TaskType, dimension.Dimension);
        Assert.AreEqual(ChoiceDimensionStatuses.Resolved, dimension.Status);
        CollectionAssert.AreEqual(new[] { "Повышение разрешения" }, dimension.Values.ToArray());
        Assert.AreEqual("user custom input", dimension.Evidence);
    }

    private static ChoiceScenarioStep Step(string type, string question, string optionId) => new()
    {
        StepType = type,
        Question = question,
        Options = [new ChoiceScenarioOption { Id = optionId, Title = optionId }]
    };
}
