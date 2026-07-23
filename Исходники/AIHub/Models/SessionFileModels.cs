namespace AIHub.Models;

public static class SessionFileIntentStatuses
{
    public const string Unknown = "unknown";
    public const string None = "none";
    public const string Selected = "selected";
}

public sealed class SessionFileManifest
{
    public int SchemaVersion { get; set; } = 1;

    public string Intent { get; set; } = SessionFileIntentStatuses.Unknown;

    public List<SessionFileReference> Files { get; set; } = [];
}

public sealed class SessionFileReference
{
    public string Id { get; set; } = string.Empty;

    public string SourcePath { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string Category { get; set; } = SessionFileCategories.Unknown;

    public long SizeBytes { get; set; }

    public bool IsAvailable { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    public DateTimeOffset LastCheckedAt { get; set; }
}

public static class SessionFileCategories
{
    public const string Document = "document";
    public const string Table = "table";
    public const string Image = "image";
    public const string Code = "code";
    public const string Text = "text";
    public const string Archive = "archive";
    public const string Audio = "audio";
    public const string Video = "video";
    public const string Unknown = "unknown";
}

public sealed class SessionFilePromptManifest
{
    public string Intent { get; set; } = SessionFileIntentStatuses.Unknown;

    public int FileCount { get; set; }

    public long TotalSizeBytes { get; set; }

    public bool ContentAccessAvailable { get; set; }

    public List<string> RequiredCapabilities { get; set; } = [];

    public List<SessionFilePromptItem> Files { get; set; } = [];
}

public sealed class SessionFilePromptItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Extension { get; set; } = string.Empty;

    public string Category { get; set; } = SessionFileCategories.Unknown;

    public long SizeBytes { get; set; }

    public bool IsAvailable { get; set; }
}

public sealed class SessionFileCardViewModel
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Details { get; init; } = string.Empty;

    public string AvailabilityText { get; init; } = string.Empty;

    public bool IsAvailable { get; init; }

    public string RemoveTooltip { get; init; } = string.Empty;
}
