using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public static class CoreSpeechTextService
{
    public static CoreSpeechComposition Compose(IReadOnlyList<CoreSpeechSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var builder = new StringBuilder();
        var spans = new List<CoreSpeechSegmentSpan>();
        foreach (var segment in segments.Where(item => !string.IsNullOrWhiteSpace(item.Text)))
        {
            if (builder.Length > 0)
            {
                var previous = builder[^1];
                if (previous is not '.' and not '!' and not '?' and not ':' and not ';')
                {
                    builder.Append('.');
                }

                builder.Append(' ');
            }

            var text = segment.Text.Trim();
            var start = builder.Length;
            builder.Append(text);
            spans.Add(new CoreSpeechSegmentSpan(segment.Id, text, start, text.Length));
        }

        return new CoreSpeechComposition(builder.ToString(), spans);
    }

    public static IReadOnlyDictionary<string, int> MapVisibleCharacters(
        CoreSpeechComposition composition,
        int combinedVisibleCharacters)
    {
        var visible = Math.Clamp(combinedVisibleCharacters, 0, composition.Text.Length);
        return composition.Segments.ToDictionary(
            segment => segment.Id,
            segment => Math.Clamp(visible - segment.Start, 0, segment.Length),
            StringComparer.Ordinal);
    }

    public static int NativeCharacterPositionToUtf16Index(string text, int oneBasedPosition)
    {
        if (oneBasedPosition <= 1 || string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var scalarTarget = oneBasedPosition - 1;
        var scalarIndex = 0;
        var utf16Index = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (scalarIndex >= scalarTarget)
            {
                break;
            }

            utf16Index += rune.Utf16SequenceLength;
            scalarIndex++;
        }

        return Math.Clamp(utf16Index, 0, text.Length);
    }

    public static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var count = 0;
        var inWord = false;
        foreach (var rune in text.EnumerateRunes())
        {
            var isWord = Rune.IsLetterOrDigit(rune);
            if (isWord && !inWord)
            {
                count++;
            }

            inWord = isWord;
        }

        return count;
    }
}
