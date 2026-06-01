namespace AIHub.Models;

public sealed class WebResearchResponse
{
    public string Task { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Diagnosis { get; set; } = string.Empty;

    public int ConfirmedSourceCount { get; set; }

    public string SavedPath { get; set; } = string.Empty;

    public List<string> GeneratedQueries { get; set; } = [];

    public List<WebResearchAttempt> Attempts { get; set; } = [];

    public List<WebResearchSource> Sources { get; set; } = [];

    public List<WebResearchPage> ReadPages { get; set; } = [];

    public List<WebResearchDatedItem> DatedItems { get; set; } = [];

    public List<string> RecommendedNextSteps { get; set; } = [];
}
