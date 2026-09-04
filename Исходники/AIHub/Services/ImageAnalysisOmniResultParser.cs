using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIHub.Models;

namespace AIHub.Services;

public static class ImageAnalysisOmniResultParser
{
    public static ImageAnalysisLiteraryResult Parse(
        string visualReport,
        string response,
        IReadOnlyList<ImageAnalysisHiddenMessage> conversation,
        long visualMilliseconds,
        long composeMilliseconds,
        Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(visualReport))
        {
            throw new InvalidDataException("Omni returned an empty visual report.");
        }
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidDataException("Omni returned an empty final response.");
        }

        OmniFinalResult? result;
        var json = ExtractSingleJsonObject(response);
        try
        {
            try
            {
                result = JsonSerializer.Deserialize<OmniFinalResult>(json, JsonOptions);
            }
            catch (JsonException) when (OmniJsonSyntaxRecovery.TryRemoveDuplicateArrayCloser(json, out _))
            {
                OmniJsonSyntaxRecovery.TryRemoveDuplicateArrayCloser(json, out var recovered);
                result = JsonSerializer.Deserialize<OmniFinalResult>(recovered, JsonOptions);
                log?.Invoke("Omni JSON syntax recovered: removed one duplicate array closer; original response preserved.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Omni returned incomplete or invalid final JSON.", ex);
        }
        if (result is null || result.Paragraphs is null || result.Paragraphs.Count == 0
            || result.Paragraphs.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("Omni final JSON does not contain a complete paragraphs array.");
        }
        if (result.ReviewItems is null || result.ReviewItems.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("Omni final JSON must contain a review_items array with valid text entries.");
        }
        if (result.Uncertainties is null || result.Uncertainties.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException("Omni final JSON must contain an uncertainties array with valid text entries.");
        }

        var textParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(result.Title))
        {
            textParts.Add(result.Title.Trim());
        }
        textParts.AddRange(result.Paragraphs.Select(item => item.Trim()));
        return new ImageAnalysisLiteraryResult(
            visualReport.Trim(),
            string.Join(Environment.NewLine + Environment.NewLine, textParts),
            new ImageAnalysisReviewSummary
            {
                Items = result.ReviewItems.Select(item => item.Trim()).ToList(),
                Uncertainties = result.Uncertainties.Select(item => item.Trim()).ToList()
            },
            conversation.Select(CloneMessage).ToList(),
            response,
            visualMilliseconds,
            composeMilliseconds);
    }

    private static string ExtractSingleJsonObject(string response)
    {
        var start = response.IndexOf('{');
        if (start < 0)
        {
            return response;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = start; index < response.Length; index++)
        {
            var character = response[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }
                continue;
            }

            if (character == '"')
            {
                inString = true;
            }
            else if (character == '{')
            {
                depth++;
            }
            else if (character == '}' && --depth == 0)
            {
                if (response.IndexOf('{', index + 1) >= 0)
                {
                    throw new InvalidDataException("Omni returned more than one final JSON object.");
                }
                var suffix = response[(index + 1)..].Trim();
                if (suffix.Length > 0 && suffix != "```")
                {
                    throw new InvalidDataException("Omni returned unexpected content after the final JSON object.");
                }
                return response[start..(index + 1)];
            }
        }

        return response;
    }

    private static ImageAnalysisHiddenMessage CloneMessage(ImageAnalysisHiddenMessage item) => new()
    {
        Role = item.Role,
        Content = item.Content,
        IncludesImage = item.IncludesImage,
        CreatedAt = item.CreatedAt
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private sealed class OmniFinalResult
    {
        public string? Title { get; set; }

        public List<string>? Paragraphs { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("review_items")]
        public List<string>? ReviewItems { get; set; }

        public List<string>? Uncertainties { get; set; }
    }
}
