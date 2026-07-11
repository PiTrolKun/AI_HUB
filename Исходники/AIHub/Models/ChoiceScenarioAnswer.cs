namespace AIHub.Models;

public sealed class ChoiceScenarioAnswer
{
    public int StepNumber { get; set; }

    public string Question { get; set; } = string.Empty;

    public string OptionId { get; set; } = string.Empty;

    public string OptionTitle { get; set; } = string.Empty;

    public bool IsCustom { get; set; }

    public string DecisionDimension { get; set; } = string.Empty;

    public List<string> SelectionImpact { get; set; } = [];

    public List<ChoiceCapabilityDimension> AppliedProfileEffects { get; set; } = [];
}
