using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public static partial class SpeechTimelineBuilder
{
    [GeneratedRegex(@"[\p{L}\p{N}]+(?:[-'][\p{L}\p{N}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();

    public static IReadOnlyList<SpeechRevealCue> BuildEstimated(string text, int rate)
    {
        text ??= string.Empty;
        var matches = WordRegex().Matches(text);
        if (matches.Count == 0)
        {
            return [new SpeechRevealCue(0, text.Length)];
        }

        var millisecondsPerWord = 60_000d / Math.Clamp(rate, 80, 450);
        var cues = new List<SpeechRevealCue>(matches.Count);
        for (var index = 0; index < matches.Count; index++)
        {
            var current = matches[index];
            var nextStart = index + 1 < matches.Count ? matches[index + 1].Index : text.Length;
            var revealAtWord = index + 2;
            var time = (int)Math.Round(revealAtWord * millisecondsPerWord);
            cues.Add(new SpeechRevealCue(time, nextStart));
        }

        if (cues[^1].VisibleCharacters != text.Length)
        {
            cues.Add(new SpeechRevealCue(cues[^1].TimeMilliseconds + (int)millisecondsPerWord, text.Length));
        }

        return cues;
    }
}
