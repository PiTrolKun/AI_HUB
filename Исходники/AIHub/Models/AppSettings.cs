namespace AIHub.Models;

public sealed class AppSettings
{
    public string LanguageCode { get; set; } = "ru";

    public bool LanguageWasChosen { get; set; }

    public CoreVoiceSettings CoreVoice { get; set; } = new();
}
