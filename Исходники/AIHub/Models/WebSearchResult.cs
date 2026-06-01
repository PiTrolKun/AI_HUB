namespace AIHub.Models;

public sealed class WebSearchResult
{
    public int OriginalRank { get; set; }

    public int RerankedRank { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public double? RerankScore { get; set; }
}
