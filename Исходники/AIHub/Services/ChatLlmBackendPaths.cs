using System.IO;

namespace AIHub.Services;

public static class ChatLlmBackendPaths
{
    public const string Release = "v24";
    public const string Platform = "win-x64";

    public static string DirectoryPath => Path.Combine(
        AppDataPaths.BackendsDirectory,
        "chatllm.cpp",
        Release,
        Platform);

    public static string ServerExecutablePath => Path.Combine(DirectoryPath, "server.exe");

    public static string ImageMagickDirectoryPath => Path.Combine(DirectoryPath, "imagemagick");

    public static string ImageMagickExecutablePath => Path.Combine(ImageMagickDirectoryPath, "magick.exe");

    public static string DisplayName => $"chatllm.cpp {Release}";
}
