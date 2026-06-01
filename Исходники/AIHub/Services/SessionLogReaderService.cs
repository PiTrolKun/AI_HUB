using System.IO;
using System.Text;

namespace AIHub.Services;

public sealed class SessionLogReaderService
{
    private const int DefaultTailCount = 40;
    private const int MaxTailCount = 160;
    private const int DefaultSearchAround = 4;
    private const int MaxSearchResults = 8;

    public string Read(string sessionLogPath, string request)
    {
        if (string.IsNullOrWhiteSpace(sessionLogPath) || !File.Exists(sessionLogPath))
        {
            return "Session log is not available.";
        }

        var lines = File.ReadAllLines(sessionLogPath, Encoding.UTF8);
        var trimmed = request.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || trimmed.Equals("tail", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("tail ", StringComparison.OrdinalIgnoreCase))
        {
            return ReadTail(lines, ParseFirstInt(trimmed, DefaultTailCount));
        }

        if (trimmed.StartsWith("search ", StringComparison.OrdinalIgnoreCase))
        {
            return Search(lines, trimmed["search ".Length..].Trim());
        }

        return Search(lines, trimmed);
    }

    private static string ReadTail(IReadOnlyList<string> lines, int count)
    {
        count = Math.Clamp(count, 1, MaxTailCount);
        var start = Math.Max(0, lines.Count - count);
        var builder = new StringBuilder();
        builder.AppendLine($"Session log tail: last {lines.Count - start} of {lines.Count} records.");
        for (var index = start; index < lines.Count; index++)
        {
            builder.AppendLine($"{index + 1}: {lines[index]}");
        }

        return builder.ToString().Trim();
    }

    private static string Search(IReadOnlyList<string> lines, string query)
    {
        query = NormalizeSearchQuery(query);
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Session log search: empty query.";
        }

        var matches = new List<int>();
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(index);
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Session log search: {query}");
        builder.AppendLine($"Matches: {matches.Count}");
        if (matches.Count == 0)
        {
            builder.AppendLine("No matching records in current session log.");
            builder.AppendLine("Try a shorter word, another spelling, or `session_log: tail 80`.");
            return builder.ToString().Trim();
        }

        foreach (var match in matches.Take(MaxSearchResults))
        {
            var from = Math.Max(0, match - DefaultSearchAround);
            var to = Math.Min(lines.Count - 1, match + DefaultSearchAround);
            builder.AppendLine($"--- match at record {match + 1}, context {from + 1}-{to + 1} ---");
            for (var index = from; index <= to; index++)
            {
                builder.AppendLine($"{index + 1}: {lines[index]}");
            }
        }

        if (matches.Count > MaxSearchResults)
        {
            builder.AppendLine($"More matches skipped: {matches.Count - MaxSearchResults}.");
        }

        return builder.ToString().Trim();
    }

    private static int ParseFirstInt(string text, int fallback)
    {
        foreach (var part in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
        }

        return fallback;
    }

    private static string NormalizeSearchQuery(string query)
    {
        query = query.Trim();
        if (query.StartsWith("query=", StringComparison.OrdinalIgnoreCase))
        {
            query = query["query=".Length..].Trim();
        }

        return query.Trim('"', '\'', '`');
    }
}
