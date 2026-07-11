namespace AIHub.Models;

public sealed class HuggingFaceCatalogSnapshot
{
    public int SchemaVersion { get; set; } = 1;

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public string Query { get; set; } = string.Empty;

    public string SearchSourceUrl { get; set; } = string.Empty;

    public string RawSearchRelativePath { get; set; } = string.Empty;

    public string RawSearchSha256 { get; set; } = string.Empty;

    public List<HuggingFaceCatalogEntry> Entries { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}
