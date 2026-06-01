namespace AIHub.Models;

public sealed class WebResearchSource
{
    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public double? Score { get; set; }

    public bool WasRead { get; set; }
}
