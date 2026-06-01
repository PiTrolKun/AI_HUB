namespace AIHub.Models;

public sealed class WebDownloadProgress
{
    public required string Url { get; init; }

    public required string FilePath { get; init; }

    public long DownloadedBytes { get; init; }

    public long? TotalBytes { get; init; }

    public double BytesPerSecond { get; init; }

    public bool IsComplete { get; init; }
}
