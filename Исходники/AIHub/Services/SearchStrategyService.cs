using System.IO;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed partial class SearchStrategyService
{
    private const int MaxQueries = 6;
    private const int MaxSources = 10;
    private const int MaxReadPages = 3;
    private const int MaxPreviewCharacters = 1400;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly WebSearchTool _searchTool = new();
    private readonly WebPageReaderTool _pageReaderTool = new();

    public async Task<WebResearchResponse> ResearchAsync(
        string task,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var result = new WebResearchResponse
        {
            Task = task.Trim()
        };
        result.GeneratedQueries = BuildQueries(task);

        var sourceByUrl = new Dictionary<string, WebResearchSource>(StringComparer.OrdinalIgnoreCase);
        foreach (var query in result.GeneratedQueries.Take(MaxQueries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            WebSearchResponse search;
            try
            {
                search = await _searchTool.SearchAsync(query, storageSettings, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                result.Attempts.Add(new WebResearchAttempt
                {
                    Query = query,
                    Provider = "none",
                    Status = "error",
                    ResultCount = 0,
                    HttpStatusCode = 0,
                    PossibleReason = ex.Message,
                    SavedPath = string.Empty
                });
                continue;
            }

            result.Attempts.Add(new WebResearchAttempt
            {
                Query = search.Query,
                Provider = search.Provider,
                Status = search.Status,
                ResultCount = search.ResultCount,
                HttpStatusCode = search.HttpStatusCode,
                PossibleReason = search.PossibleReason,
                SavedPath = search.SavedPath
            });

            foreach (var item in search.Results)
            {
                if (sourceByUrl.Count >= MaxSources)
                {
                    break;
                }

                if (!IsUsefulSourceUrl(item.Url)
                    || !IsRelevantToTask(result.Task, query, item)
                    || sourceByUrl.ContainsKey(item.Url))
                {
                    continue;
                }

                sourceByUrl[item.Url] = new WebResearchSource
                {
                    Title = item.Title,
                    Url = item.Url,
                    Snippet = item.Snippet,
                    Query = search.Query,
                    Provider = search.Provider,
                    Score = item.RerankScore
                };
            }

            if (sourceByUrl.Count >= 8 && result.Attempts.Count >= 3)
            {
                break;
            }

            await Task.Delay(250, cancellationToken);
        }

        result.Sources = sourceByUrl.Values
            .OrderByDescending(source => source.Score ?? 0)
            .ThenBy(source => source.Title, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSources)
            .ToList();

        foreach (var source in result.Sources.Take(MaxReadPages))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = new WebResearchPage
            {
                Url = source.Url,
                Title = source.Title
            };

            try
            {
                var read = await _pageReaderTool.ReadAsync(source.Url, storageSettings, cancellationToken);
                page.Title = string.IsNullOrWhiteSpace(read.Title) ? source.Title : read.Title;
                page.TextPreview = Limit(read.Text, MaxPreviewCharacters);
                page.SavedPath = read.SavedPath;
                source.WasRead = true;
                AddDatedItems(result, read.Text, page.Title, source.Url);
            }
            catch (Exception ex)
            {
                page.Error = ex.Message;
            }

            result.ReadPages.Add(page);
            await Task.Delay(250, cancellationToken);
        }

        result.ConfirmedSourceCount = result.ReadPages.Count(page => string.IsNullOrWhiteSpace(page.Error) && !string.IsNullOrWhiteSpace(page.TextPreview));
        ApplyDiagnosis(result);

        result.SavedPath = WebToolPathService.CreateStampedPath(
            WebToolPathService.GetResearchDirectory(storageSettings),
            "research",
            ".json");
        await File.WriteAllTextAsync(result.SavedPath, JsonSerializer.Serialize(result, JsonOptions), Encoding.UTF8, cancellationToken);
        return result;
    }

    private static List<string> BuildQueries(string task)
    {
        var cleaned = NormalizeQuery(task);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return [];
        }

        var queries = new List<string> { cleaned };
        var exactQuoted = cleaned.StartsWith('"') && cleaned.EndsWith('"') && cleaned.Length > 2;
        if (exactQuoted)
        {
            return queries;
        }

        var simplified = SimplifyQuery(cleaned);
        AddUnique(queries, simplified);

        if (LooksLikeSpaceNews(cleaned))
        {
            AddUnique(queries, "latest space news");
            AddUnique(queries, "space news NASA ESA Reuters Space.com");
            AddUnique(queries, "NASA latest space news");
            AddUnique(queries, "ESA latest space news");
            AddUnique(queries, "Reuters space news");
            return queries;
        }

        if (LooksLikeNews(cleaned))
        {
            var english = BuildSimpleEnglishNewsQuery(cleaned);
            AddUnique(queries, english);
            AddUnique(queries, english + " Reuters");
            AddUnique(queries, english + " BBC");
            AddUnique(queries, simplified + " новости");
            return queries;
        }

        AddUnique(queries, BuildSimpleEnglishQuery(cleaned));
        AddUnique(queries, simplified + " official");
        AddUnique(queries, simplified + " documentation");
        return queries;
    }

    private static void ApplyDiagnosis(WebResearchResponse result)
    {
        if (result.ConfirmedSourceCount > 0)
        {
            result.Status = "ok";
            result.Diagnosis = result.DatedItems.Count > 0
                ? "confirmed_sources_read_with_dated_items"
                : "confirmed_sources_read";
            result.RecommendedNextSteps =
            [
                "Use dated items first when summarizing current news.",
                "Use read page previews and saved page JSON only as supporting context.",
                "If the answer needs higher confidence, read additional sources."
            ];
            return;
        }

        if (result.Sources.Count > 0)
        {
            result.Status = "partial";
            result.Diagnosis = "search_results_found_but_pages_not_confirmed";
            result.RecommendedNextSteps =
            [
                "Do not present snippets as confirmed facts.",
                "Try reading sources again or use a more direct source URL.",
                "Explain that URLs were found but page text could not be confirmed."
            ];
            return;
        }

        result.Status = "empty";
        result.Diagnosis = result.Attempts.Any(attempt => attempt.ResultCount > 0)
            ? "search_results_found_but_filtered_as_irrelevant"
            : result.Attempts.Any(attempt => attempt.PossibleReason.Contains("blocked", StringComparison.OrdinalIgnoreCase))
                ? "provider_blocked_or_challenged"
                : "no_confirmed_results";
        result.RecommendedNextSteps =
        [
            "Do not claim that anything was found.",
            "Try a broader query.",
            "Try a different language.",
            "Try a direct official source.",
            "If raw search results exist but were filtered out, explain that the results did not match the task.",
            "If several providers are empty, explain that no confirmed result was found."
        ];
    }

    private static string NormalizeQuery(string value)
    {
        var query = value.Trim();
        if (query.StartsWith("web_research:", StringComparison.OrdinalIgnoreCase))
        {
            query = query["web_research:".Length..].Trim();
        }

        return WhitespaceRegex().Replace(query, " ").Trim();
    }

    private static string SimplifyQuery(string query)
    {
        var simplified = query
            .Replace("за последние 2 дня", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("за последние два дня", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("за последние 3 дня", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("за неделю", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("последние", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("latest", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("last 2 days", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("last two days", string.Empty, StringComparison.OrdinalIgnoreCase);

        return WhitespaceRegex().Replace(simplified, " ").Trim();
    }

    private static string BuildSimpleEnglishNewsQuery(string query)
    {
        if (LooksLikeSpaceNews(query))
        {
            return "latest space news";
        }

        return BuildSimpleEnglishQuery(query) + " news";
    }

    private static string BuildSimpleEnglishQuery(string query)
    {
        var lower = query.ToLowerInvariant();
        if (lower.Contains("космос", StringComparison.Ordinal) || lower.Contains("space", StringComparison.Ordinal))
        {
            return "space";
        }

        if (lower.Contains("лондон", StringComparison.Ordinal) || lower.Contains("london", StringComparison.Ordinal))
        {
            return "London";
        }

        return SimplifyQuery(query);
    }

    private static bool LooksLikeNews(string query)
    {
        var lower = query.ToLowerInvariant();
        return lower.Contains("новост", StringComparison.Ordinal)
            || lower.Contains("news", StringComparison.Ordinal)
            || lower.Contains("latest", StringComparison.Ordinal)
            || lower.Contains("последн", StringComparison.Ordinal)
            || lower.Contains("актуаль", StringComparison.Ordinal);
    }

    private static bool LooksLikeSpaceNews(string query)
    {
        var lower = query.ToLowerInvariant();
        return (lower.Contains("космос", StringComparison.Ordinal) || lower.Contains("space", StringComparison.Ordinal))
            && LooksLikeNews(query);
    }

    private static bool IsUsefulSourceUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host.Contains("duckduckgo.", StringComparison.Ordinal)
            || host.Contains("bing.com", StringComparison.Ordinal)
            || host.Contains("google.", StringComparison.Ordinal))
        {
            return false;
        }

        var path = uri.AbsolutePath.ToLowerInvariant();
        return !path.Contains("/search", StringComparison.Ordinal);
    }

    private static bool IsRelevantToTask(string task, string query, WebSearchResult item)
    {
        var normalizedTask = NormalizeQuery(task).Trim('"').ToLowerInvariant();
        var normalizedQuery = NormalizeQuery(query).Trim('"').ToLowerInvariant();
        var haystack = (item.Title + " " + item.Snippet + " " + item.Url).ToLowerInvariant();

        if (normalizedTask.Length >= 12
            && task.Trim().StartsWith('"')
            && task.Trim().EndsWith('"'))
        {
            return haystack.Contains(normalizedTask, StringComparison.OrdinalIgnoreCase);
        }

        if (normalizedTask.Contains("космос", StringComparison.Ordinal)
            || normalizedTask.Contains("space", StringComparison.Ordinal)
            || normalizedQuery.Contains("космос", StringComparison.Ordinal)
            || normalizedQuery.Contains("space", StringComparison.Ordinal))
        {
            string[] spaceMarkers =
            [
                "космос",
                "косми",
                "space",
                "nasa",
                "esa",
                "roscosmos",
                "spacex",
                "rocket",
                "astronomy",
                "universe",
                "moon",
                "mars"
            ];

            return spaceMarkers.Any(marker => haystack.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }

        var taskTerms = ExtractImportantTerms(normalizedTask);
        if (taskTerms.Count == 0)
        {
            return true;
        }

        return taskTerms.Any(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> ExtractImportantTerms(string value)
    {
        string[] stopWords =
        [
            "найди",
            "найти",
            "новости",
            "новость",
            "последние",
            "последний",
            "latest",
            "news",
            "the",
            "and",
            "for",
            "with",
            "about",
            "за",
            "дня",
            "дней",
            "неделю"
        ];

        return WordRegex()
            .Matches(value)
            .Select(match => match.Value.ToLowerInvariant())
            .Where(term => term.Length >= 4 && !stopWords.Contains(term, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static string Limit(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength] + Environment.NewLine + "...";
    }

    private static void AddDatedItems(WebResearchResponse result, string pageText, string sourceTitle, string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(pageText))
        {
            return;
        }

        foreach (Match match in DateRegex().Matches(pageText))
        {
            if (result.DatedItems.Count >= 12)
            {
                return;
            }

            var parsedDate = TryParseDate(match.Value);
            if (parsedDate is not null && !IsDateInsideRequestedWindow(result.Task, parsedDate.Value))
            {
                continue;
            }

            var start = Math.Max(0, match.Index - 180);
            var length = Math.Min(pageText.Length - start, 520);
            var fragment = pageText.Substring(start, length);
            fragment = WhitespaceRegex().Replace(fragment, " ").Trim();
            if (IsLikelyNavigationFragment(fragment))
            {
                continue;
            }

            if (result.DatedItems.Any(item =>
                    item.SourceUrl.Equals(sourceUrl, StringComparison.OrdinalIgnoreCase)
                    && item.Text.Equals(fragment, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.DatedItems.Add(new WebResearchDatedItem
            {
                DateText = match.Value,
                DateIso = parsedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                Text = fragment,
                SourceTitle = sourceTitle,
                SourceUrl = sourceUrl
            });
        }
    }

    private static bool IsLikelyNavigationFragment(string fragment)
    {
        var lower = fragment.ToLowerInvariant();
        string[] navigationMarkers =
        [
            "sign in",
            "sign out",
            "subscribe",
            "open menu",
            "privacy policy",
            "terms of use",
            "пользовательское соглашение",
            "политика конфиденциальности",
            "главное меню",
            "регистрация",
            "войти"
        ];

        return navigationMarkers.Count(marker => lower.Contains(marker, StringComparison.OrdinalIgnoreCase)) >= 3;
    }

    private static DateOnly? TryParseDate(string value)
    {
        var normalized = value.Trim().Trim('.', ',');
        if (DateOnly.TryParseExact(
                normalized,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var isoDate))
        {
            return isoDate;
        }

        if (DateOnly.TryParseExact(
                normalized,
                "MMMM d, yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var englishLong))
        {
            return englishLong;
        }

        if (DateOnly.TryParseExact(
                normalized,
                "MMM d, yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var englishShort))
        {
            return englishShort;
        }

        if (DateOnly.TryParseExact(
                normalized,
                "d MMM yy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var englishDayShortYear))
        {
            return englishDayShortYear.Year < 2000
                ? englishDayShortYear.AddYears(2000)
                : englishDayShortYear;
        }

        var russianMatch = RussianDateRegex().Match(normalized);
        if (russianMatch.Success
            && int.TryParse(russianMatch.Groups["day"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day)
            && int.TryParse(russianMatch.Groups["year"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
            && TryGetRussianMonth(russianMatch.Groups["month"].Value, out var month))
        {
            return new DateOnly(year, month, day);
        }

        return null;
    }

    private static bool IsDateInsideRequestedWindow(string task, DateOnly date)
    {
        var lower = task.ToLowerInvariant();
        var now = DateOnly.FromDateTime(DateTime.Now);
        var days = 0;
        if (lower.Contains("за 3 дня", StringComparison.Ordinal)
            || lower.Contains("последние 3 дня", StringComparison.Ordinal)
            || lower.Contains("last 3 days", StringComparison.Ordinal)
            || lower.Contains("past 3 days", StringComparison.Ordinal))
        {
            days = 3;
        }
        else if (lower.Contains("за 2 дня", StringComparison.Ordinal)
            || lower.Contains("последние 2 дня", StringComparison.Ordinal)
            || lower.Contains("last 2 days", StringComparison.Ordinal)
            || lower.Contains("past 2 days", StringComparison.Ordinal))
        {
            days = 2;
        }
        else if (lower.Contains("за неделю", StringComparison.Ordinal)
            || lower.Contains("last week", StringComparison.Ordinal)
            || lower.Contains("past week", StringComparison.Ordinal))
        {
            days = 7;
        }
        else if (LooksLikeNews(task))
        {
            days = 14;
        }

        if (days <= 0)
        {
            return true;
        }

        var earliest = now.AddDays(-days);
        return date >= earliest && date <= now.AddDays(1);
    }

    private static bool TryGetRussianMonth(string value, out int month)
    {
        month = value.ToLowerInvariant() switch
        {
            "января" => 1,
            "февраля" => 2,
            "марта" => 3,
            "апреля" => 4,
            "мая" => 5,
            "июня" => 6,
            "июля" => 7,
            "августа" => 8,
            "сентября" => 9,
            "октября" => 10,
            "ноября" => 11,
            "декабря" => 12,
            _ => 0
        };

        return month > 0;
    }

    private static void AddUnique(List<string> queries, string query)
    {
        var normalized = NormalizeQuery(query);
        if (!string.IsNullOrWhiteSpace(normalized)
            && !queries.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            queries.Add(normalized);
        }
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("[\\p{L}\\p{N}_-]+")]
    private static partial Regex WordRegex();

    [GeneratedRegex("\\b(?:[0-3]?\\d\\s+(?:января|февраля|марта|апреля|мая|июня|июля|августа|сентября|октября|ноября|декабря)\\s+20\\d{2}|20\\d{2}-[01]\\d-[0-3]\\d|(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)[a-z]*\\.?\\s+[0-3]?\\d,\\s+20\\d{2}|[0-3]?\\d\\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)[a-z]*\\.?\\s+\\d{2,4})\\b", RegexOptions.IgnoreCase)]
    private static partial Regex DateRegex();

    [GeneratedRegex("^(?<day>[0-3]?\\d)\\s+(?<month>января|февраля|марта|апреля|мая|июня|июля|августа|сентября|октября|ноября|декабря)\\s+(?<year>20\\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex RussianDateRegex();
}
