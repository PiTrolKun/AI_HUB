namespace AIHub.Models;

public sealed class ChoiceScenarioStepBudget
{
    public const int AutomaticSafetyLimit = 30;

    public string Mode { get; init; } = string.Empty;

    public int MaximumSteps { get; init; }

    public bool IsAutomatic { get; init; }

    public static bool TryCreate(string optionId, out ChoiceScenarioStepBudget budget)
    {
        budget = optionId switch
        {
            "budget_4" => new ChoiceScenarioStepBudget { Mode = "quick", MaximumSteps = 4 },
            "budget_10" => new ChoiceScenarioStepBudget { Mode = "normal", MaximumSteps = 10 },
            "budget_20" => new ChoiceScenarioStepBudget { Mode = "detailed", MaximumSteps = 20 },
            "budget_auto" => new ChoiceScenarioStepBudget
            {
                Mode = "automatic",
                MaximumSteps = AutomaticSafetyLimit,
                IsAutomatic = true
            },
            _ => new ChoiceScenarioStepBudget()
        };
        return budget.MaximumSteps > 0;
    }
}
