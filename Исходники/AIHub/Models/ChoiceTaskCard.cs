namespace AIHub.Models;

public sealed class ChoiceTaskCard
{
    public string Goal { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public List<string> Criteria { get; set; } = [];

    public List<string> Constraints { get; set; } = [];

    public bool NeedsWeb { get; set; }

    public List<string> RequiredTools { get; set; } = [];

    public ChoiceCapabilityProfile CapabilityProfile { get; set; } = new();

    public string ExecutorRole { get; set; } = string.Empty;

    public string ExecutorCapabilityClass { get; set; } = string.Empty;

    public string RecommendedExecutor { get; set; } = string.Empty;

    public string ExecutorStatus { get; set; } = string.Empty;

    public string ExecutorReason { get; set; } = string.Empty;

    public string PromptForExecutor { get; set; } = string.Empty;
}
