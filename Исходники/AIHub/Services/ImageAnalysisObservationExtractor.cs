using System.Text.RegularExpressions;

namespace AIHub.Services;

public static partial class ImageAnalysisObservationExtractor
{
    public static IReadOnlyList<string> Extract(string? visualReport, int maximum = 18)
    {
        if (string.IsNullOrWhiteSpace(visualReport) || maximum <= 0)
        {
            return [];
        }

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = visualReport.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        foreach (var rawLine in lines)
        {
            var line = CleanLine(rawLine);
            if (line.Length < 8 || IsServiceLine(line))
            {
                continue;
            }

            foreach (var candidate in SplitLongLine(line))
            {
                var value = candidate.Trim();
                if (value.Length < 8 || !seen.Add(value))
                {
                    continue;
                }
                results.Add(value.Length <= 260 ? value : value[..257].TrimEnd() + "…");
                if (results.Count >= maximum)
                {
                    return results;
                }
            }
        }
        return results;
    }

    private static string CleanLine(string line)
    {
        var value = line.Trim();
        value = MarkdownPrefixRegex().Replace(value, string.Empty).Trim();
        value = value.Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal)
            .Trim();
        return value;
    }

    private static bool IsServiceLine(string line) =>
        line.StartsWith("<think", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("</think", StringComparison.OrdinalIgnoreCase)
        || line.Equals("analysis", StringComparison.OrdinalIgnoreCase)
        || line.Equals("final", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("```", StringComparison.Ordinal);

    private static IEnumerable<string> SplitLongLine(string line)
    {
        if (line.Length <= 260)
        {
            yield return line;
            yield break;
        }
        foreach (var sentence in SentenceBoundaryRegex().Split(line))
        {
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                yield return sentence;
            }
        }
    }

    [GeneratedRegex(@"^(?:#{1,6}\s*|[-*•]\s+|\d+[.)]\s+)")]
    private static partial Regex MarkdownPrefixRegex();

    [GeneratedRegex(@"(?<=[.!?…])\s+")]
    private static partial Regex SentenceBoundaryRegex();
}
