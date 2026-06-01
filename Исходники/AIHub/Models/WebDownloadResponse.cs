namespace AIHub.Models;

public sealed class WebDownloadResponse
{
    public string Url { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string ContentKind { get; set; } = string.Empty;

    public bool IsHtmlPage { get; set; }

    public bool IsImage { get; set; }

    public bool ExtensionWasAdded { get; set; }

    public string Warning { get; set; } = string.Empty;
}
