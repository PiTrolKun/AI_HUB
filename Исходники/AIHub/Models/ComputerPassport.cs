namespace AIHub.Models;

public sealed class ComputerPassport
{
    public DateTimeOffset CreatedAt { get; set; }

    public string MachineName { get; set; } = "unknown";

    public string WindowsVersion { get; set; } = "unknown";

    public string OperatingSystemArchitecture { get; set; } = "unknown";

    public string UserName { get; set; } = "unknown";

    public string CpuName { get; set; } = "unknown";

    public double RamTotalGb { get; set; }

    public List<GpuPassport> Gpus { get; set; } = [];

    public List<DrivePassport> Drives { get; set; } = [];
}
