using System.Text.Json;

namespace AIHub.Services;

internal static class OmniJsonSyntaxRecovery
{
    // Recover only the observed duplicate array closer. Never touch quoted text,
    // missing content, unmatched object braces, or a second ambiguous error.
    public static bool TryRemoveDuplicateArrayCloser(string json, out string recovered)
    {
        recovered = json;
        var stack = new Stack<char>();
        var inString = false;
        var escaped = false;
        var previous = '\0';
        var duplicate = -1;
        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }
            if (char.IsWhiteSpace(c)) continue;
            if (c == '"') inString = true;
            else if (c is '[' or '{') stack.Push(c);
            else if (c is ']' or '}')
            {
                var expected = c == ']' ? '[' : '{';
                if (stack.TryPeek(out var open) && open == expected) stack.Pop();
                else if (c == ']' && previous == ']' && open == '{' && duplicate < 0)
                    duplicate = i;
                else return false;
            }
            previous = c;
        }
        if (inString || stack.Count != 0 || duplicate < 0) return false;
        var candidate = json.Remove(duplicate, 1);
        try
        {
            using var parsed = JsonDocument.Parse(candidate);
            if (parsed.RootElement.ValueKind != JsonValueKind.Object) return false;
        }
        catch (JsonException) { return false; }
        recovered = candidate;
        return true;
    }
}
