using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class JsonlSessionLog : IDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly object _sync = new();
    private bool _disposed;

    public JsonlSessionLog(string filePath)
    {
        FilePath = filePath;
        SessionId = Path.GetFileNameWithoutExtension(filePath);
    }

    public string FilePath { get; }

    public string SessionId { get; }

    public static JsonlSessionLog CreateCore(StorageSettings storageSettings)
    {
        var directory = SessionPathService.GetCoreSessionsDirectory(storageSettings);
        return new JsonlSessionLog(SessionPathService.CreateSessionFilePath(directory, "core"));
    }

    public static JsonlSessionLog CreateDebugModelTester(StorageSettings storageSettings)
    {
        var directory = SessionPathService.GetDebugModelTesterSessionsDirectory(storageSettings);
        return new JsonlSessionLog(SessionPathService.CreateSessionFilePath(directory, "debug-model-tester"));
    }

    public void Write(string type, object? payload = null)
    {
        if (_disposed)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        var entry = new
        {
            Type = type,
            SessionId,
            LocalTime = now,
            UtcTime = now.ToUniversalTime(),
            Payload = payload
        };

        var line = JsonSerializer.Serialize(entry, JsonOptions);
        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.AppendAllText(FilePath, line + Environment.NewLine, Utf8NoBom);
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
