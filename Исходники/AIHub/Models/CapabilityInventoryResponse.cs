namespace AIHub.Models;

public sealed class CapabilityInventoryResponse
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public List<CapabilityInventoryItem> Items { get; set; } = [];
}
