using System.IO;
using System.Security.Cryptography;
using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public sealed class SessionFileManifestService
{
    private static readonly IReadOnlyDictionary<string, string> Categories =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = SessionFileCategories.Document,
            [".doc"] = SessionFileCategories.Document,
            [".docx"] = SessionFileCategories.Document,
            [".odt"] = SessionFileCategories.Document,
            [".rtf"] = SessionFileCategories.Document,
            [".epub"] = SessionFileCategories.Document,
            [".xls"] = SessionFileCategories.Table,
            [".xlsx"] = SessionFileCategories.Table,
            [".ods"] = SessionFileCategories.Table,
            [".csv"] = SessionFileCategories.Table,
            [".tsv"] = SessionFileCategories.Table,
            [".parquet"] = SessionFileCategories.Table,
            [".png"] = SessionFileCategories.Image,
            [".jpg"] = SessionFileCategories.Image,
            [".jpeg"] = SessionFileCategories.Image,
            [".webp"] = SessionFileCategories.Image,
            [".gif"] = SessionFileCategories.Image,
            [".bmp"] = SessionFileCategories.Image,
            [".tif"] = SessionFileCategories.Image,
            [".tiff"] = SessionFileCategories.Image,
            [".svg"] = SessionFileCategories.Image,
            [".cs"] = SessionFileCategories.Code,
            [".csx"] = SessionFileCategories.Code,
            [".py"] = SessionFileCategories.Code,
            [".js"] = SessionFileCategories.Code,
            [".ts"] = SessionFileCategories.Code,
            [".tsx"] = SessionFileCategories.Code,
            [".jsx"] = SessionFileCategories.Code,
            [".java"] = SessionFileCategories.Code,
            [".cpp"] = SessionFileCategories.Code,
            [".c"] = SessionFileCategories.Code,
            [".h"] = SessionFileCategories.Code,
            [".hpp"] = SessionFileCategories.Code,
            [".rs"] = SessionFileCategories.Code,
            [".go"] = SessionFileCategories.Code,
            [".sql"] = SessionFileCategories.Code,
            [".html"] = SessionFileCategories.Code,
            [".css"] = SessionFileCategories.Code,
            [".xaml"] = SessionFileCategories.Code,
            [".json"] = SessionFileCategories.Text,
            [".jsonl"] = SessionFileCategories.Text,
            [".xml"] = SessionFileCategories.Text,
            [".yaml"] = SessionFileCategories.Text,
            [".yml"] = SessionFileCategories.Text,
            [".md"] = SessionFileCategories.Text,
            [".txt"] = SessionFileCategories.Text,
            [".log"] = SessionFileCategories.Text,
            [".ini"] = SessionFileCategories.Text,
            [".zip"] = SessionFileCategories.Archive,
            [".7z"] = SessionFileCategories.Archive,
            [".rar"] = SessionFileCategories.Archive,
            [".tar"] = SessionFileCategories.Archive,
            [".gz"] = SessionFileCategories.Archive,
            [".wav"] = SessionFileCategories.Audio,
            [".mp3"] = SessionFileCategories.Audio,
            [".flac"] = SessionFileCategories.Audio,
            [".ogg"] = SessionFileCategories.Audio,
            [".m4a"] = SessionFileCategories.Audio,
            [".aac"] = SessionFileCategories.Audio,
            [".mp4"] = SessionFileCategories.Video,
            [".mkv"] = SessionFileCategories.Video,
            [".mov"] = SessionFileCategories.Video,
            [".avi"] = SessionFileCategories.Video,
            [".webm"] = SessionFileCategories.Video
        };

    public int AddFiles(
        SessionFileManifest manifest,
        IEnumerable<string> paths,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(paths);
        var timestamp = now ?? DateTimeOffset.Now;
        var added = 0;
        foreach (var rawPath in paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var fullPath = Path.GetFullPath(rawPath);
            var id = CreateId(fullPath);
            if (manifest.Files.Any(file => string.Equals(file.Id, id, StringComparison.Ordinal)))
            {
                continue;
            }

            var fileInfo = new FileInfo(fullPath);
            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            manifest.Files.Add(new SessionFileReference
            {
                Id = id,
                SourcePath = fullPath,
                DisplayName = Path.GetFileName(fullPath),
                Extension = extension,
                Category = Classify(extension),
                SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                IsAvailable = fileInfo.Exists,
                AddedAt = timestamp,
                LastCheckedAt = timestamp
            });
            added++;
        }

        if (manifest.Files.Count > 0)
        {
            manifest.Intent = SessionFileIntentStatuses.Selected;
        }

        return added;
    }

    public bool RemoveFile(SessionFileManifest manifest, string id)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var removed = manifest.Files.RemoveAll(file =>
            string.Equals(file.Id, id, StringComparison.Ordinal)) > 0;
        if (removed && manifest.Files.Count == 0)
        {
            manifest.Intent = SessionFileIntentStatuses.None;
        }

        return removed;
    }

    public void SetNoFilesPlanned(SessionFileManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        manifest.Files.Clear();
        manifest.Intent = SessionFileIntentStatuses.None;
    }

    public bool RefreshAvailability(
        SessionFileManifest manifest,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var timestamp = now ?? DateTimeOffset.Now;
        var changed = false;
        foreach (var file in manifest.Files)
        {
            var fileInfo = new FileInfo(file.SourcePath);
            var available = fileInfo.Exists;
            var size = available ? fileInfo.Length : file.SizeBytes;
            changed |= available != file.IsAvailable || size != file.SizeBytes;
            file.IsAvailable = available;
            file.SizeBytes = size;
            file.LastCheckedAt = timestamp;
        }

        return changed;
    }

    public SessionFilePromptManifest CreatePromptManifest(SessionFileManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return new SessionFilePromptManifest
        {
            Intent = manifest.Intent,
            FileCount = manifest.Files.Count,
            TotalSizeBytes = manifest.Files.Where(file => file.IsAvailable).Sum(file => file.SizeBytes),
            ContentAccessAvailable = false,
            RequiredCapabilities = manifest.Files
                .Select(file => CapabilityFor(file.Category))
                .Where(capability => !string.IsNullOrWhiteSpace(capability))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList(),
            Files = manifest.Files.Select(file => new SessionFilePromptItem
            {
                Id = file.Id,
                Name = file.DisplayName,
                Extension = file.Extension,
                Category = file.Category,
                SizeBytes = file.SizeBytes,
                IsAvailable = file.IsAvailable
            }).ToList()
        };
    }

    public IReadOnlyList<ChoiceCapabilityDimension> CreateCapabilityUpdate(
        SessionFileManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (manifest.Intent == SessionFileIntentStatuses.Unknown)
        {
            return [];
        }

        var hasFiles = manifest.Files.Count > 0;
        return
        [
            new ChoiceCapabilityDimension
            {
                Dimension = ChoiceDecisionDimensions.InputModality,
                Status = hasFiles
                    ? ChoiceDimensionStatuses.Resolved
                    : ChoiceDimensionStatuses.NotApplicable,
                Values = hasFiles
                    ? manifest.Files
                        .Select(file => $"file:{file.Category}")
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Order(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                    : [],
                Evidence = hasFiles
                    ? "AI HUB file manifest selected by the user; file contents are unavailable."
                    : "The user declared that no files are planned at scenario start."
            }
        ];
    }

    public static string Classify(string extension) =>
        Categories.TryGetValue(extension, out var category)
            ? category
            : SessionFileCategories.Unknown;

    private static string CapabilityFor(string category) => category switch
    {
        SessionFileCategories.Document => "document_understanding",
        SessionFileCategories.Table => "structured_data",
        SessionFileCategories.Image => "vision",
        SessionFileCategories.Code => "code_processing",
        SessionFileCategories.Text => "text_processing",
        SessionFileCategories.Archive => "archive_handling",
        SessionFileCategories.Audio => "audio_understanding",
        SessionFileCategories.Video => "video_understanding",
        _ => "unknown_file_handling"
    };

    private static string CreateId(string fullPath)
    {
        var normalized = fullPath.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash[..12]).ToLowerInvariant();
    }
}
