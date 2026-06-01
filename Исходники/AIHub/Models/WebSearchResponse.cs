namespace AIHub.Models;

public sealed class WebSearchResponse
{
    public string Query { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string SavedPath { get; set; } = string.Empty;

    public WebSearchRerankInfo Rerank { get; set; } = new();

    public List<WebSearchResult> Results { get; set; } = [];
}
