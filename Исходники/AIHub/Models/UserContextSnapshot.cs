namespace AIHub.Models;

public sealed class UserContextSnapshot
{
    public DateTimeOffset LocalTime { get; set; }

    public DateTimeOffset UtcTime { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public string TimeZoneDisplayName { get; set; } = string.Empty;

    public string UtcOffset { get; set; } = string.Empty;

    public UserLocation? Location { get; set; }

    public string WorkloadMode { get; set; } = UserWorkloadModes.Balanced;
}
