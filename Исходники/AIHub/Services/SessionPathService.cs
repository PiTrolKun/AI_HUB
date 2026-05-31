using System.IO;
using AIHub.Models;

namespace AIHub.Services;

public static class SessionPathService
{
    public static string GetCoreSessionsDirectory(StorageSettings storageSettings)
    {
        var resultsRoot = GetDefaultResultsRoot(storageSettings);
        return resultsRoot is null
            ? Path.Combine(AppDataPaths.RuntimeDirectory, "Core", "Sessions")
            : Path.Combine(resultsRoot, "AI_HUB", "Core", "Sessions");
    }

    public static string GetDebugModelTesterSessionsDirectory(StorageSettings storageSettings)
    {
        var resultsRoot = GetDefaultResultsRoot(storageSettings);
        return resultsRoot is null
            ? Path.Combine(AppDataPaths.RuntimeDirectory, "Debug", "ModelTester", "Sessions")
            : Path.Combine(resultsRoot, "AI_HUB", "Debug", "ModelTester", "Sessions");
    }

    public static string CreateSessionFilePath(string directory, string suffix)
    {
        Directory.CreateDirectory(directory);
        var safeSuffix = string.IsNullOrWhiteSpace(suffix) ? "session" : suffix;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(directory, $"{timestamp}_{safeSuffix}_{Guid.NewGuid():N}.jsonl");
    }

    private static string? GetDefaultResultsRoot(StorageSettings storageSettings)
    {
        var configuredPath = storageSettings.Results.Locations
            .Select(location => location.Path?.Trim())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        return string.IsNullOrWhiteSpace(configuredPath) ? null : configuredPath;
    }
}
