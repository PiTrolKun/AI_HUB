using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed class HuggingFaceProviderTool
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<HuggingFaceFindModelResponse> FindModelAsync(
        string argument,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var options = ParseOptions(argument);
        var role = GetOption(options, "role", "general");
        var format = GetOption(options, "format", string.Empty);
        var license = GetOption(options, "license", string.Empty);
        var maxSizeBytes = ParseSize(GetOption(options, "max_size", string.Empty));
        var query = GetOption(options, "query", BuildDefaultQuery(role, format));

        var response = new HuggingFaceFindModelResponse
        {
            Role = role,
            Query = query,
            Format = format,
            License = license,
            MaxSizeBytes = maxSizeBytes
        };

        var searchUri = new Uri($"https://huggingface.co/api/models?search={Uri.EscapeDataString(query)}&limit=10&full=true&config=false");
        using var request = new HttpRequestMessage(HttpMethod.Get, searchUri);
        request.Headers.UserAgent.ParseAdd("AI_HUB/0.1 (+https://github.com/PiTrolKun/AI_HUB)");
        using var searchResponse = await _httpClient.SendAsync(request, cancellationToken);
        searchResponse.EnsureSuccessStatusCode();

        var json = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        foreach (var model in document.RootElement.EnumerateArray())
        {
            var repoId = ReadString(model, "id");
            if (string.IsNullOrWhiteSpace(repoId))
            {
                continue;
            }

            var candidate = CreateCandidateFromSearch(model);
            await EnrichFilesAsync(candidate, format, maxSizeBytes, cancellationToken);

            if (!string.IsNullOrWhiteSpace(license)
                && !string.Equals(candidate.License, license, StringComparison.OrdinalIgnoreCase))
            {
                candidate.Warnings.Add($"License differs from requested: {candidate.License}");
            }

            if (!string.IsNullOrWhiteSpace(format) && candidate.Files.Count == 0)
            {
                candidate.Warnings.Add($"No matching {format} files found in repository metadata.");
            }

            response.Candidates.Add(candidate);
        }

        response.Candidates = response.Candidates
            .OrderBy(candidate => candidate.Warnings.Count)
            .ThenByDescending(candidate => candidate.Downloads ?? 0)
            .ThenByDescending(candidate => candidate.Likes ?? 0)
            .Take(5)
            .ToList();

        if (response.Candidates.Count == 0)
        {
            response.Warnings.Add("No Hugging Face candidates returned by API.");
        }

        response.SavedPath = WebToolPathService.CreateStampedPath(
            Path.Combine(WebToolPathService.GetWebRoot(storageSettings), "HuggingFace"),
            "hf_find_model",
            ".json");
        await File.WriteAllTextAsync(response.SavedPath, JsonSerializer.Serialize(response, JsonOptions), Encoding.UTF8, cancellationToken);
        return response;
    }

    public async Task<HuggingFaceModelCandidate> GetModelFilesAsync(
        string repoId,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var candidate = new HuggingFaceModelCandidate { RepoId = repoId.Trim() };
        await EnrichFilesAsync(candidate, string.Empty, null, cancellationToken);

        var path = WebToolPathService.CreateStampedPath(
            Path.Combine(WebToolPathService.GetWebRoot(storageSettings), "HuggingFace"),
            "hf_model_files",
            ".json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(candidate, JsonOptions), Encoding.UTF8, cancellationToken);
        return candidate;
    }

    private async Task EnrichFilesAsync(
        HuggingFaceModelCandidate candidate,
        string requestedFormat,
        long? maxSizeBytes,
        CancellationToken cancellationToken)
    {
        var repoId = candidate.RepoId;
        if (string.IsNullOrWhiteSpace(repoId))
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://huggingface.co/api/models/{repoId}?blobs=true"));
        request.Headers.UserAgent.ParseAdd("AI_HUB/0.1 (+https://github.com/PiTrolKun/AI_HUB)");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            candidate.Warnings.Add($"Hugging Face model details failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            return;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        candidate.Author = string.IsNullOrWhiteSpace(candidate.Author) ? ReadString(root, "author") : candidate.Author;
        candidate.PipelineTag = string.IsNullOrWhiteSpace(candidate.PipelineTag) ? ReadString(root, "pipeline_tag") : candidate.PipelineTag;
        candidate.Tags = candidate.Tags.Count == 0 ? ReadTags(root) : candidate.Tags;
        candidate.License = string.IsNullOrWhiteSpace(candidate.License) ? ReadLicense(root, candidate.Tags) : candidate.License;
        candidate.LastModified ??= ReadDate(root, "lastModified");
        candidate.Downloads ??= ReadInt(root, "downloads");
        candidate.Likes ??= ReadInt(root, "likes");

        if (!root.TryGetProperty("siblings", out var siblings) || siblings.ValueKind != JsonValueKind.Array)
        {
            candidate.Warnings.Add("Model metadata has no siblings list.");
            return;
        }

        var files = new List<HuggingFaceModelFile>();
        foreach (var sibling in siblings.EnumerateArray())
        {
            var fileName = ReadString(sibling, "rfilename");
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var size = ReadLfsSize(sibling);
            var matchesFormat = string.IsNullOrWhiteSpace(requestedFormat)
                || fileName.EndsWith("." + requestedFormat.TrimStart('.'), StringComparison.OrdinalIgnoreCase);
            if (!matchesFormat)
            {
                continue;
            }

            if (maxSizeBytes is not null && size is not null && size.Value > maxSizeBytes.Value)
            {
                continue;
            }

            files.Add(new HuggingFaceModelFile
            {
                FileName = fileName,
                SizeBytes = size,
                LfsOid = ReadLfsOid(sibling),
                DownloadUrl = $"https://huggingface.co/{repoId}/resolve/main/{Uri.EscapeDataString(fileName).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)}",
                MatchesFormat = matchesFormat
            });
        }

        candidate.Files = files
            .OrderBy(file => file.SizeBytes ?? long.MaxValue)
            .ThenBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }

    private static HuggingFaceModelCandidate CreateCandidateFromSearch(JsonElement model)
    {
        var tags = ReadTags(model);
        return new HuggingFaceModelCandidate
        {
            RepoId = ReadString(model, "id"),
            Author = ReadString(model, "author"),
            PipelineTag = ReadString(model, "pipeline_tag"),
            License = ReadLicense(model, tags),
            LastModified = ReadDate(model, "lastModified"),
            Downloads = ReadInt(model, "downloads"),
            Likes = ReadInt(model, "likes"),
            Tags = tags
        };
    }

    private static string BuildDefaultQuery(string role, string format)
    {
        var baseQuery = role.ToLowerInvariant() switch
        {
            "embedding" => "embedding text retrieval",
            "reranker" => "reranker cross encoder",
            "core" => "instruction gguf qwen",
            _ => role
        };

        return string.IsNullOrWhiteSpace(format) ? baseQuery : baseQuery + " " + format;
    }

    private static Dictionary<string, string> ParseOptions(string argument)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matches = Regex.Matches(argument, @"(?<key>[A-Za-z_][A-Za-z0-9_]*)=");
        if (matches.Count == 0)
        {
            result["query"] = argument.Trim();
            return result;
        }

        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var valueStart = match.Index + match.Length;
            var valueEnd = index + 1 < matches.Count ? matches[index + 1].Index : argument.Length;
            if (valueEnd <= valueStart)
            {
                continue;
            }

            var key = match.Groups["key"].Value.Trim();
            var value = argument[valueStart..valueEnd].Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static string GetOption(IReadOnlyDictionary<string, string> options, string key, string fallback)
    {
        return options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static long? ParseSize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        var multiplier = normalized.EndsWith("GB", StringComparison.Ordinal) ? 1024L * 1024L * 1024L
            : normalized.EndsWith("MB", StringComparison.Ordinal) ? 1024L * 1024L
            : normalized.EndsWith("KB", StringComparison.Ordinal) ? 1024L
            : 1L;
        normalized = normalized.Replace("GB", string.Empty, StringComparison.Ordinal)
            .Replace("MB", string.Empty, StringComparison.Ordinal)
            .Replace("KB", string.Empty, StringComparison.Ordinal)
            .Trim();

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? (long)(number * multiplier)
            : null;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value) ? value : null;
    }

    private static DateTimeOffset? ReadDate(JsonElement element, string propertyName)
    {
        return DateTimeOffset.TryParse(ReadString(element, propertyName), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
            ? value
            : null;
    }

    private static List<string> ReadTags(JsonElement element)
    {
        if (!element.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return tags.EnumerateArray()
            .Where(tag => tag.ValueKind == JsonValueKind.String)
            .Select(tag => tag.GetString() ?? string.Empty)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ReadLicense(JsonElement element, IReadOnlyList<string> tags)
    {
        if (element.TryGetProperty("cardData", out var cardData)
            && cardData.ValueKind == JsonValueKind.Object
            && cardData.TryGetProperty("license", out var license)
            && license.ValueKind == JsonValueKind.String)
        {
            return license.GetString() ?? string.Empty;
        }

        var licenseTag = tags.FirstOrDefault(tag => tag.StartsWith("license:", StringComparison.OrdinalIgnoreCase));
        return licenseTag is null ? string.Empty : licenseTag["license:".Length..];
    }

    private static long? ReadLfsSize(JsonElement sibling)
    {
        return sibling.TryGetProperty("lfs", out var lfs)
            && lfs.ValueKind == JsonValueKind.Object
            && lfs.TryGetProperty("size", out var size)
            && size.TryGetInt64(out var value)
                ? value
                : null;
    }

    private static string ReadLfsOid(JsonElement sibling)
    {
        return sibling.TryGetProperty("lfs", out var lfs)
            && lfs.ValueKind == JsonValueKind.Object
            && lfs.TryGetProperty("oid", out var oid)
            && oid.ValueKind == JsonValueKind.String
                ? oid.GetString() ?? string.Empty
                : string.Empty;
    }
}
