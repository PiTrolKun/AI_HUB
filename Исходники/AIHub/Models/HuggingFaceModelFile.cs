namespace AIHub.Models;

public sealed class HuggingFaceModelFile
{
    public string FileName { get; set; } = string.Empty;

    public long? SizeBytes { get; set; }

    public string LfsOid { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;

    public bool MatchesFormat { get; set; }
}
