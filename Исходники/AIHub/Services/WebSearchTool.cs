using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed partial class WebSearchTool
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };
    private readonly WebSearchRerankerService _rerankerService = new();

    public async Task<WebSearchResponse> SearchAsync(
        string query,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var requestUri = new Uri($"https://lite.duckduckgo.com/lite/?q={Uri.EscapeDataString(query)}");
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 AI_HUB");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var items = ResultRegex()
            .Matches(html)
            .Select((match, index) => new WebSearchResult
            {
                OriginalRank = index + 1,
                RerankedRank = index + 1,
                Title = CleanHtml(match.Groups["title"].Value),
                Url = DecodeDuckDuckGoUrl(match.Groups["href"].Value),
                Snippet = CleanHtml(match.Groups["snippet"].Value)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Url))
            .Take(10)
            .ToList();

        var result = new WebSearchResponse
        {
            Query = query,
            Provider = "DuckDuckGo Lite",
            Results = items
        };
        result.Rerank = await _rerankerService.RerankAsync(query, result.Results, storageSettings, cancellationToken);

        var path = WebToolPathService.CreateStampedPath(
            WebToolPathService.GetSearchDirectory(storageSettings),
            "search",
            ".json");
        result.SavedPath = path;
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(result, JsonOptions), Encoding.UTF8, cancellationToken);
        return result;
    }

    private static string CleanHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutTags = TagRegex().Replace(value, " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    private static string DecodeDuckDuckGoUrl(string href)
    {
        var decoded = System.Net.WebUtility.HtmlDecode(href).Trim();
        if (decoded.StartsWith("//", StringComparison.Ordinal))
        {
            decoded = "https:" + decoded;
        }

        var marker = "uddg=";
        var markerIndex = decoded.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return decoded;
        }

        var valueStart = markerIndex + marker.Length;
        var valueEnd = decoded.IndexOf('&', valueStart);
        var encodedTarget = valueEnd < 0 ? decoded[valueStart..] : decoded[valueStart..valueEnd];
        return Uri.UnescapeDataString(encodedTarget);
    }

    [GeneratedRegex("<a[^>]+href=\"(?<href>[^\"]+)\"[^>]+class=['\"]result-link['\"][^>]*>(?<title>.*?)</a>.*?(?:<td class=['\"]result-snippet['\"]>(?<snippet>.*?)</td>)?", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ResultRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
