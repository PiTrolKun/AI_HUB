namespace AIHub.Models;

public sealed class CoreModelDownloadProgress
{
    public long DownloadedBytes { get; set; }

    public long TotalBytes { get; set; }

    public double BytesPerSecond { get; set; }

    public string Stage { get; set; } = "downloading";
}
