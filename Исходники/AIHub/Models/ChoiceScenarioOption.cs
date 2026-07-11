namespace AIHub.Models;

public sealed class ChoiceScenarioOption
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsRecommended { get; set; }

    public string RecommendationReason { get; set; } = string.Empty;

    public List<ChoiceCapabilityDimension> ProfileEffects { get; set; } = [];
}
