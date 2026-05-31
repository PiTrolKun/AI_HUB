using System.IO;

namespace AIHub.Services;

public static class AppDataPaths
{
    private const string AppFolderName = "AI_HUB";

    public static string BaseDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppFolderName);

    public static string StatePath { get; } = Path.Combine(BaseDirectory, "state.json");

    public static string ComputerPassportPath { get; } = Path.Combine(BaseDirectory, "computer-passport.json");

    public static string StorageSettingsPath { get; } = Path.Combine(BaseDirectory, "storage-settings.json");

    public static string SettingsPath { get; } = Path.Combine(BaseDirectory, "settings.json");

    public static string LocalizationDirectory { get; } = Path.Combine(BaseDirectory, "Localization");

    public static string? ProjectRoot { get; } = FindProjectRoot();

    public static string RuntimeDirectory { get; } = ProjectRoot is null
        ? Path.Combine(BaseDirectory, "Runtime")
        : Path.Combine(ProjectRoot, "Runtime");

    public static string BackendsDirectory { get; } = Path.Combine(RuntimeDirectory, "Backends");

    public static void EnsureBaseDirectory()
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(LocalizationDirectory);
    }

    private static string? FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VERSION"))
                && Directory.Exists(Path.Combine(directory.FullName, "Runtime")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
