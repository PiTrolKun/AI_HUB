namespace AIHub.Models;

public sealed class AppSettings
{
    public string LanguageCode { get; set; } = "ru";

    public bool LanguageWasChosen { get; set; }

    public CoreVoiceSettings CoreVoice { get; set; } = new();

    public FileViewerSettings FileViewer { get; set; } = new();
}

public sealed class FileViewerSettings
{
    public bool PreferInternalViewers { get; set; } = true;

    public Dictionary<string, bool> PreferInternalByExtension { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}
