using System.Globalization;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public static partial class ModelHardwareCompatibilityService
{
    private const double BytesPerQ4Parameter = 0.5;
    private const double RuntimeOverheadMultiplier = 1.20;
    private const double BytesInGb = 1024d * 1024d * 1024d;

    public static ModelHardwareCompatibility Assess(
        long? parameterCount,
        ComputerPassport passport,
        string workloadMode)
    {
        ArgumentNullException.ThrowIfNull(passport);
        var result = new ModelHardwareCompatibility
        {
            AvailableRamGb = passport.RamTotalGb,
            AvailableVramGb = passport.Gpus.Select(gpu => gpu.VramGb).DefaultIfEmpty(0).Max(),
            LargestFreeDriveGb = passport.Drives.Select(drive => drive.FreeGb).DefaultIfEmpty(0).Max()
        };
        if (parameterCount is null or <= 0)
        {
            result.Reason = "Parameter count is unknown, so hardware fit cannot be verified.";
            return result;
        }

        var estimatedGb = parameterCount.Value * BytesPerQ4Parameter * RuntimeOverheadMultiplier / BytesInGb;
        result.EstimatedQ4RuntimeGb = Math.Round(estimatedGb, 2);
        var normalizedMode = NormalizeMode(workloadMode);
        var ramFactor = normalizedMode switch
        {
            "light" => 0.60,
            "extreme" => 0.90,
            _ => 0.75
        };
        var vramFactor = normalizedMode switch
        {
            "light" => 0.80,
            "extreme" => 0.95,
            _ => 0.90
        };
        var usableRam = result.AvailableRamGb * ramFactor;
        var usableVram = result.AvailableVramGb * vramFactor;
        var diskRequired = estimatedGb * 0.90;
        if (result.LargestFreeDriveGb > 0 && diskRequired > result.LargestFreeDriveGb)
        {
            result.Status = "not_fit";
            result.IsCompatible = false;
            result.Reason = $"Estimated Q4 file needs about {diskRequired:0.##} GB, exceeding the largest known free drive space.";
            return result;
        }

        if (estimatedGb <= usableVram && usableVram > 0)
        {
            result.Status = "gpu_fit";
            result.IsCompatible = true;
            result.Reason = "Estimated Q4 runtime memory fits the selected workload VRAM budget.";
            return result;
        }

        if (estimatedGb <= usableRam + usableVram)
        {
            result.Status = usableVram > 0 ? "hybrid_fit" : "cpu_fit";
            result.IsCompatible = true;
            result.Reason = usableVram > 0
                ? "Estimated Q4 runtime memory fits combined RAM and VRAM budgets with offload."
                : "Estimated Q4 runtime memory fits the RAM budget.";
            return result;
        }

        result.Status = "not_fit";
        result.IsCompatible = false;
        result.Reason = $"Estimated Q4 runtime memory ({estimatedGb:0.##} GB) exceeds the {normalizedMode} RAM/VRAM budget ({usableRam + usableVram:0.##} GB).";
        return result;
    }

    public static long? TryReadParameterCountFromName(string value)
    {
        var match = ParameterCountRegex().Match(value ?? string.Empty);
        if (!match.Success
            || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var billions))
        {
            return null;
        }

        return (long)Math.Round(billions * 1_000_000_000d, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeMode(string value) => value?.Trim().ToLowerInvariant() switch
    {
        UserWorkloadModes.Light => "light",
        UserWorkloadModes.Extreme => "extreme",
        "optimal" or UserWorkloadModes.Balanced => "optimal",
        _ => "optimal"
    };

    [GeneratedRegex(@"(?<![\d.])(\d+(?:\.\d+)?)\s*[bB]\b", RegexOptions.CultureInvariant)]
    private static partial Regex ParameterCountRegex();
}
