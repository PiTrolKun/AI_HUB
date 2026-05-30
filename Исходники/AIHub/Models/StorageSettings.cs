namespace AIHub.Models;

public sealed class StorageSettings
{
    public StorageCategorySettings Models { get; set; } = new();

    public StorageCategorySettings Results { get; set; } = new();
}
