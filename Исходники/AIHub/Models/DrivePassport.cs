namespace AIHub.Models;

public sealed class DrivePassport
{
    public string Name { get; set; } = "unknown";

    public string DriveType { get; set; } = "unknown";

    public double TotalGb { get; set; }

    public double FreeGb { get; set; }
}
