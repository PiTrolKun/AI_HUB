namespace AIHub.Models;

public sealed class WebPageReadResponse
{
    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public List<string> CandidateFileUrls { get; set; } = [];

    public string SavedPath { get; set; } = string.Empty;
}
