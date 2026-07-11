namespace AIHub.Models;

public sealed class ChoiceScenarioStep
{
    public string StepType { get; set; } = "question_step";

    public string Question { get; set; } = string.Empty;

    public string CoreThought { get; set; } = string.Empty;

    public string DecisionDimension { get; set; } = string.Empty;

    public List<string> SelectionImpact { get; set; } = [];

    public List<ChoiceCapabilityDimension> ProfileUpdate { get; set; } = [];

    public string RevisitReason { get; set; } = string.Empty;

    public List<ChoiceScenarioOption> Options { get; set; } = [];

    public bool AllowCustom { get; set; } = true;

    public bool IsFinal { get; set; }

    public List<string> SummaryLines { get; set; } = [];

    public ChoiceTaskCard? TaskCard { get; set; }
}
