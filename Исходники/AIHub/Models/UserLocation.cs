namespace AIHub.Models;

public sealed class UserLocation
{
    public string Mode { get; set; } = "auto";

    public string City { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string Timezone { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string Source { get; set; } = "ip";

    public DateTimeOffset? UpdatedAt { get; set; }
}
