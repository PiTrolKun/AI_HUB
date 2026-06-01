namespace AIHub.Models;

public sealed class ToolModelFileManifest
{
    public string File { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}
