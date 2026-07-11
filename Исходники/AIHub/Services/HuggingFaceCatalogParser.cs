using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public static partial class HuggingFaceCatalogParser
{
    private const int MaximumDescriptionLength = 1200;

    public static IReadOnlyList<string> ParseRepositoryIds(string searchJson)
    {
        using var document = JsonDocument.Parse(searchJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Hugging Face search response must be an array.");
        }

        return document.RootElement
            .EnumerateArray()
            .Select(item => ReadString(item, "id"))
            .Where(repoId => !string.IsNullOrWhiteSpace(repoId) && repoId.Contains('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<HuggingFaceSearchCandidate> ParseSearchCandidates(string searchJson)
    {
        using var document = JsonDocument.Parse(searchJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Hugging Face search response must be an array.");
        }

        var result = new List<HuggingFaceSearchCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var repoId = ReadString(item, "id");
            if (string.IsNullOrWhiteSpace(repoId) || !repoId.Contains('/') || !seen.Add(repoId))
            {
                continue;
            }

            var safetensors = ReadObject(item, "safetensors");
            var gguf = ReadObject(item, "gguf");
            result.Add(new HuggingFaceSearchCandidate
            {
                RepoId = repoId,
                Author = ReadString(item, "author"),
                CreatedAtUtc = ReadDate(item, "createdAt"),
                Downloads = ReadLong(item, "downloads"),
                Likes = ReadLong(item, "likes"),
                TrendingScore = ReadDouble(item, "trendingScore"),
                PipelineTag = ReadString(item, "pipeline_tag"),
                ParameterCount = ReadLong(safetensors, "total") ?? ReadLong(gguf, "total")
            });
        }

        return result;
    }

    public static HuggingFaceCatalogEntry ParseModel(
        string detailJson,
        string modelCardMarkdown,
        string apiSourceUrl,
        string modelCardSourceUrl,
        DateTimeOffset retrievedAtUtc,
        string rawApiRelativePath,
        string rawApiSha256,
        string rawModelCardRelativePath,
        string rawModelCardSha256)
    {
        using var document = JsonDocument.Parse(detailJson);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Hugging Face model response must be an object.");
        }

        var tags = ReadStringList(root, "tags");
        var cardData = ReadObject(root, "cardData");
        var config = ReadObject(root, "config");
        var gguf = ReadObject(root, "gguf");
        var safetensors = ReadObject(root, "safetensors");
        var entry = new HuggingFaceCatalogEntry
        {
            RepoId = ReadString(root, "id"),
            Author = ReadString(root, "author"),
            RevisionSha = ReadString(root, "sha"),
            CreatedAtUtc = ReadDate(root, "createdAt"),
            LastModifiedUtc = ReadDate(root, "lastModified"),
            Downloads = ReadLong(root, "downloads"),
            Likes = ReadLong(root, "likes"),
            TrendingScore = ReadDouble(root, "trendingScore"),
            PipelineTag = FirstNonEmpty(ReadString(root, "pipeline_tag"), ReadString(cardData, "pipeline_tag")),
            LibraryName = ReadString(root, "library_name"),
            License = FirstNonEmpty(ReadString(cardData, "license"), ReadTagValue(tags, "license:")),
            LicenseUrl = ReadString(cardData, "license_link"),
            IsGated = ReadAccessFlag(root, "gated"),
            IsPrivate = ReadBool(root, "private"),
            IsDisabled = ReadBool(root, "disabled"),
            BaseModels = ReadBaseModels(cardData, tags),
            BaseModelRelation = FirstNonEmpty(ReadString(cardData, "base_model_relation"), ReadBaseModelRelation(tags)),
            Languages = ReadLanguages(cardData, tags),
            Datasets = ReadDatasets(cardData, tags),
            Tags = tags,
            Architectures = ReadStringList(config, "architectures"),
            ModelType = ReadString(config, "model_type"),
            GgufArchitecture = ReadString(gguf, "architecture"),
            ParameterCount = ReadLong(gguf, "total") ?? ReadLong(safetensors, "total"),
            ContextLength = ReadLong(gguf, "context_length"),
            TotalFileSizeBytes = ReadLong(gguf, "totalFileSize"),
            AuthorDescription = ExtractAuthorDescription(modelCardMarkdown),
            IsRevisionPinned = !string.IsNullOrWhiteSpace(ReadString(root, "sha")),
            ApiSourceUrl = apiSourceUrl,
            ModelCardSourceUrl = modelCardSourceUrl,
            RawApiRelativePath = rawApiRelativePath,
            RawApiSha256 = rawApiSha256,
            RawModelCardRelativePath = rawModelCardRelativePath,
            RawModelCardSha256 = rawModelCardSha256,
            RetrievedAtUtc = retrievedAtUtc
        };

        AddCompletenessWarnings(entry);
        return entry;
    }

    public static string ExtractAuthorDescription(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var inFrontMatter = lines.Length > 0 && lines[0].Trim() == "---";
        var inCodeFence = false;
        var paragraph = new List<string>();

        for (var index = inFrontMatter ? 1 : 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (inFrontMatter)
            {
                if (line == "---")
                {
                    inFrontMatter = false;
                }
                continue;
            }

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence || IsDecorativeMarkdownLine(line))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (paragraph.Count > 0)
                {
                    break;
                }
                continue;
            }

            paragraph.Add(line);
            if (paragraph.Sum(value => value.Length) >= MaximumDescriptionLength)
            {
                break;
            }
        }

        var description = string.Join(' ', paragraph);
        description = MarkdownLinkRegex().Replace(description, "$1");
        description = HtmlTagRegex().Replace(description, string.Empty);
        description = MarkdownEmphasisRegex().Replace(description, string.Empty);
        description = WhitespaceRegex().Replace(description, " ").Trim();
        return description.Length <= MaximumDescriptionLength
            ? description
            : description[..MaximumDescriptionLength].TrimEnd() + "...";
    }

    private static void AddCompletenessWarnings(HuggingFaceCatalogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.RepoId))
        {
            entry.Warnings.Add("Missing repository id.");
        }
        if (string.IsNullOrWhiteSpace(entry.RevisionSha))
        {
            entry.Warnings.Add("Missing revision SHA; source cannot be pinned exactly.");
        }
        if (string.IsNullOrWhiteSpace(entry.License))
        {
            entry.Warnings.Add("License is not declared in Hub metadata.");
        }
        if (entry.BaseModels.Count == 0)
        {
            entry.Warnings.Add("Base model is not declared.");
        }
        if (entry.ParameterCount is null)
        {
            entry.Warnings.Add("Parameter count is not available in GGUF metadata.");
        }
        if (string.IsNullOrWhiteSpace(entry.AuthorDescription))
        {
            entry.Warnings.Add("Model Card has no short prose description.");
        }
        if (entry.IsGated || entry.IsPrivate || entry.IsDisabled)
        {
            entry.Warnings.Add("Repository access is restricted or disabled.");
        }
    }

    private static bool IsDecorativeMarkdownLine(string line) =>
        string.IsNullOrWhiteSpace(line)
        || line.StartsWith('#')
        || line.StartsWith('!')
        || line.StartsWith('<')
        || line.StartsWith('|')
        || line.StartsWith("[![", StringComparison.Ordinal)
        || line.StartsWith("- [", StringComparison.Ordinal)
        || line.All(character => character is '-' or '=' or '_' or ' ');

    private static JsonElement ReadObject(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.Object
            ? property
            : default;

    private static string ReadString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static long? ReadLong(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.TryGetInt64(out var integer))
        {
            return integer;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var number)
            ? Convert.ToInt64(number, CultureInfo.InvariantCulture)
            : null;
    }

    private static double? ReadDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value)
            ? value
            : null;
    }

    private static bool ReadBool(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.True;

    private static bool ReadAccessFlag(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return property.ValueKind == JsonValueKind.True
            || property.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(property.GetString())
                && !string.Equals(property.GetString(), "false", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? ReadDate(JsonElement element, string propertyName) =>
        DateTimeOffset.TryParse(
            ReadString(element, propertyName),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var value)
                ? value.ToUniversalTime()
                : null;

    private static List<string> ReadStringList(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return [];
        }

        return ReadStringOrArray(property);
    }

    private static List<string> ReadStringOrArray(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return [element.GetString() ?? string.Empty];
        }

        return element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString() ?? string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : [];
    }

    private static List<string> ReadBaseModels(JsonElement cardData, IReadOnlyList<string> tags)
    {
        var result = ReadStringList(cardData, "base_model");
        foreach (var tag in tags.Where(tag => tag.StartsWith("base_model:", StringComparison.OrdinalIgnoreCase)))
        {
            var value = tag["base_model:".Length..];
            var relationSeparator = value.IndexOf(':');
            if (relationSeparator >= 0 && value[(relationSeparator + 1)..].Contains('/'))
            {
                value = value[(relationSeparator + 1)..];
            }
            if (value.Contains('/') && !result.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(value);
            }
        }
        return result;
    }

    private static string ReadBaseModelRelation(IReadOnlyList<string> tags)
    {
        var tag = tags.FirstOrDefault(value => value.StartsWith("base_model:", StringComparison.OrdinalIgnoreCase));
        if (tag is null)
        {
            return string.Empty;
        }

        var value = tag["base_model:".Length..];
        var separator = value.IndexOf(':');
        return separator > 0 && value[(separator + 1)..].Contains('/') ? value[..separator] : string.Empty;
    }

    private static List<string> ReadLanguages(JsonElement cardData, IReadOnlyList<string> tags)
    {
        var result = ReadStringList(cardData, "language");
        foreach (var tag in tags.Where(tag => LanguageTagRegex().IsMatch(tag) || tag == "multilingual"))
        {
            if (!result.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(tag);
            }
        }
        return result;
    }

    private static List<string> ReadDatasets(JsonElement cardData, IReadOnlyList<string> tags)
    {
        var result = ReadStringList(cardData, "datasets");
        foreach (var value in tags
            .Where(tag => tag.StartsWith("dataset:", StringComparison.OrdinalIgnoreCase))
            .Select(tag => tag["dataset:".Length..]))
        {
            if (!result.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(value);
            }
        }
        return result;
    }

    private static string ReadTagValue(IEnumerable<string> tags, string prefix) =>
        tags.FirstOrDefault(tag => tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..]
        ?? string.Empty;

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    [GeneratedRegex(@"\[([^\]]+)\]\([^\)]+\)", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"[*_`]+", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownEmphasisRegex();

    [GeneratedRegex(@"<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^[a-z]{2,3}(?:-[A-Z]{2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex LanguageTagRegex();
}
