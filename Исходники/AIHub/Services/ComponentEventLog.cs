using System.IO;
using System.Text.Json;

namespace AIHub.Services;

public sealed class ComponentEventLog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _sync = new();

    public void Write(string eventName, object? data = null)
    {
        AppDataPaths.EnsureComponentDirectories();
        var record = new
        {
            Timestamp = DateTimeOffset.Now,
            Event = eventName,
            Data = data
        };
        var line = JsonSerializer.Serialize(record, JsonOptions);
        var path = Path.Combine(
            AppDataPaths.ComponentLogsDirectory,
            $"components-{DateTimeOffset.Now:yyyy-MM}.jsonl");
        lock (_sync)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}
