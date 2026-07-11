namespace AIHub.Models;

public sealed class HuggingFaceCatalogDatabase
{
    public int SchemaVersion { get; set; } = 2;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }

    public List<HuggingFaceCatalogRecord> Records { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}

public sealed class HuggingFaceCatalogRecord
{
    public HuggingFaceCatalogEntry Entry { get; set; } = new();

    public List<HuggingFaceCatalogSeedSlot> SeedSlots { get; set; } = [];

    public List<string> CatalogDirections { get; set; } = [];

    public bool IsRadarDiscovery { get; set; }

    public bool IsNewAuthor { get; set; }

    public List<string> DiscoverySources { get; set; } = [];

    public double DiscoveryScore { get; set; }

    public DateTimeOffset FirstSeenUtc { get; set; }

    public DateTimeOffset LastSeenUtc { get; set; }

    public DateTimeOffset? LastSuccessfulCheckUtc { get; set; }

    public string PreviousRevisionSha { get; set; } = string.Empty;

    public int RevisionUpdateCount { get; set; }

    public bool IsAvailable { get; set; } = true;

    public string LastError { get; set; } = string.Empty;
}

public sealed class HuggingFaceCatalogChange
{
    public DateTimeOffset OccurredAtUtc { get; set; }

    public string RepoId { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string PreviousRevisionSha { get; set; } = string.Empty;

    public string CurrentRevisionSha { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}

public sealed class HuggingFaceCatalogSyncResult
{
    public string CatalogPath { get; set; } = string.Empty;

    public string ChangesPath { get; set; } = string.Empty;

    public int SeedSlotCount { get; set; }

    public int TrackedRepositoryCount { get; set; }

    public int AddedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int UnchangedCount { get; set; }

    public int UnavailableCount { get; set; }

    public int RadarCandidateCount { get; set; }

    public int RadarAddedCount { get; set; }

    public int RadarRemovedCount { get; set; }

    public int RadarRejectedCount { get; set; }

    public List<string> Warnings { get; set; } = [];
}
