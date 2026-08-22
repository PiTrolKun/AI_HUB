using System.IO;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ImageAnalysisHardwareSnapshotService
{
    private const double BytesInGb = 1024d * 1024d * 1024d;

    public ImageAnalysisHardwareSnapshot Create(
        ComputerPassport passport,
        StorageSettings storageSettings)
    {
        ArgumentNullException.ThrowIfNull(passport);
        ArgumentNullException.ThrowIfNull(storageSettings);

        var maxVram = passport.Gpus
            .Where(gpu => gpu.VramGb > 0)
            .Select(gpu => gpu.VramGb)
            .DefaultIfEmpty(0)
            .Max();

        return new ImageAnalysisHardwareSnapshot
        {
            RamGb = passport.RamTotalGb > 0 ? passport.RamTotalGb : null,
            VramGb = maxVram > 0 ? maxVram : null,
            LogicalProcessorCount = passport.LogicalProcessorCount > 0
                ? passport.LogicalProcessorCount
                : null,
            FreeDiskGb = TryGetModelsStorageFreeSpace(storageSettings)
        };
    }

    private static double? TryGetModelsStorageFreeSpace(StorageSettings storageSettings)
    {
        var configuredPath = storageSettings.Models.Locations
            .Select(location => location.Path)
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return null;
        }

        try
        {
            var fullPath = Path.GetFullPath(configuredPath);
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            var drive = new DriveInfo(root);
            return drive.IsReady
                ? Math.Round(drive.AvailableFreeSpace / BytesInGb, 2)
                : null;
        }
        catch
        {
            return null;
        }
    }
}
