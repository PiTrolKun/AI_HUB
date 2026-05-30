namespace AIHub.Models;

public sealed class StorageLocationSettings
{
    public string Path { get; set; } = string.Empty;

    public double LimitGb { get; set; }
}
