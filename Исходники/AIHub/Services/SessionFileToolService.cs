using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Media.Imaging;
using AIHub.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Data.Sqlite;
using MimeKit;
using SharpCompress.Archives;
using UglyToad.PdfPig;

namespace AIHub.Services;

public sealed class SessionFileToolService
{
    public const int DefaultReturnedCharacters = 8_000;
    public const int MaximumReturnedCharacters = 16_000;
    public const int MaximumOffset = 1_000_000;
    public const long MaximumStructuredFileBytes = 128L * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly HashSet<string> PlainTextExtensions = new(
        [
            ".txt", ".log", ".md", ".markdown", ".json", ".jsonl", ".xml",
            ".yaml", ".yml", ".ini", ".cfg", ".conf", ".csv", ".tsv",
            ".html", ".htm", ".css", ".svg", ".cs", ".csx", ".py", ".js",
            ".jsx", ".ts", ".tsx", ".java", ".c", ".cpp", ".h", ".hpp",
            ".rs", ".go", ".sql", ".xaml", ".ps1", ".sh", ".bat", ".cmd"
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> SpreadsheetExtensions = new(
        [".xlsx", ".xlsm"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ArchiveExtensions = new(
        [".zip", ".7z", ".rar", ".tar", ".gz"],
        StringComparer.OrdinalIgnoreCase);

    public string ListFiles(SessionFileManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return JsonSerializer.Serialize(new
        {
            success = true,
            intent = manifest.Intent,
            file_count = manifest.Files.Count,
            files = manifest.Files.Select(file => new
            {
                file_id = file.Id,
                name = file.DisplayName,
                extension = file.Extension,
                category = file.Category,
                size_bytes = file.SizeBytes,
                available = file.IsAvailable
            })
        }, JsonOptions);
    }

    public string Inspect(SessionFileManifest manifest, string fileId)
    {
        var resolved = Resolve(manifest, fileId);
        var details = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["success"] = true,
            ["file_id"] = resolved.Reference.Id,
            ["name"] = resolved.Reference.DisplayName,
            ["extension"] = resolved.Extension,
            ["category"] = resolved.Reference.Category,
            ["size_bytes"] = resolved.Info.Length,
            ["available"] = true,
            ["content_readable"] = CanReadContent(resolved.Extension),
            ["read_mode"] = GetReadMode(resolved.Extension),
            ["semantic_media_access"] = false
        };

        if (IsRasterImage(resolved.Extension))
        {
            TryAddImageDimensions(resolved.Info.FullName, details);
        }

        details["note"] = GetInspectNote(resolved.Extension);
        return JsonSerializer.Serialize(details, JsonOptions);
    }

    public string Read(
        SessionFileManifest manifest,
        string fileId,
        int offset,
        int maxCharacters,
        CancellationToken cancellationToken)
    {
        if (offset < 0 || offset > MaximumOffset)
        {
            throw new SessionFileToolException("invalid_offset", "The requested offset is outside the safe range.");
        }

        var boundedMaximum = maxCharacters <= 0
            ? DefaultReturnedCharacters
            : Math.Min(maxCharacters, MaximumReturnedCharacters);
        var resolved = Resolve(manifest, fileId);
        if (!CanReadContent(resolved.Extension))
        {
            throw new SessionFileToolException(
                "content_adapter_unavailable",
                GetUnavailableReadMessage(resolved.Extension));
        }

        if (RequiresStructuredLoad(resolved.Extension)
            && resolved.Info.Length > MaximumStructuredFileBytes)
        {
            throw new SessionFileToolException(
                "file_too_large",
                "This structured file is too large for the safe in-process reader.");
        }

        var targetCharacters = checked(offset + boundedMaximum + 1);
        var extracted = ExtractText(resolved, targetCharacters, cancellationToken);
        if (offset >= extracted.Length)
        {
            return JsonSerializer.Serialize(new
            {
                success = true,
                file_id = resolved.Reference.Id,
                name = resolved.Reference.DisplayName,
                representation = GetReadMode(resolved.Extension),
                offset,
                returned_characters = 0,
                has_more = false,
                next_offset = (int?)null,
                content = string.Empty,
                note = "The requested offset is at or beyond the end of the extracted representation."
            }, JsonOptions);
        }

        var availableCharacters = Math.Min(boundedMaximum, extracted.Length - offset);
        var content = extracted.Substring(offset, availableCharacters);
        var hasMore = extracted.Length > offset + availableCharacters;
        return JsonSerializer.Serialize(new
        {
            success = true,
            file_id = resolved.Reference.Id,
            name = resolved.Reference.DisplayName,
            representation = GetReadMode(resolved.Extension),
            offset,
            returned_characters = content.Length,
            has_more = hasMore,
            next_offset = hasMore ? offset + content.Length : (int?)null,
            content,
            note = hasMore
                ? "Call session_file_read again with next_offset to continue."
                : "End of the available extracted representation."
        }, JsonOptions);
    }

    private static ResolvedSessionFile Resolve(SessionFileManifest manifest, string fileId)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (string.IsNullOrWhiteSpace(fileId))
        {
            throw new SessionFileToolException("missing_file_id", "A session file ID is required.");
        }

        var reference = manifest.Files.SingleOrDefault(file =>
            string.Equals(file.Id, fileId.Trim(), StringComparison.Ordinal));
        if (reference is null)
        {
            throw new SessionFileToolException(
                "file_not_allowed",
                "The requested file is not present in the trusted session manifest.");
        }

        if (!reference.IsAvailable || string.IsNullOrWhiteSpace(reference.SourcePath))
        {
            throw new SessionFileToolException(
                "file_unavailable",
                "The selected session file is currently unavailable.");
        }

        var info = new FileInfo(reference.SourcePath);
        if (!info.Exists)
        {
            throw new SessionFileToolException(
                "file_unavailable",
                "The selected session file is no longer available.");
        }

        if ((info.Attributes & FileAttributes.Directory) != 0 || info.LinkTarget is not null)
        {
            throw new SessionFileToolException(
                "unsafe_file_type",
                "Directories and linked files are not accepted by this tool.");
        }

        return new ResolvedSessionFile(
            reference,
            info,
            NormalizeExtension(reference.Extension, info.Extension));
    }

    private static string ExtractText(
        ResolvedSessionFile file,
        int targetCharacters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (PlainTextExtensions.Contains(file.Extension))
        {
            return ReadPlainText(file.Info.FullName, targetCharacters, cancellationToken);
        }

        return file.Extension.ToLowerInvariant() switch
        {
            ".pdf" => ReadPdf(file.Info.FullName, targetCharacters, cancellationToken),
            ".docx" => ReadWord(file.Info.FullName, targetCharacters, cancellationToken),
            ".pptx" => ReadPresentation(file.Info.FullName, targetCharacters, cancellationToken),
            ".xlsx" or ".xlsm" => ReadSpreadsheet(file.Info.FullName, targetCharacters, cancellationToken),
            ".eml" or ".mime" => ReadEmail(file.Info.FullName, targetCharacters),
            ".epub" => ReadEpub(file.Info.FullName, targetCharacters, cancellationToken),
            ".db" or ".sqlite" or ".sqlite3" => ReadSqliteSchema(file.Info.FullName, targetCharacters),
            _ when ArchiveExtensions.Contains(file.Extension) =>
                ReadArchiveListing(file.Info.FullName, targetCharacters, cancellationToken),
            _ => throw new SessionFileToolException(
                "content_adapter_unavailable",
                GetUnavailableReadMessage(file.Extension))
        };
    }

    private static string ReadPlainText(
        string path,
        int targetCharacters,
        CancellationToken cancellationToken)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 16_384,
            leaveOpen: false);
        var builder = new StringBuilder(Math.Min(targetCharacters, 64_000));
        var buffer = new char[Math.Min(16_384, targetCharacters)];
        while (builder.Length < targetCharacters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = reader.Read(
                buffer,
                0,
                Math.Min(buffer.Length, targetCharacters - builder.Length));
            if (count == 0)
            {
                break;
            }

            builder.Append(buffer, 0, count);
        }

        return builder.ToString();
    }

    private static string ReadPdf(
        string path,
        int targetCharacters,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(path);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendBounded(builder, $"--- Page {page.Number} ---{Environment.NewLine}", targetCharacters);
            AppendBounded(builder, page.Text, targetCharacters);
            AppendBounded(builder, Environment.NewLine, targetCharacters);
            if (builder.Length >= targetCharacters)
            {
                break;
            }
        }

        return builder.ToString();
    }

    private static string ReadWord(
        string path,
        int targetCharacters,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var document = WordprocessingDocument.Open(path, false);
        var paragraphs = document.MainDocumentPart?.Document?.Body?
            .Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>() ?? [];
        foreach (var paragraph in paragraphs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendBounded(builder, paragraph.InnerText, targetCharacters);
            AppendBounded(builder, Environment.NewLine, targetCharacters);
            if (builder.Length >= targetCharacters)
            {
                break;
            }
        }

        return builder.ToString();
    }

    private static string ReadPresentation(
        string path,
        int targetCharacters,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var document = PresentationDocument.Open(path, false);
        var slides = document.PresentationPart?.SlideParts.ToList() ?? [];
        for (var index = 0; index < slides.Count && builder.Length < targetCharacters; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendBounded(builder, $"--- Slide {index + 1} ---{Environment.NewLine}", targetCharacters);
            var slide = slides[index].Slide;
            if (slide is null)
            {
                continue;
            }

            foreach (var node in slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
            {
                AppendBounded(builder, node.Text, targetCharacters);
                AppendBounded(builder, Environment.NewLine, targetCharacters);
                if (builder.Length >= targetCharacters)
                {
                    break;
                }
            }
        }

        return builder.ToString();
    }

    private static string ReadSpreadsheet(
        string path,
        int targetCharacters,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var workbook = new XLWorkbook(path);
        foreach (var worksheet in workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendBounded(builder, $"--- Sheet: {worksheet.Name} ---{Environment.NewLine}", targetCharacters);
            var range = worksheet.RangeUsed();
            if (range is null)
            {
                AppendBounded(builder, "[empty]" + Environment.NewLine, targetCharacters);
                continue;
            }

            var maximumColumns = Math.Min(range.ColumnCount(), 256);
            foreach (var row in range.Rows())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var values = Enumerable.Range(1, maximumColumns)
                    .Select(column => SanitizeTabularCell(row.Cell(column).GetFormattedString()));
                AppendBounded(builder, string.Join('\t', values), targetCharacters);
                AppendBounded(builder, Environment.NewLine, targetCharacters);
                if (builder.Length >= targetCharacters)
                {
                    break;
                }
            }

            if (builder.Length >= targetCharacters)
            {
                break;
            }
        }

        return builder.ToString();
    }

    private static string ReadEmail(string path, int targetCharacters)
    {
        var message = MimeMessage.Load(path);
        var builder = new StringBuilder();
        AppendBounded(builder, $"From: {message.From}{Environment.NewLine}", targetCharacters);
        AppendBounded(builder, $"To: {message.To}{Environment.NewLine}", targetCharacters);
        AppendBounded(builder, $"Cc: {message.Cc}{Environment.NewLine}", targetCharacters);
        AppendBounded(builder, $"Date: {message.Date}{Environment.NewLine}", targetCharacters);
        AppendBounded(builder, $"Subject: {message.Subject}{Environment.NewLine}{Environment.NewLine}", targetCharacters);
        AppendBounded(builder, message.TextBody ?? message.HtmlBody ?? "[no text body]", targetCharacters);
        if (message.Attachments.Any() && builder.Length < targetCharacters)
        {
            AppendBounded(builder, $"{Environment.NewLine}{Environment.NewLine}Attachments:{Environment.NewLine}", targetCharacters);
            foreach (var attachment in message.Attachments)
            {
                var name = attachment.ContentDisposition?.FileName
                    ?? attachment.ContentType.Name
                    ?? "attachment";
                AppendBounded(builder, $"- {name}{Environment.NewLine}", targetCharacters);
                if (builder.Length >= targetCharacters)
                {
                    break;
                }
            }
        }

        return builder.ToString();
    }

    private static string ReadEpub(
        string path,
        int targetCharacters,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var archive = ArchiveFactory.Open(path);
        foreach (var entry in archive.Entries.Where(entry =>
                     !entry.IsDirectory
                     && IsEpubTextEntry(entry.Key)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = entry.OpenEntryStream();
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var remaining = targetCharacters - builder.Length;
            var buffer = new char[Math.Min(16_384, remaining)];
            while (builder.Length < targetCharacters)
            {
                var count = reader.Read(buffer, 0, Math.Min(buffer.Length, targetCharacters - builder.Length));
                if (count == 0)
                {
                    break;
                }

                builder.Append(buffer, 0, count);
            }

            AppendBounded(builder, Environment.NewLine, targetCharacters);
            if (builder.Length >= targetCharacters)
            {
                break;
            }
        }

        return builder.ToString();
    }

    private static string ReadArchiveListing(
        string path,
        int targetCharacters,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        using var archive = ArchiveFactory.Open(path);
        foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendBounded(
                builder,
                $"{entry.Size.ToString(CultureInfo.InvariantCulture)}\t{entry.Key}{Environment.NewLine}",
                targetCharacters);
            if (builder.Length >= targetCharacters)
            {
                break;
            }
        }

        return builder.ToString();
    }

    private static string ReadSqliteSchema(string path, int targetCharacters)
    {
        var builder = new StringBuilder();
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name, type, sql FROM sqlite_master WHERE type IN ('table','view') ORDER BY type, name";
        using var reader = command.ExecuteReader();
        while (reader.Read() && builder.Length < targetCharacters)
        {
            AppendBounded(builder, $"{reader.GetString(1)}: {reader.GetString(0)}{Environment.NewLine}", targetCharacters);
            if (!reader.IsDBNull(2))
            {
                AppendBounded(builder, reader.GetString(2), targetCharacters);
                AppendBounded(builder, Environment.NewLine, targetCharacters);
            }
        }

        return builder.ToString();
    }

    private static void AppendBounded(StringBuilder builder, string? value, int maximum)
    {
        if (string.IsNullOrEmpty(value) || builder.Length >= maximum)
        {
            return;
        }

        var remaining = maximum - builder.Length;
        builder.Append(value.AsSpan(0, Math.Min(value.Length, remaining)));
    }

    private static bool CanReadContent(string extension) =>
        PlainTextExtensions.Contains(extension)
        || SpreadsheetExtensions.Contains(extension)
        || ArchiveExtensions.Contains(extension)
        || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".docx", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".eml", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".mime", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".epub", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".db", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".sqlite", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".sqlite3", StringComparison.OrdinalIgnoreCase);

    private static bool RequiresStructuredLoad(string extension) =>
        !PlainTextExtensions.Contains(extension)
        && !extension.Equals(".db", StringComparison.OrdinalIgnoreCase)
        && !extension.Equals(".sqlite", StringComparison.OrdinalIgnoreCase)
        && !extension.Equals(".sqlite3", StringComparison.OrdinalIgnoreCase);

    private static string GetReadMode(string extension)
    {
        if (PlainTextExtensions.Contains(extension))
        {
            return "bounded_text";
        }

        if (SpreadsheetExtensions.Contains(extension))
        {
            return "sheet_rows_as_tsv";
        }

        if (ArchiveExtensions.Contains(extension))
        {
            return "archive_entry_listing";
        }

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "pdf_text",
            ".docx" => "word_text",
            ".pptx" => "presentation_text",
            ".eml" or ".mime" => "email_text",
            ".epub" => "epub_markup_text",
            ".db" or ".sqlite" or ".sqlite3" => "sqlite_schema",
            _ => "metadata_only"
        };
    }

    private static string GetInspectNote(string extension)
    {
        if (CanReadContent(extension))
        {
            return "Use session_file_read for a bounded representation. The tool does not modify the file.";
        }

        if (IsRasterImage(extension))
        {
            return "Only technical image metadata is available. This tool does not understand image pixels.";
        }

        if (IsMedia(extension))
        {
            return "Only technical file metadata is available. This tool does not hear audio or watch video.";
        }

        return "No safe content adapter is available for this format.";
    }

    private static string GetUnavailableReadMessage(string extension)
    {
        if (IsRasterImage(extension))
        {
            return "This tool can inspect technical image metadata but cannot understand image pixels.";
        }

        if (IsMedia(extension))
        {
            return "This tool can inspect technical media metadata but cannot hear audio or watch video.";
        }

        return "No safe read adapter is available for this file format.";
    }

    private static bool IsRasterImage(string extension) =>
        extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);

    private static bool IsMedia(string extension) =>
        extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".flac", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".aac", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".avi", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".webm", StringComparison.OrdinalIgnoreCase);

    private static void TryAddImageDimensions(
        string path,
        IDictionary<string, object?> details)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            var frame = decoder.Frames.FirstOrDefault();
            if (frame is not null)
            {
                details["pixel_width"] = frame.PixelWidth;
                details["pixel_height"] = frame.PixelHeight;
            }
        }
        catch
        {
            details["technical_metadata_warning"] = "Image dimensions could not be read.";
        }
    }

    private static string NormalizeExtension(string manifestExtension, string fileExtension)
    {
        var extension = string.IsNullOrWhiteSpace(manifestExtension)
            ? fileExtension
            : manifestExtension;
        return extension.StartsWith(".", StringComparison.Ordinal)
            ? extension.ToLowerInvariant()
            : "." + extension.ToLowerInvariant();
    }

    private static bool IsEpubTextEntry(string? key) =>
        (key ?? string.Empty).EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase)
        || (key ?? string.Empty).EndsWith(".html", StringComparison.OrdinalIgnoreCase)
        || (key ?? string.Empty).EndsWith(".htm", StringComparison.OrdinalIgnoreCase);

    private static string SanitizeTabularCell(string value) =>
        value.Replace('\t', ' ').Replace("\r", " ").Replace("\n", " ");

    private sealed record ResolvedSessionFile(
        SessionFileReference Reference,
        FileInfo Info,
        string Extension);
}

public sealed class SessionFileToolException : Exception
{
    public SessionFileToolException(
        string code,
        string safeMessage,
        string diagnosticMessage = "")
        : base(safeMessage)
    {
        Code = code;
        SafeMessage = safeMessage;
        DiagnosticMessage = diagnosticMessage;
    }

    public string Code { get; }

    public string SafeMessage { get; }

    public string DiagnosticMessage { get; }
}
