namespace AIHub.Models;

public sealed class CapabilityInventoryItem
{
    public string Role { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool IsInstalled { get; set; }

    public bool IsRunnable { get; set; }

    public string Format { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public string SemanticDescriptionRu { get; set; } = string.Empty;

    public string SemanticDescriptionEn { get; set; } = string.Empty;
}
