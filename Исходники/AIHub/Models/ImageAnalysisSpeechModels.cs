namespace AIHub.Models;

public static class ImageAnalysisSpeechModes
{
    public const string Off = "off";
    public const string Omni = "omni";
    public const string Kokoro = "kokoro";
    public const string Programmatic = "programmatic";

    public static string Normalize(string? mode) => mode switch
    {
        Kokoro => Kokoro,
        Programmatic => Programmatic,
        _ => Off
    };

    public static string Next(string? mode) => Normalize(mode) switch
    {
        Off => Kokoro,
        Kokoro => Programmatic,
        _ => Off
    };

    public static string NormalizeHeavy(string? mode) => mode switch
    {
        // Omni => Omni, // Retired from image analysis: Russian Talker is unusable.
        // Migrate saved Omni selections to Kokoro without losing the dormant speaker settings.
        Kokoro => Kokoro,
        Programmatic => Programmatic,
        Off => Off,
        _ => Kokoro
    };
}

public static class ImageAnalysisOmniSpeakers
{
    public const string Ethan = "Ethan";
    public const string Chelsie = "Chelsie";

    public static string Normalize(string? speaker) => speaker switch
    {
        Chelsie => Chelsie,
        _ => Ethan
    };
}

public sealed class ImageAnalysisHeavySpeechSettings
{
    private string _mode = ImageAnalysisSpeechModes.Kokoro;
    private string _omniSpeaker = ImageAnalysisOmniSpeakers.Ethan;
    private int _omniVolume = 100;
    private int _omniRatePercent = 100;
    private int _kokoroVolume = 100;
    private int _kokoroRatePercent = 100;
    private int _programmaticVolume = 100;
    private int _programmaticRatePercent = 100;

    public string Mode { get => _mode; set => _mode = ImageAnalysisSpeechModes.NormalizeHeavy(value); }
    public string OmniSpeaker { get => _omniSpeaker; set => _omniSpeaker = ImageAnalysisOmniSpeakers.Normalize(value); }
    public int OmniVolume { get => _omniVolume; set => _omniVolume = Math.Clamp(value, 0, 100); }
    public int OmniRatePercent { get => _omniRatePercent; set => _omniRatePercent = Math.Clamp(value, 70, 160); }
    public int KokoroVolume { get => _kokoroVolume; set => _kokoroVolume = Math.Clamp(value, 0, 100); }
    public int KokoroRatePercent { get => _kokoroRatePercent; set => _kokoroRatePercent = Math.Clamp(value, 70, 160); }
    public int ProgrammaticVolume { get => _programmaticVolume; set => _programmaticVolume = Math.Clamp(value, 0, 100); }
    public int ProgrammaticRatePercent { get => _programmaticRatePercent; set => _programmaticRatePercent = Math.Clamp(value, 70, 160); }

    public int GetActiveVolume() => Mode switch
    {
        ImageAnalysisSpeechModes.Omni => OmniVolume,
        ImageAnalysisSpeechModes.Kokoro => KokoroVolume,
        _ => ProgrammaticVolume
    };

    public int GetActiveRatePercent() => Mode switch
    {
        ImageAnalysisSpeechModes.Omni => OmniRatePercent,
        ImageAnalysisSpeechModes.Kokoro => KokoroRatePercent,
        _ => ProgrammaticRatePercent
    };
}

public sealed class ImageAnalysisSpeechSettings
{
    private string _mode = ImageAnalysisSpeechModes.Off;
    private int _kokoroVolume = 100;
    private int _kokoroRatePercent = 100;
    private int _programmaticVolume = 100;
    private int _programmaticRatePercent = 100;

    public string Mode
    {
        get => _mode;
        set => _mode = ImageAnalysisSpeechModes.Normalize(value);
    }

    public int KokoroVolume
    {
        get => _kokoroVolume;
        set => _kokoroVolume = Math.Clamp(value, 0, 100);
    }

    public int KokoroRatePercent
    {
        get => _kokoroRatePercent;
        set => _kokoroRatePercent = Math.Clamp(value, 70, 160);
    }

    public int ProgrammaticVolume
    {
        get => _programmaticVolume;
        set => _programmaticVolume = Math.Clamp(value, 0, 100);
    }

    public int ProgrammaticRatePercent
    {
        get => _programmaticRatePercent;
        set => _programmaticRatePercent = Math.Clamp(value, 70, 160);
    }

    public int GetActiveVolume() => Mode == ImageAnalysisSpeechModes.Kokoro
        ? KokoroVolume
        : ProgrammaticVolume;

    public int GetActiveRatePercent() => Mode == ImageAnalysisSpeechModes.Kokoro
        ? KokoroRatePercent
        : ProgrammaticRatePercent;
}

public sealed record ImageAnalysisSpeechMemoryDecision(
    bool HasEnoughMemory,
    long AvailableBytes,
    long ExpectedRuntimeBytes,
    long PendingAllocationBytes,
    long SafetyReserveBytes,
    long RequiredBytes);

public static class KokoroWarmupCodes
{
    public const string Ready = "ready";
    public const string AlreadyReady = "already_ready";
    public const string ModelMissing = "model_missing";
    public const string RuntimeMissing = "runtime_missing";
    public const string InsufficientMemory = "insufficient_memory";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}

public static class KokoroSpeechStages
{
    public const string Warming = "warming";
    public const string Synthesizing = "synthesizing";
    public const string Playing = "playing";
}

public sealed record KokoroSpeechProgress(string Stage);

public sealed record KokoroWarmupResult(
    string Code,
    ImageAnalysisSpeechMemoryDecision? Memory = null,
    long LoadMilliseconds = 0,
    long PeakWorkingSetBytes = 0,
    string Error = "",
    string ErrorStage = "",
    string ErrorType = "",
    string StandardErrorTail = "",
    double AverageCpuPercent = 0,
    double PeakCpuPercent = 0)
{
    public bool IsReady => Code is KokoroWarmupCodes.Ready or KokoroWarmupCodes.AlreadyReady;
}

public sealed record KokoroSpeechResult(
    bool Completed,
    string Code,
    long GenerationMilliseconds = 0,
    long TimeToFirstAudioMilliseconds = 0,
    long PeakWorkingSetBytes = 0,
    double CpuMilliseconds = 0,
    string Error = "",
    string ErrorStage = "",
    string ErrorType = "",
    string StandardErrorTail = "",
    double AverageCpuPercent = 0,
    double PeakCpuPercent = 0,
    string AudioPath = "");

public sealed record KokoroSpeechMetric(
    DateTimeOffset CreatedAt,
    string LanguageCode,
    string Operation,
    bool ColdStart,
    long LoadMilliseconds,
    long GenerationMilliseconds,
    long TimeToFirstAudioMilliseconds,
    long PeakWorkingSetBytes,
    double CpuMilliseconds,
    bool Succeeded,
    string ErrorCode,
    string ErrorStage = "",
    string ErrorType = "",
    string Error = "",
    double AverageCpuPercent = 0,
    double PeakCpuPercent = 0,
    string Diagnostics = "");
