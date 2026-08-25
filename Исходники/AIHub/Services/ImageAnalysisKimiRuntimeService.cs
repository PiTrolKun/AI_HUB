using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ImageAnalysisKimiRuntimeService : IDisposable
{
    private readonly ManagedModelLibraryStore _libraryStore;
    private readonly VisionImagePayloadService _imagePayloadService = new();
    private readonly HttpClient _httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private bool _disposed;

    public ImageAnalysisKimiRuntimeService(ManagedModelLibraryStore libraryStore)
    {
        _libraryStore = libraryStore;
    }

    public async Task<string> DescribeAsync(
        ImageAnalysisFilePassport passport,
        string prompt,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passport);
        var card = _libraryStore.Load(ManagedModelCatalog.KimiMediumArtifactId)
            ?? throw new InvalidOperationException("The visual analyst is not registered in the AI HUB model library.");
        if (card.Status != ManagedModelStatuses.Installed)
        {
            throw new InvalidOperationException("The visual analyst is not verified and ready for image analysis.");
        }

        var modelPath = ResolveFile(card, "main_model");
        if (!File.Exists(ChatLlmBackendPaths.ServerExecutablePath))
        {
            throw new FileNotFoundException(
                "The verified runtime required for the visual analyst was not found.",
                ChatLlmBackendPaths.ServerExecutablePath);
        }
        if (!File.Exists(ChatLlmBackendPaths.ImageMagickExecutablePath))
        {
            throw new FileNotFoundException(
                "The private image decoder required by the visual analyst was not found.",
                ChatLlmBackendPaths.ImageMagickExecutablePath);
        }

        progress?.Report(new ImageAnalysisLiteraryProgress(
            ManagedModelRoles.Vision,
            "preparing_image",
            "Preparing the selected image for Kimi."));
        var payload = await _imagePayloadService.PrepareAsync(
            new SessionFileReference
            {
                Id = "image-analysis-source",
                SourcePath = passport.SourcePath,
                DisplayName = passport.DisplayName,
                Extension = passport.Extension,
                Category = SessionFileCategories.Image,
                SizeBytes = passport.SizeBytes,
                IsAvailable = true
            },
            cancellationToken);
        var dataUri = $"data:{payload.MimeType};base64,{Convert.ToBase64String(payload.Bytes)}";
        var requestJson = ImageAnalysisKimiRequestBuilder.BuildRequestBody(dataUri, prompt);

        progress?.Report(new ImageAnalysisLiteraryProgress(
            ManagedModelRoles.Vision,
            "starting_cpu",
            "Starting Kimi with the verified CPU/RAM profile."));
        return await RunAttemptAsync(
            modelPath,
            requestJson,
            log,
            progress,
            cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _httpClient.Dispose();
        _disposed = true;
    }

    private async Task<string> RunAttemptAsync(
        string modelPath,
        string requestJson,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        CancellationToken cancellationToken)
    {
        var port = ReserveLoopbackPort();
        using var process = StartServer(modelPath, port, log);
        try
        {
            await WaitForServerAsync(process, port, cancellationToken);
            progress?.Report(new ImageAnalysisLiteraryProgress(
                ManagedModelRoles.Vision,
                "analysing",
                "Kimi is reading the image and preparing a grounded report."));
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(
                $"http://127.0.0.1:{port}/v1/chat/completions",
                content,
                cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();
            return ImageAnalysisKimiRequestBuilder.ParseResponseContent(responseBody);
        }
        finally
        {
            StopServer(process);
        }
    }

    private static Process StartServer(
        string modelPath,
        int port,
        Action<string> log)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ChatLlmBackendPaths.ServerExecutablePath,
            WorkingDirectory = ChatLlmBackendPaths.DirectoryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        var existingPath = startInfo.Environment.TryGetValue("PATH", out var inheritedPath)
            ? inheritedPath
            : Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        startInfo.Environment["PATH"] = ChatLlmBackendPaths.ImageMagickDirectoryPath
            + Path.PathSeparator
            + existingPath;
        startInfo.Environment["MAGICK_HOME"] = ChatLlmBackendPaths.ImageMagickDirectoryPath;
        foreach (var argument in ImageAnalysisKimiRequestBuilder.BuildArguments(
            modelPath,
            port))
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, args) => LogBackendLine(args.Data, log);
        process.ErrorDataReceived += (_, args) => LogBackendLine(args.Data, log);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private async Task WaitForServerAsync(
        Process process,
        int port,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(8);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The visual analyst runtime exited with code {process.ExitCode}.");
            }
            try
            {
                using var response = await _httpClient.GetAsync(
                    $"http://127.0.0.1:{port}/health",
                    cancellationToken);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // The model is still loading.
            }
            await Task.Delay(750, cancellationToken);
        }
        throw new TimeoutException("The visual analyst did not become ready in time.");
    }

    private static string ResolveFile(ManagedModelArtifactCard card, string purpose)
    {
        var file = card.Files.FirstOrDefault(item => string.Equals(
            item.Purpose,
            purpose,
            StringComparison.Ordinal))
            ?? throw new InvalidDataException($"The visual analyst manifest has no '{purpose}' file.");
        var root = Path.GetFullPath(card.InstallDirectory)
            .TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, file.RelativePath));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            throw new FileNotFoundException("A verified visual analyst file is missing.", path);
        }
        return path;
    }

    private static int ReserveLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static void StopServer(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
        }
        catch
        {
            // Best-effort local runtime shutdown.
        }
    }

    private static void LogBackendLine(string? line, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }
        if (line.Contains("serving at", StringComparison.OrdinalIgnoreCase)
            || line.Contains("POST /v1/chat/completions", StringComparison.OrdinalIgnoreCase)
            || line.Contains("error", StringComparison.OrdinalIgnoreCase)
            || line.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            log(line.Trim());
        }
    }
}
