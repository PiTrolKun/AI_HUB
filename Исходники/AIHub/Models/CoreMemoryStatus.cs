namespace AIHub.Models;

public sealed class CoreMemoryStatus
{
    public bool IsActive { get; set; }

    public bool IsCompressing { get; set; }

    public bool HasCompressedSummary { get; set; }

    public int UsedUnits { get; set; }

    public int LimitUnits { get; set; } = CoreContextRuntimeLimits.Qwen3EightBNativeContextLimit;

    public double FillPercent => LimitUnits <= 0
        ? 0
        : Math.Clamp(UsedUnits * 100d / LimitUnits, 0, 100);

    public bool IsNearFull => FillPercent >= 86;

    public static CoreMemoryStatus Inactive() => new()
    {
        IsActive = false,
        UsedUnits = 0,
        LimitUnits = CoreContextRuntimeLimits.Qwen3EightBNativeContextLimit
    };
}

public static class CoreContextRuntimeLimits
{
    public const int CurrentBackendContextLimit = Qwen3EightBNativeContextLimit;

    public const int Qwen3EightBNativeContextLimit = 32768;

    public const int Qwen3EightBYarnContextLimit = 131072;

    public const string Qwen3EightBOfficialSource = "https://huggingface.co/Qwen/Qwen3-8B";

    public const string Qwen3EightBGgufOfficialSource = "https://huggingface.co/Qwen/Qwen3-8B-GGUF";
}
