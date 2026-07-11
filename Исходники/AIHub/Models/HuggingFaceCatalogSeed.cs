namespace AIHub.Models;

public sealed class HuggingFaceCatalogSeed
{
    public int SchemaVersion { get; set; } = 1;

    public HuggingFaceRadarSettings Radar { get; set; } = new();

    public List<HuggingFaceCatalogSeedSlot> Slots { get; set; } = [];
}

public sealed class HuggingFaceCatalogSeedSlot
{
    public string Direction { get; set; } = string.Empty;

    public int Slot { get; set; }

    public string LoadLevel { get; set; } = string.Empty;

    public string RepoId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsManualException { get; set; }
}

public sealed class HuggingFaceRadarSettings
{
    public int LookbackDays { get; set; } = 365;

    public int QueryLimit { get; set; } = 50;

    public int MaximumNewEntriesPerSync { get; set; } = 30;

    public long MinimumParameterCountExclusive { get; set; } = 8_000_000_000;

    public long MinimumDownloads { get; set; } = 1_000;

    public long MinimumLikes { get; set; } = 25;

    public int AutomaticTrendingRankLimit { get; set; } = 10;

    public List<string> SupportedPipelineTags { get; set; } = [];
}
