using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class HuggingFaceCatalogStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions JsonLineOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public HuggingFaceCatalogDatabase Load(string catalogPath, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogPath);
        if (!File.Exists(catalogPath))
        {
            return new HuggingFaceCatalogDatabase { CreatedAtUtc = nowUtc };
        }

        var json = File.ReadAllText(catalogPath, Encoding.UTF8);
        var database = JsonSerializer.Deserialize<HuggingFaceCatalogDatabase>(json, JsonOptions)
            ?? throw new InvalidDataException("Hugging Face catalog database is empty.");
        if (database.SchemaVersion != 2)
        {
            throw new InvalidDataException($"Unsupported Hugging Face catalog database schema: {database.SchemaVersion}.");
        }

        return database;
    }

    public async Task SaveAsync(
        HuggingFaceCatalogDatabase database,
        string catalogPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogPath);
        var fullPath = Path.GetFullPath(catalogPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + ".tmp";
        var json = JsonSerializer.Serialize(database, JsonOptions);
        await File.WriteAllTextAsync(temporaryPath, json, new UTF8Encoding(false), cancellationToken);
        File.Move(temporaryPath, fullPath, true);
    }

    public async Task AppendChangesAsync(
        IEnumerable<HuggingFaceCatalogChange> changes,
        string changesPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(changesPath);
        var pending = changes.ToList();
        if (pending.Count == 0)
        {
            return;
        }

        var fullPath = Path.GetFullPath(changesPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var stream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        foreach (var change in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(JsonSerializer.Serialize(change, JsonLineOptions).AsMemory(), cancellationToken);
        }
    }
}
