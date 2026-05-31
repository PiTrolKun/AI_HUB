namespace AIHub.Models;

public sealed class CoreModelCheckResult
{
    public CoreModelAvailability Availability { get; set; }

    public string? ModelsRoot { get; set; }

    public string? ModelDirectory { get; set; }

    public string? ModelPath { get; set; }

    public string? PartialPath { get; set; }

    public string? ManifestPath { get; set; }

    public long ExistingBytes { get; set; }

    public long RequiredBytes { get; set; }

    public long FreeBytes { get; set; }

    public bool HasEnoughSpace { get; set; } = true;
}
