using System.IO;
using AIHub.Models;

namespace AIHub.Services;

public static class WebToolPathService
{
    public static string GetWebRoot(StorageSettings storageSettings)
    {
        var resultsRoot = storageSettings.Results.Locations
            .Select(location => location.Path?.Trim())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        return string.IsNullOrWhiteSpace(resultsRoot)
            ? Path.Combine(AppDataPaths.RuntimeDirectory, "Tools", "Web")
            : Path.Combine(resultsRoot, "AI_HUB", "Tools", "Web");
    }

    public static string GetSearchDirectory(StorageSettings storageSettings) =>
        Ensure(Path.Combine(GetWebRoot(storageSettings), "Search"));

    public static string GetPagesDirectory(StorageSettings storageSettings) =>
        Ensure(Path.Combine(GetWebRoot(storageSettings), "Pages"));

    public static string GetDownloadsDirectory(StorageSettings storageSettings) =>
        Ensure(Path.Combine(GetWebRoot(storageSettings), "Downloads"));

    public static string CreateStampedPath(string directory, string prefix, string extension)
    {
        Directory.CreateDirectory(directory);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
        var safeExtension = extension.StartsWith('.') ? extension : "." + extension;
        return Path.Combine(directory, $"{timestamp}_{prefix}_{Guid.NewGuid():N}{safeExtension}");
    }

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
