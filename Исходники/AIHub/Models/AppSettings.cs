namespace AIHub.Models;

public sealed class AppSettings
{
    public string LanguageCode { get; set; } = "ru";

    public bool LanguageWasChosen { get; set; }

    public CoreVoiceSettings CoreVoice { get; set; } = new();

    public CoreAutonomySettings CoreAutonomy { get; set; } = new();

    public ModelDownloadSettings ModelDownloads { get; set; } = new();

    public FileViewerSettings FileViewer { get; set; } = new();
}

public sealed class ModelDownloadSettings
{
    private int _maximumParallelConnections;

    public int MaximumParallelConnections
    {
        get => _maximumParallelConnections;
        set => _maximumParallelConnections = value is 1 or 2 or 4 or 8 ? value : 0;
    }
}

public sealed record ModelDownloadConnectionOption(int Value, string DisplayName);

public sealed class CoreAutonomySettings
{
    public const int MinimumSeconds = 30;
    public const int MaximumSeconds = 180;
    public const int DefaultSeconds = 90;

    private int _maximumIndependentSearchSeconds = DefaultSeconds;

    public int MaximumIndependentSearchSeconds
    {
        get => _maximumIndependentSearchSeconds;
        set => _maximumIndependentSearchSeconds = Math.Clamp(
            value,
            MinimumSeconds,
            MaximumSeconds);
    }
}

public sealed class FileViewerSettings
{
    public bool PreferInternalViewers { get; set; } = true;

    public Dictionary<string, bool> PreferInternalByExtension { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
