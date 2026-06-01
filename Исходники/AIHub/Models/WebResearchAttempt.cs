namespace AIHub.Models;

public sealed class WebResearchAttempt
{
    public string Query { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int ResultCount { get; set; }

    public int HttpStatusCode { get; set; }

    public string PossibleReason { get; set; } = string.Empty;

    public string SavedPath { get; set; } = string.Empty;
}
