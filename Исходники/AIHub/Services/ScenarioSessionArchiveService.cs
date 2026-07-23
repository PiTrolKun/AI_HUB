using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ScenarioSessionArchiveService
{
    public const string ScenarioId = "uncertainty";
    private const string SessionFileName = "session.json";
    private const string PreviousSessionFileName = "session.previous.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object _sync = new();

    public ResumableScenarioSession Create(
        StorageSettings storageSettings,
        string scenarioName,
        ChoiceScenarioStateCheckpoint coreCheckpoint)
    {
        var now = DateTimeOffset.Now;
        var session = new ResumableScenarioSession
        {
            SessionId = $"session_{Guid.NewGuid():N}",
            CurrentRunId = CreateRunId(),
            ScenarioName = scenarioName,
            CreatedAt = now,
            UpdatedAt = now,
            Status = ResumableSessionStatuses.Active,
            IsRunOpen = true,
            Core = coreCheckpoint
        };
        Save(storageSettings, session, touchUpdatedAt: false);
        return session;
    }

    public IReadOnlyList<ResumableScenarioSession> LoadAll(StorageSettings storageSettings)
    {
        var root = GetProjectsRoot(storageSettings);
        if (!Directory.Exists(root))
        {
            return [];
        }

        var sessions = new List<ResumableScenarioSession>();
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var session = TryLoadFromDirectory(directory);
            if (session is not null)
            {
                sessions.Add(session);
            }
        }

        return sessions
            .OrderByDescending(session => session.UpdatedAt)
            .ThenByDescending(session => session.CreatedAt)
            .ToList();
    }

    public ResumableScenarioSession? Load(StorageSettings storageSettings, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        return TryLoadFromDirectory(GetSessionDirectory(storageSettings, sessionId));
    }

    public SessionRestorationContext BeginRestoredRun(
        StorageSettings storageSettings,
        ResumableScenarioSession session)
    {
        var previousRunWasOpen = session.IsRunOpen;
        var previousStopKind = previousRunWasOpen
            ? ResumableSessionStopKinds.Crash
            : session.LastStopKind;
        var context = new SessionRestorationContext
        {
            SessionId = session.SessionId,
            RunId = CreateRunId(),
            ResumeCount = session.ResumeCount + 1,
            OriginalCreatedAt = session.CreatedAt,
            RestoredAt = DateTimeOffset.Now,
            PreviousStopKind = previousStopKind,
            PreviousStopReason = session.LastStopReason,
            LostUncommittedTurn = previousRunWasOpen || session.LostUncommittedTurn,
            LastStableStage = session.Executor?.CurrentStageId
                ?? session.Core.Steps.LastOrDefault()?.StepType
                ?? string.Empty
        };

        session.CurrentRunId = context.RunId;
        session.ResumeCount = context.ResumeCount;
        session.Status = previousRunWasOpen
            ? ResumableSessionStatuses.Recovered
            : ResumableSessionStatuses.Active;
        session.LastStopKind = previousStopKind;
        session.IsRunOpen = true;
        session.LostUncommittedTurn = context.LostUncommittedTurn;
        Save(storageSettings, session);
        return context;
    }

    public void Save(
        StorageSettings storageSettings,
        ResumableScenarioSession session,
        bool touchUpdatedAt = true)
    {
        ArgumentNullException.ThrowIfNull(session);
        EnsureValidSessionId(session.SessionId);

        lock (_sync)
        {
            if (touchUpdatedAt)
            {
                session.UpdatedAt = DateTimeOffset.Now;
            }

            session.Revision++;
            var directory = GetSessionDirectory(storageSettings, session.SessionId);
            Directory.CreateDirectory(directory);
            var currentPath = Path.Combine(directory, SessionFileName);
            var previousPath = Path.Combine(directory, PreviousSessionFileName);
            var temporaryPath = Path.Combine(directory, $"{SessionFileName}.{Guid.NewGuid():N}.tmp");
            try
            {
                var json = JsonSerializer.Serialize(session, JsonOptions);
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           16 * 1024,
                           FileOptions.WriteThrough))
                using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(currentPath))
                {
                    try
                    {
                        File.Replace(temporaryPath, currentPath, previousPath, ignoreMetadataErrors: true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(currentPath, previousPath, overwrite: true);
                        File.Move(temporaryPath, currentPath, overwrite: true);
                    }
                    catch (IOException)
                    {
                        File.Copy(currentPath, previousPath, overwrite: true);
                        File.Move(temporaryPath, currentPath, overwrite: true);
                    }
                }
                else
                {
                    File.Move(temporaryPath, currentPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }
    }

    public void MarkStopped(
        StorageSettings storageSettings,
        ResumableScenarioSession session,
        string stopKind,
        string reason,
        string status)
    {
        session.IsRunOpen = false;
        session.LastStopKind = stopKind;
        session.LastStopReason = reason;
        session.Status = status;
        session.LostUncommittedTurn = false;
        Save(storageSettings, session);
    }

    public void Rename(
        StorageSettings storageSettings,
        ResumableScenarioSession session,
        string title)
    {
        session.CustomTitle = title.Trim();
        Save(storageSettings, session);
    }

    public void Delete(StorageSettings storageSettings, IEnumerable<string> sessionIds)
    {
        foreach (var sessionId in sessionIds
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var directory = GetSessionDirectory(storageSettings, sessionId);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    public string GetSessionDirectory(StorageSettings storageSettings, string sessionId) =>
        Path.Combine(GetProjectsRoot(storageSettings), EnsureValidSessionId(sessionId));

    public string GetLogsDirectory(StorageSettings storageSettings, string sessionId)
    {
        var directory = Path.Combine(GetSessionDirectory(storageSettings, sessionId), "Logs");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static ResumableScenarioSession? TryLoadFromDirectory(string directory)
    {
        var directorySessionId = Path.GetFileName(directory);
        try
        {
            EnsureValidSessionId(directorySessionId);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        var currentPath = Path.Combine(directory, SessionFileName);
        var previousPath = Path.Combine(directory, PreviousSessionFileName);
        var loaded = TryLoad(currentPath) ?? TryLoad(previousPath);
        if (loaded is not null)
        {
            loaded.SessionId = directorySessionId;
            return loaded;
        }

        if (!File.Exists(currentPath) && !File.Exists(previousPath))
        {
            return null;
        }

        var timestamps = new[] { currentPath, previousPath }
            .Where(File.Exists)
            .Select(path => new FileInfo(path))
            .ToList();
        var createdAt = timestamps.Count == 0
            ? Directory.GetCreationTime(directory)
            : timestamps.Min(file => file.CreationTime);
        var updatedAt = timestamps.Count == 0
            ? Directory.GetLastWriteTime(directory)
            : timestamps.Max(file => file.LastWriteTime);
        return new ResumableScenarioSession
        {
            SessionId = directorySessionId,
            ScenarioName = ScenarioId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Status = ResumableSessionStatuses.Unavailable,
            IsRunOpen = false,
            LastStopReason = "checkpoint_unreadable"
        };
    }

    private static ResumableScenarioSession? TryLoad(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var session = JsonSerializer.Deserialize<ResumableScenarioSession>(
                File.ReadAllText(path),
                JsonOptions);
            return session is { SchemaVersion: ResumableScenarioSession.CurrentSchemaVersion }
                ? session
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string GetProjectsRoot(StorageSettings storageSettings)
    {
        var configuredRoot = storageSettings.Results.Locations
            .Select(location => location.Path?.Trim())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        return string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(
                AppDataPaths.BaseDirectory,
                "Scenarios",
                "Uncertainty",
                "Projects")
            : Path.Combine(
                configuredRoot,
                "AI_HUB",
                "Scenarios",
                "Uncertainty",
                "Projects");
    }

    private static string CreateRunId() =>
        $"run_{DateTimeOffset.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";

    private static string EnsureValidSessionId(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)
            || sessionId is "." or ".."
            || sessionId.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '_' or '-')))
        {
            throw new InvalidOperationException("The resumable session identifier is invalid.");
        }

        return sessionId;
    }
}
