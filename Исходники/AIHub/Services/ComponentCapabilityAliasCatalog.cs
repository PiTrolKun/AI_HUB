namespace AIHub.Services;

public static class ComponentCapabilityAliasCatalog
{
    private static readonly IReadOnlyDictionary<string, string> CanonicalAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["transcribe.audio"] = "extract.audio_transcript.multilingual",
            ["extract.audio_transcript"] = "extract.audio_transcript.multilingual",
            ["speech.to_text"] = "extract.audio_transcript.multilingual",
            ["inspect.image.pixels"] = "read.image_pixels",
            ["read.image"] = "read.image_pixels",
            ["ocr.image"] = "extract.image_ocr",
            ["image.ocr"] = "extract.image_ocr",
            ["inspect.image.extended"] = "read.image_extended",
            ["image.edit"] = "edit.image",
            ["describe.image"] = "analyze.image.semantic",
            ["caption.image"] = "analyze.image.semantic",
            ["image.caption"] = "analyze.image.semantic",
            ["vision.image"] = "analyze.image.semantic"
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Aliases =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["transcribe.audio"] =
            [
                "extract.audio_transcript.multilingual",
                "extract.audio_transcript"
            ],
            ["extract.audio_transcript"] =
            [
                "extract.audio_transcript.multilingual"
            ],
            ["speech.to_text"] =
            [
                "extract.audio_transcript.multilingual",
                "extract.audio_transcript"
            ],
            ["inspect.image.pixels"] =
            [
                "read.image_pixels"
            ],
            ["ocr.image"] =
            [
                "extract.image_ocr"
            ],
            ["image.ocr"] =
            [
                "extract.image_ocr"
            ],
            ["inspect.image.extended"] =
            [
                "read.image_extended"
            ],
            ["image.edit"] =
            [
                "edit.image"
            ],
            ["describe.image"] =
            [
                "analyze.image.semantic",
                "caption.image"
            ],
            ["caption.image"] =
            [
                "analyze.image.semantic",
                "describe.image"
            ],
            ["image.caption"] =
            [
                "analyze.image.semantic",
                "describe.image"
            ],
            ["vision.image"] =
            [
                "analyze.image.semantic",
                "describe.image"
            ]
        };

    public static IReadOnlyList<string> Expand(IEnumerable<string> capabilities) =>
        capabilities
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(ExpandOne)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<string> ExpandOne(string capability)
    {
        var normalized = capability.Trim().ToLowerInvariant();
        var canonical = Canonicalize(normalized);
        if (!Aliases.TryGetValue(normalized, out var aliases))
        {
            return [canonical];
        }

        return new[] { canonical, normalized }
            .Concat(aliases)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string Canonicalize(string capability)
    {
        var normalized = capability?.Trim().ToLowerInvariant() ?? string.Empty;
        return CanonicalAliases.TryGetValue(normalized, out var canonical)
            ? canonical
            : normalized;
    }
}
