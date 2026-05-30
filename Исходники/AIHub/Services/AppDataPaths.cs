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

    public static void EnsureBaseDirectory()
    {
        Directory.CreateDirectory(BaseDirectory);
    }
}
