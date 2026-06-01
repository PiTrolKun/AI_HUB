namespace AIHub.Models;

public sealed class WebSearchRerankInfo
{
    public bool Applied { get; set; }

    public string Mode { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
