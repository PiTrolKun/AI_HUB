namespace AIHub.Models;

public sealed class ChoiceScenarioGenerationResult
{
    public ChoiceScenarioStep? Step { get; init; }

    public string RawResponse { get; init; } = string.Empty;

    public string Error { get; init; } = string.Empty;

    public int RepairAttempts { get; init; }

    public bool IsSuccess => Step is not null;
}
