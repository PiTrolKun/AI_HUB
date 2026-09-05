using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class HuggingFaceCatalogCollector : IDisposable
{
    private const int MaximumApiResponseBytes = 8 * 1024 * 1024;
    private const int MaximumModelCardBytes = 2 * 1024 * 1024;
    private const int MaximumRequestAttempts = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public HuggingFaceCatalogCollector(HttpClient? httpClient = null)
    {
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LOPATA-catalog-parser/0.1 (+https://github.com/PiTrolKun/LOPATA)");
        }
    }

    public async Task<HuggingFaceCatalogSnapshot> CollectProbeAsync(
        string query,
        int limit,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        limit = Math.Clamp(limit, 1, 50);
        var sourceUrl = $"https://huggingface.co/api/models?search={Uri.EscapeDataString(query)}&limit={limit}&full=true&config=true";
        return await CollectFromSourceAsync(query, sourceUrl, limit, outputDirectory, cancellationToken);
    }

    public async Task<HuggingFaceCatalogSnapshot> CollectAuthorProbeAsync(
        string author,
        int limit,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(author);
        limit = Math.Clamp(limit, 1, 50);
        var sourceUrl = $"https://huggingface.co/api/models?author={Uri.EscapeDataString(author)}&sort=lastModified&direction=-1&limit={limit}&full=true&config=true";
        return await CollectFromSourceAsync($"author:{author}", sourceUrl, limit, outputDirectory, cancellationToken);
    }

    public async Task<HuggingFaceCatalogSnapshot> CollectLatestProbeAsync(
        int limit,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 50);
        var sourceUrl = $"https://huggingface.co/api/models?sort=lastModified&direction=-1&limit={limit}&full=true&config=true";
        return await CollectFromSourceAsync("latest_public_models", sourceUrl, limit, outputDirectory, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> SearchRepositoryIdsAsync(
        string sourceLabel,
        string sourceUrl,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var outputRoot = Path.GetFullPath(outputDirectory);
        var searchesDirectory = Path.Combine(outputRoot, "raw", "searches");
        Directory.CreateDirectory(searchesDirectory);
        var bytes = await DownloadRequiredAsync(sourceUrl, MaximumApiResponseBytes, cancellationToken);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var path = Path.Combine(searchesDirectory, $"{CreateSafeFileName(sourceLabel)}_{timestamp}.json");
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return HuggingFaceCatalogParser.ParseRepositoryIds(Encoding.UTF8.GetString(bytes));
    }

    public async Task<IReadOnlyList<HuggingFaceSearchCandidate>> SearchCandidatesAsync(
        string sourceLabel,
        string sourceUrl,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var outputRoot = Path.GetFullPath(outputDirectory);
        var searchesDirectory = Path.Combine(outputRoot, "raw", "searches");
        Directory.CreateDirectory(searchesDirectory);
        var bytes = await DownloadRequiredAsync(sourceUrl, MaximumApiResponseBytes, cancellationToken);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        var path = Path.Combine(searchesDirectory, $"{CreateSafeFileName(sourceLabel)}_{timestamp}.json");
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return HuggingFaceCatalogParser.ParseSearchCandidates(Encoding.UTF8.GetString(bytes));
    }

    public async Task<HuggingFaceCatalogEntry> CollectRepositoryEntryAsync(
        string repoId,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var outputRoot = Path.GetFullPath(outputDirectory);
        var modelsDirectory = Path.Combine(outputRoot, "raw", "models");
        var cardsDirectory = Path.Combine(outputRoot, "raw", "cards");
        Directory.CreateDirectory(modelsDirectory);
        Directory.CreateDirectory(cardsDirectory);
        return await CollectRepositoryCoreAsync(repoId, outputRoot, modelsDirectory, cardsDirectory, cancellationToken);
    }

    private async Task<HuggingFaceCatalogSnapshot> CollectFromSourceAsync(
        string sourceLabel,
        string sourceUrl,
        int limit,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var outputRoot = Path.GetFullPath(outputDirectory);
        var rawDirectory = Path.Combine(outputRoot, "raw");
        var modelsDirectory = Path.Combine(rawDirectory, "models");
        var cardsDirectory = Path.Combine(rawDirectory, "cards");
        Directory.CreateDirectory(modelsDirectory);
        Directory.CreateDirectory(cardsDirectory);

        var searchBytes = await DownloadRequiredAsync(sourceUrl, MaximumApiResponseBytes, cancellationToken);
        var searchJson = Encoding.UTF8.GetString(searchBytes);
        var rawSearchPath = Path.Combine(rawDirectory, "search.json");
        await File.WriteAllBytesAsync(rawSearchPath, searchBytes, cancellationToken);

        var snapshot = new HuggingFaceCatalogSnapshot
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Query = sourceLabel,
            SearchSourceUrl = sourceUrl,
            RawSearchRelativePath = RelativePath(outputRoot, rawSearchPath),
            RawSearchSha256 = ComputeSha256(searchBytes)
        };

        foreach (var repoId in HuggingFaceCatalogParser.ParseRepositoryIds(searchJson).Take(limit))
        {
            try
            {
                var entry = await CollectRepositoryCoreAsync(repoId, outputRoot, modelsDirectory, cardsDirectory, cancellationToken);
                snapshot.Entries.Add(entry);
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException or InvalidDataException)
            {
                snapshot.Warnings.Add($"{repoId}: {ex.Message}");
            }
        }

        if (snapshot.Entries.Count == 0)
        {
            snapshot.Warnings.Add("No public model details were parsed from the search response.");
        }

        var catalogPath = Path.Combine(outputRoot, "catalog.json");
        var catalogJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        var temporaryPath = catalogPath + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, catalogJson, new UTF8Encoding(false), cancellationToken);
        File.Move(temporaryPath, catalogPath, true);
        return snapshot;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<HuggingFaceCatalogEntry> CollectRepositoryCoreAsync(
        string repoId,
        string outputRoot,
        string modelsDirectory,
        string cardsDirectory,
        CancellationToken cancellationToken)
    {
        var escapedRepoId = Uri.EscapeDataString(repoId).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
        var apiUrl = $"https://huggingface.co/api/models/{escapedRepoId}";
        var detailBytes = await DownloadRequiredAsync(apiUrl, MaximumApiResponseBytes, cancellationToken);
        var detailJson = Encoding.UTF8.GetString(detailBytes);
        var revision = ReadRevisionSha(detailJson);
        var revisionSuffix = CreateSafeFileName(revision.Length > 12 ? revision[..12] : revision);
        var safeName = CreateSafeFileName(repoId);
        var rawApiPath = Path.Combine(modelsDirectory, $"{safeName}_{revisionSuffix}.json");
        await File.WriteAllBytesAsync(rawApiPath, detailBytes, cancellationToken);

        var modelCardUrl = $"https://huggingface.co/{escapedRepoId}/resolve/{Uri.EscapeDataString(revision)}/README.md";
        var cardBytes = await DownloadOptionalAsync(modelCardUrl, MaximumModelCardBytes, cancellationToken);
        var modelCard = cardBytes is null ? string.Empty : Encoding.UTF8.GetString(cardBytes);
        var rawCardPath = cardBytes is null ? string.Empty : Path.Combine(cardsDirectory, $"{safeName}_{revisionSuffix}.md");
        if (cardBytes is not null)
        {
            await File.WriteAllBytesAsync(rawCardPath, cardBytes, cancellationToken);
        }

        return HuggingFaceCatalogParser.ParseModel(
            detailJson,
            modelCard,
            apiUrl,
            modelCardUrl,
            DateTimeOffset.UtcNow,
            RelativePath(outputRoot, rawApiPath),
            ComputeSha256(detailBytes),
            string.IsNullOrWhiteSpace(rawCardPath) ? string.Empty : RelativePath(outputRoot, rawCardPath),
            cardBytes is null ? string.Empty : ComputeSha256(cardBytes));
    }

    private async Task<byte[]> DownloadRequiredAsync(string url, int maximumBytes, CancellationToken cancellationToken)
    {
        var bytes = await DownloadAsync(url, maximumBytes, cancellationToken);
        return bytes ?? throw new HttpRequestException($"Required Hugging Face resource was not found: {url}");
    }

    private async Task<byte[]?> DownloadOptionalAsync(string url, int maximumBytes, CancellationToken cancellationToken) =>
        await DownloadAsync(url, maximumBytes, cancellationToken, optional: true);

    private async Task<byte[]?> DownloadAsync(
        string url,
        int maximumBytes,
        CancellationToken cancellationToken,
        bool optional = false)
    {
        for (var attempt = 1; attempt <= MaximumRequestAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (optional && response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if ((response.StatusCode == HttpStatusCode.TooManyRequests
                    || (int)response.StatusCode >= 500)
                && attempt < MaximumRequestAttempts)
            {
                var retryDelay = response.Headers.RetryAfter?.Delta
                    ?? TimeSpan.FromMilliseconds(400 * attempt);
                await Task.Delay(retryDelay > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : retryDelay, cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > 0 and var contentLength && contentLength > maximumBytes)
            {
                throw new InvalidDataException($"Hugging Face response exceeded the {maximumBytes} byte safety limit.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            while (true)
            {
                var read = await stream.ReadAsync(chunk, cancellationToken);
                if (read == 0)
                {
                    return buffer.ToArray();
                }

                if (buffer.Length + read > maximumBytes)
                {
                    throw new InvalidDataException($"Hugging Face response exceeded the {maximumBytes} byte safety limit.");
                }

                await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }
        }

        throw new HttpRequestException($"Hugging Face request failed after {MaximumRequestAttempts} attempts: {url}");
    }

    private static string ReadRevisionSha(string detailJson)
    {
        using var document = JsonDocument.Parse(detailJson);
        return document.RootElement.TryGetProperty("sha", out var sha)
            && sha.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(sha.GetString())
                ? sha.GetString()!
                : "main";
    }

    private static string ComputeSha256(byte[] bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static string RelativePath(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string CreateSafeFileName(string repoId)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(repoId.Select(character => invalid.Contains(character) || character == '/' ? '_' : character).ToArray());
        return safe.Length <= 120 ? safe : safe[..120];
    }
}
