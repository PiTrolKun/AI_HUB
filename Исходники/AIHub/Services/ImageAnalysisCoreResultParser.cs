using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public static class ImageAnalysisCoreResultParser
{
    private const int MaximumReviewItems = 6;
    private const int MaximumUncertainties = 2;
    private const int MaximumItemLength = 110;

    public static (string Description, ImageAnalysisReviewSummary Summary) Parse(string? modelText)
    {
        var normalized = RemoveCodeFence(modelText);
        if (TryParseJson(normalized, out var description, out var summary))
        {
            return (description, summary);
        }

        if (LooksLikeStructuredEnvelope(normalized))
        {
            throw new InvalidDataException("The LOPATA core returned an incomplete structured result.");
        }

        return (normalized.Trim(), new ImageAnalysisReviewSummary());
    }

    private static bool TryParseJson(
        string value,
        out string description,
        out ImageAnalysisReviewSummary summary)
    {
        description = string.Empty;
        summary = new ImageAnalysisReviewSummary();
        var firstBrace = value.IndexOf('{');
        var lastBrace = value.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(value[firstBrace..(lastBrace + 1)]);
            description = ReadDescription(document.RootElement);
            if (string.IsNullOrWhiteSpace(description))
            {
                return false;
            }

            summary.Items = ReadShortItems(document.RootElement, "review_items", MaximumReviewItems);
            summary.Uncertainties = ReadShortItems(document.RootElement, "uncertainties", MaximumUncertainties);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ReadDescription(JsonElement root)
    {
        if (root.TryGetProperty("paragraphs", out var paragraphsProperty)
            && paragraphsProperty.ValueKind == JsonValueKind.Array)
        {
            var paragraphs = paragraphsProperty
                .EnumerateArray()
                .Where(element => element.ValueKind == JsonValueKind.String)
                .Select(element => element.GetString()?.Trim() ?? string.Empty)
                .Where(paragraph => paragraph.Length > 0)
                .ToList();
            if (paragraphs.Count > 0)
            {
                var title = root.TryGetProperty("title", out var titleProperty)
                    && titleProperty.ValueKind == JsonValueKind.String
                    ? titleProperty.GetString()?.Trim() ?? string.Empty
                    : string.Empty;
                if (string.Equals(title, "null", StringComparison.OrdinalIgnoreCase))
                {
                    title = string.Empty;
                }
                return string.Join(
                    $"{Environment.NewLine}{Environment.NewLine}",
                    title.Length > 0 ? new[] { title }.Concat(paragraphs) : paragraphs);
            }
        }

        return root.TryGetProperty("description", out var descriptionProperty)
            && descriptionProperty.ValueKind == JsonValueKind.String
            ? descriptionProperty.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }

    private static List<string> ReadShortItems(JsonElement root, string propertyName, int maximum)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in property.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var item = NormalizeItem(element.GetString());
            if (item.Length == 0 || !seen.Add(item))
            {
                continue;
            }

            results.Add(item);
            if (results.Count >= maximum)
            {
                break;
            }
        }
        return results;
    }

    private static string NormalizeItem(string? value)
    {
        var item = (value ?? string.Empty).Trim()
            .TrimStart('-', '*', '•', '–', '—', ' ')
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        while (item.Contains("  ", StringComparison.Ordinal))
        {
            item = item.Replace("  ", " ", StringComparison.Ordinal);
        }
        return item.Length <= MaximumItemLength
            ? item
            : item[..(MaximumItemLength - 1)].TrimEnd() + "…";
    }

    private static string RemoveCodeFence(string? modelText)
    {
        var value = (modelText ?? string.Empty).Trim();
        if (!value.StartsWith("```", StringComparison.Ordinal))
        {
            return value;
        }

        var firstLine = value.IndexOf('\n');
        var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);
        return firstLine >= 0 && lastFence > firstLine
            ? value[(firstLine + 1)..lastFence].Trim()
            : value;
    }

    private static bool LooksLikeStructuredEnvelope(string value) =>
        value.TrimStart().StartsWith('{')
        || value.Contains("\"description\"", StringComparison.OrdinalIgnoreCase)
        || value.Contains("\"paragraphs\"", StringComparison.OrdinalIgnoreCase)
        || value.Contains("\"review_items\"", StringComparison.OrdinalIgnoreCase);
}
