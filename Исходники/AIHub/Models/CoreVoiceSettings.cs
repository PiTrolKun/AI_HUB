namespace AIHub.Models;

public sealed class CoreVoiceSettings
{
    public const string EspeakProvider = "espeak";

    public const string RhVoiceProvider = "rhvoice";

    public bool Enabled { get; set; } = true;

    public string Provider { get; set; } = EspeakProvider;

    public int Volume { get; set; } = 100;

    public int Rate { get; set; } = 120;

    public string RussianVoice { get; set; } = "ru";

    public string EnglishVoice { get; set; } = "en+f3";
}

public sealed record CoreVoiceProviderOption(string Id, string DisplayName)
{
    public override string ToString() => DisplayName;
}
