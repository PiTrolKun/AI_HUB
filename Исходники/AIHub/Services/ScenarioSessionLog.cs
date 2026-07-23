using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Channels;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ScenarioSessionLog : ISessionEventLog
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly System.Text.Encoding Utf8NoBom = new System.Text.UTF8Encoding(false);
    private readonly object _sync = new();
    private readonly Channel<string> _lines = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    private readonly Task _writeTask;
    private bool _disposed;

    private ScenarioSessionLog(string filePath, string? sessionId = null)
    {
        FilePath = filePath;
        SessionId = string.IsNullOrWhiteSpace(sessionId)
            ? Path.GetFileNameWithoutExtension(filePath)
            : sessionId;
        _writeTask = Task.Run(WriteLoopAsync);
    }

    public string FilePath { get; }

    public string SessionId { get; }

    public static ScenarioSessionLog CreateUncertainty(
        StorageSettings storageSettings,
        string? sessionId = null,
        string? runId = null)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var directory = new ScenarioSessionArchiveService()
                .GetLogsDirectory(storageSettings, sessionId);
            return CreateInDirectory(directory, "core", sessionId, runId);
        }

        var configuredRoot = GetResultsRoot(storageSettings);
        var configuredDirectory = configuredRoot is null
            ? null
            : Path.Combine(configuredRoot, "AI_HUB", "Scenarios", "Uncertainty", "Sessions");

        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            try
            {
                return CreateInDirectory(configuredDirectory);
            }
            catch (Exception ex) when (!IsCriticalException(ex))
            {
                // A removable or unavailable results disk must not prevent the scenario from starting.
            }
        }

        var fallbackDirectory = Path.Combine(
            AppDataPaths.BaseDirectory,
            "Scenarios",
            "Uncertainty",
            "Sessions");
        return CreateInDirectory(fallbackDirectory);
    }

    public static ScenarioSessionLog CreateUncertaintyExecutor(
        StorageSettings storageSettings,
        string? sessionId = null,
        string? runId = null)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var projectDirectory = new ScenarioSessionArchiveService()
                .GetLogsDirectory(storageSettings, sessionId);
            return CreateInDirectory(projectDirectory, "executor", sessionId, runId);
        }

        var configuredRoot = GetResultsRoot(storageSettings);
        var directory = configuredRoot is null
            ? Path.Combine(AppDataPaths.BaseDirectory, "Scenarios", "Uncertainty", "Executors")
            : Path.Combine(configuredRoot, "AI_HUB", "Scenarios", "Uncertainty", "Executors");
        return CreateInDirectory(directory, "executor");
    }

    public void Write(string eventType, object? payload = null)
    {
        if (_disposed)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var record = new
        {
            SchemaVersion = CurrentSchemaVersion,
            Type = eventType,
            SessionId,
            LocalTime = now,
            UtcTime = now.ToUniversalTime(),
            ContainsSensitiveLocalData = true,
            Payload = payload
        };

        var line = JsonSerializer.Serialize(record, JsonOptions);
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _lines.Writer.TryWrite(line);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_sync)
        {
            _disposed = true;
            _lines.Writer.TryComplete();
        }

        _writeTask.GetAwaiter().GetResult();
    }

    private static ScenarioSessionLog CreateInDirectory(
        string directory,
        string suffix = "uncertainty",
        string? sessionId = null,
        string? runId = null)
    {
        Directory.CreateDirectory(directory);
        var filePath = string.IsNullOrWhiteSpace(runId)
            ? SessionPathService.CreateSessionFilePath(directory, suffix)
            : CreateRunLogPath(directory, suffix, runId);
        using (File.Open(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
        }

        return new ScenarioSessionLog(filePath, sessionId);
    }

    private static string CreateRunLogPath(string directory, string suffix, string runId)
    {
        var safeRunId = string.Concat(runId.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        var basePath = Path.Combine(directory, $"{safeRunId}_{suffix}.jsonl");
        if (!File.Exists(basePath))
        {
            return basePath;
        }

        return Path.Combine(
            directory,
            $"{safeRunId}_{suffix}_{Guid.NewGuid():N}.jsonl");
    }

    private static string? GetResultsRoot(StorageSettings storageSettings)
    {
        return storageSettings.Results.Locations
            .Select(location => location.Path?.Trim())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
    }

    private static bool IsCriticalException(Exception exception) =>
        exception is OutOfMemoryException or StackOverflowException;

    private async Task WriteLoopAsync()
    {
        try
        {
            await using var stream = new FileStream(
                FilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            await using var writer = new StreamWriter(stream, Utf8NoBom);
            await foreach (var line in _lines.Reader.ReadAllAsync())
            {
                await writer.WriteLineAsync(line);
                await writer.FlushAsync();
            }
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            // Logging is diagnostic and must never crash the scenario.
        }
    }
}
