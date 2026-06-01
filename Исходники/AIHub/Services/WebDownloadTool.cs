using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Diagnostics;
using AIHub.Models;

namespace AIHub.Services;

public sealed class WebDownloadTool
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public async Task<WebDownloadResponse> DownloadAsync(
        string url,
        StorageSettings storageSettings,
        CancellationToken cancellationToken,
        IProgress<WebDownloadProgress>? progress = null)
    {
        var uri = NormalizeHttpUrl(url);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("AI_HUB/0.1 (+https://github.com/PiTrolKun/AI_HUB)");
        request.Headers.Accept.ParseAdd("*/*");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        var originalContentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
        var fileNameInfo = CreateSafeFileName(uri, contentType);
        var fileName = fileNameInfo.FileName;
        var directory = WebToolPathService.GetDownloadsDirectory(storageSettings);
        var path = GetUniquePath(Path.Combine(directory, fileName));
        var totalBytes = response.Content.Headers.ContentLength;
        var stopwatch = Stopwatch.StartNew();
        long downloadedBytes = 0;

        ReportProgress(progress, uri, path, downloadedBytes, totalBytes, stopwatch, isComplete: false);

        await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var output = File.Create(path))
        {
            var buffer = new byte[1024 * 128];
            var lastReport = TimeSpan.Zero;

            while (true)
            {
                var read = await input.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloadedBytes += read;

                if (stopwatch.Elapsed - lastReport >= TimeSpan.FromMilliseconds(250))
                {
                    ReportProgress(progress, uri, path, downloadedBytes, totalBytes, stopwatch, isComplete: false);
                    lastReport = stopwatch.Elapsed;
                }
            }
        }

        ReportProgress(progress, uri, path, downloadedBytes, totalBytes, stopwatch, isComplete: true);

        await using var fileStream = File.OpenRead(path);
        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(fileStream, cancellationToken)).ToLowerInvariant();
        var fileInfo = new FileInfo(path);

        return new WebDownloadResponse
        {
            Url = uri.ToString(),
            FilePath = path,
            SizeBytes = fileInfo.Length,
            Sha256 = sha256,
            ContentType = originalContentType,
            ContentKind = GetContentKind(contentType),
            IsHtmlPage = IsHtmlContent(contentType),
            IsImage = IsImageContent(contentType),
            ExtensionWasAdded = fileNameInfo.ExtensionWasAdded,
            Warning = CreateWarning(contentType)
        };
    }

    private static Uri NormalizeHttpUrl(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Only http/https URLs are supported.");
        }

        return uri;
    }

    private static FileNameInfo CreateSafeFileName(Uri uri, string contentType)
    {
        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "download";
        }

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid, '_');
        }

        var extensionWasAdded = false;
        var extension = GetExtensionForContentType(contentType);
        if (!string.IsNullOrWhiteSpace(extension) && string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
        {
            fileName += extension;
            extensionWasAdded = true;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
        return new FileNameInfo($"{timestamp}_{fileName}", extensionWasAdded);
    }

    private static string GetContentKind(string contentType)
    {
        if (IsImageContent(contentType))
        {
            return "image";
        }

        if (IsHtmlContent(contentType))
        {
            return "html";
        }

        if (contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
        {
            return "json";
        }

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return "document";
        }

        if (contentType.Equals("application/zip", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/x-7z-compressed", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/x-rar-compressed", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/gzip", StringComparison.OrdinalIgnoreCase))
        {
            return "archive";
        }

        if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return "audio";
        }

        if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return "video";
        }

        if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return "text";
        }

        return string.IsNullOrWhiteSpace(contentType) ? "unknown" : "binary";
    }

    private static bool IsHtmlContent(string contentType)
    {
        return contentType.Equals("text/html", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageContent(string contentType)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateWarning(string contentType)
    {
        return IsHtmlContent(contentType)
            ? "Downloaded content is an HTML page, not a direct file/image. If the user asked for an image or binary file, continue searching for a direct URL."
            : string.Empty;
    }

    private static string GetExtensionForContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "text/html" or "application/xhtml+xml" => ".html",
            "text/plain" => ".txt",
            "application/json" => ".json",
            "application/pdf" => ".pdf",
            "application/zip" => ".zip",
            "application/x-7z-compressed" => ".7z",
            "application/x-rar-compressed" => ".rar",
            "application/gzip" => ".gz",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/svg+xml" => ".svg",
            "audio/mpeg" => ".mp3",
            "audio/wav" or "audio/x-wav" => ".wav",
            "audio/ogg" => ".ogg",
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            _ => string.Empty
        };
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var index = 1; ; index++)
        {
            var candidate = Path.Combine(directory, $"{name}_{index}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static void ReportProgress(
        IProgress<WebDownloadProgress>? progress,
        Uri uri,
        string path,
        long downloadedBytes,
        long? totalBytes,
        Stopwatch stopwatch,
        bool isComplete)
    {
        if (progress is null)
        {
            return;
        }

        var seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
        progress.Report(new WebDownloadProgress
        {
            Url = uri.ToString(),
            FilePath = path,
            DownloadedBytes = downloadedBytes,
            TotalBytes = totalBytes,
            BytesPerSecond = downloadedBytes / seconds,
            IsComplete = isComplete
        });
    }

    private sealed record FileNameInfo(string FileName, bool ExtensionWasAdded);
}
