namespace AIHub.Models;

public sealed class CatalogStartupSyncResult
{
    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset? PreviousSyncUtc { get; set; }

    public DateTimeOffset? CurrentSyncUtc { get; set; }

    public int TrackedRepositoryCount { get; set; }

    public int UpdatedCount { get; set; }

    public int AddedCount { get; set; }
}
