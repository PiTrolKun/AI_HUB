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

    public static void EnsureBaseDirectory()
    {
        Directory.CreateDirectory(BaseDirectory);
        Directory.CreateDirectory(LocalizationDirectory);
    }
}
