using System.IO;
using System.IO.Compression;
using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ArtifactValidationService
{
    public ArtifactValidationResult Validate(
        string filePath,
        ArtifactContract contract)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(contract);

        var fullPath = Path.GetFullPath(filePath);
        var result = new ArtifactValidationResult
        {
            FilePath = fullPath,
            DetectedExtension = Path.GetExtension(fullPath).ToLowerInvariant()
        };
        if (!File.Exists(fullPath))
        {
            result.Status = ArtifactValidationStatuses.Invalid;
            result.Errors.Add("The output file does not exist.");
            return result;
        }

        var info = new FileInfo(fullPath);
        result.SizeBytes = info.Length;
        if (info.Length == 0)
        {
            result.Errors.Add("The output file is empty.");
        }
        else
        {
            result.Checks.Add("The output file exists and is non-empty.");
        }

        try
        {
            using var stream = File.OpenRead(fullPath);
            result.DetectedMimeType = DetectMime(stream, result.DetectedExtension);
            if (!IsArtifactFamilyCompatible(
                    contract.ArtifactKind,
                    result.DetectedExtension,
                    result.DetectedMimeType))
            {
                result.Errors.Add(
                    $"The detected file type does not match artifact kind '{contract.ArtifactKind}'.");
            }
            else
            {
                result.Checks.Add(
                    $"The detected container is compatible with '{contract.ArtifactKind}'.");
            }

            ValidateReadability(
                stream,
                result.DetectedExtension,
                result.DetectedMimeType,
                result);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            result.Errors.Add($"The output file could not be read: {ex.Message}");
        }

        result.Status = result.Errors.Count == 0
            ? ArtifactValidationStatuses.Valid
            : ArtifactValidationStatuses.Invalid;
        return result;
    }

    private static string DetectMime(Stream stream, string extension)
    {
        Span<byte> header = stackalloc byte[16];
        var read = stream.Read(header);
        stream.Position = 0;
        if (read >= 8 && header[..8].SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "image/png";
        }

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (read >= 6
            && Encoding.ASCII.GetString(header[..6]) is "GIF87a" or "GIF89a")
        {
            return "image/gif";
        }

        if (read >= 12 && Encoding.ASCII.GetString(header[..4]) == "RIFF")
        {
            return Encoding.ASCII.GetString(header.Slice(8, 4)) switch
            {
                "WAVE" => "audio/wav",
                "WEBP" => "image/webp",
                "AVI " => "video/x-msvideo",
                _ => "application/octet-stream"
            };
        }

        if (read >= 12 && Encoding.ASCII.GetString(header.Slice(4, 4)) == "ftyp")
        {
            return "video/mp4";
        }

        if (read >= 4
            && header[0] == 0x50
            && header[1] == 0x4B
            && header[2] is 0x03 or 0x05 or 0x07
            && header[3] is 0x04 or 0x06 or 0x08)
        {
            return extension switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".zip" => "application/zip",
                _ => "application/zip"
            };
        }

        return extension switch
        {
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".patch" or ".diff" => "text/x-diff",
            ".bmp" => "image/bmp",
            ".mp3" => "audio/mpeg",
            ".flac" => "audio/flac",
            ".ogg" => "audio/ogg",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }

    private static bool IsArtifactFamilyCompatible(
        string artifactKind,
        string extension,
        string mimeType) =>
        artifactKind switch
        {
            ArtifactKinds.Image => mimeType.StartsWith("image/", StringComparison.Ordinal),
            ArtifactKinds.Audio => mimeType.StartsWith("audio/", StringComparison.Ordinal),
            ArtifactKinds.Video => mimeType.StartsWith("video/", StringComparison.Ordinal),
            ArtifactKinds.Document => extension is ".docx" or ".txt" or ".md" or ".pdf",
            ArtifactKinds.Table => extension is ".xlsx" or ".csv" or ".tsv",
            ArtifactKinds.Presentation => extension is ".pptx" or ".pdf",
            ArtifactKinds.Code => mimeType.StartsWith("text/", StringComparison.Ordinal)
                || extension is ".json" or ".xml",
            ArtifactKinds.Archive => extension is ".zip" or ".7z" or ".tar" or ".gz",
            ArtifactKinds.Text => mimeType.StartsWith("text/", StringComparison.Ordinal),
            ArtifactKinds.File => true,
            _ => true
        };

    private static void ValidateReadability(
        Stream stream,
        string extension,
        string mimeType,
        ArtifactValidationResult result)
    {
        stream.Position = 0;
        if (mimeType.StartsWith("text/", StringComparison.Ordinal)
            || extension is ".json" or ".xml" or ".patch" or ".diff")
        {
            using var reader = new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true);
            if (string.IsNullOrWhiteSpace(reader.ReadToEnd()))
            {
                result.Errors.Add("The text artifact contains no readable content.");
                return;
            }

            result.Checks.Add("The text artifact contains readable content.");
            return;
        }

        if (mimeType.Contains("openxmlformats", StringComparison.Ordinal)
            || mimeType == "application/zip")
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count == 0)
            {
                result.Errors.Add("The package contains no entries.");
                return;
            }

            result.Checks.Add("The ZIP/Open XML package can be enumerated.");
            return;
        }

        result.Checks.Add("The binary container has a recognized signature or extension.");
    }
}
