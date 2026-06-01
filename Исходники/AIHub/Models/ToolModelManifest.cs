namespace AIHub.Models;

public sealed class ToolModelManifest
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = "tool";

    public string ToolKind { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string SourceRepository { get; set; } = string.Empty;

    public string SourceCommit { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public string Status { get; set; } = "missing";

    public long DownloadedBytes { get; set; }

    public long TotalBytes { get; set; }

    public List<ToolModelFileManifest> Files { get; set; } = [];

    public DateTimeOffset? VerifiedAt { get; set; }
}
