namespace AIHub.Models;

public sealed class StorageCategorySettings
{
    public double TotalLimitGb { get; set; }

    public bool AllowTemporaryOverflow { get; set; }

    public double TemporaryOverflowGb { get; set; }

    public List<StorageLocationSettings> Locations { get; set; } = [];
}
