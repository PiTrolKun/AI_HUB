using System.IO;

namespace AIHub.Services;

public sealed record CoreVoiceRuntime(string DirectoryPath, string LibraryPath, string DataPath);

public sealed class CoreVoiceRuntimeLocator
{
    public const string RuntimeVersion = "1.52.0";

    public CoreVoiceRuntime? Find()
    {
        foreach (var directory in CandidateDirectories())
        {
            var libraryPath = Path.Combine(directory, "libespeak-ng.dll");
            var dataPath = Path.Combine(directory, "espeak-ng-data");
            if (File.Exists(libraryPath) && Directory.Exists(dataPath))
            {
                return new CoreVoiceRuntime(directory, libraryPath, dataPath);
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateDirectories()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "VoiceRuntime", "eSpeakNG");

        var projectRuntime = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "Runtime", "Voice", "eSpeakNG", RuntimeVersion, "extracted", "eSpeak NG"));
        yield return projectRuntime;
    }
}
