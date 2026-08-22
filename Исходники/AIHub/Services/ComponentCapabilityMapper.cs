using AIHub.Models;

namespace AIHub.Services;

public static class ComponentCapabilityMapper
{
    private static readonly HashSet<string> OcrDenialValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "no_ocr",
        "without_ocr",
        "ocr_not_required",
        "ocr_not_needed",
        "exclude_ocr",
        "disable_ocr"
    };

    private static readonly string[] GenerationMarkers =
    [
        "generat", "synthes", "creation", "create", "compos"
    ];

    private static readonly string[] EditingMarkers =
    [
        "edit", "modify", "enhanc", "upscal", "restor"
    ];

    private static readonly string[] AnalysisMarkers =
    [
        "analysis", "analyz", "understand", "recognition", "inspect"
    ];

    public static IReadOnlyList<string> FromProfile(ChoiceCapabilityProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var dimensions = profile.Dimensions
            .Where(dimension => dimension.Status is ChoiceDimensionStatuses.Resolved
                or ChoiceDimensionStatuses.Provisional)
            .ToList();
        var values = dimensions
            .SelectMany(dimension => dimension.Values)
            .Select(Normalize)
            .Where(value => value.Length > 0)
            .ToList();
        var inputValues = dimensions
            .Where(dimension => string.Equals(
                dimension.Dimension,
                ChoiceDecisionDimensions.InputModality,
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(dimension => dimension.Values)
            .Select(Normalize)
            .Where(value => value.Length > 0)
            .ToList();
        var capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var approvedCapabilities = ComponentCatalog.Processing
            .SelectMany(entry => entry.Capabilities)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (approvedCapabilities.Contains(value) || IsCapabilityIdentifier(value))
            {
                capabilities.Add(value);
            }
        }

        AddIf(inputValues, capabilities, "read.text", "text", "document", "txt");
        AddIf(inputValues, capabilities, "read.office_openxml", "docx", "xlsx", "pptx", "office_openxml");
        AddIf(inputValues, capabilities, "read.spreadsheet", "spreadsheet", "excel", "table");
        AddIf(inputValues, capabilities, "read.pdf_text", "pdf");
        AddIf(inputValues, capabilities, "read.archive", "archive", "zip", "rar", "7z");
        AddIf(inputValues, capabilities, "read.email", "email", "eml", "mime");
        AddIf(inputValues, capabilities, "read.database.sqlite", "sqlite", "database");
        AddIf(inputValues, capabilities, "read.image_pixels", "image", "photo", "vision", "multimodal");
        AddIf(inputValues, capabilities, "read.audio", "audio", "music", "song", "speech", "voice");
        AddIf(inputValues, capabilities, "read.video", "video");

        AddMediaOperationCapabilities(values, capabilities);
        if (!IsExplicitlyDenied(profile, "extract.image_ocr"))
        {
            AddIf(values, capabilities, "extract.image_ocr", "ocr", "scan", "scanned_document");
        }
        AddIf(values, capabilities, "extract.audio_transcript", "speech", "transcription", "voice");
        AddIf(values, capabilities, "extract.video_frames", "video_analysis", "frames");
        AddIf(values, capabilities, "convert.legacy_office", "doc", "xls", "ppt", "odt", "legacy_office");

        return capabilities.Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static bool IsExplicitlyDenied(
        ChoiceCapabilityProfile profile,
        string capability)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var canonical = ComponentCapabilityAliasCatalog.Canonicalize(capability);
        if (!string.Equals(canonical, "extract.image_ocr", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return profile.Dimensions
            .Where(dimension => dimension.Status is ChoiceDimensionStatuses.Resolved
                or ChoiceDimensionStatuses.Provisional)
            .SelectMany(dimension => dimension.Values)
            .Select(Normalize)
            .Any(OcrDenialValues.Contains);
    }

    public static ComponentAcquisitionPlan BuildPlan(
        ChoiceCapabilityProfile profile,
        string reason,
        bool required = true) => new ComponentManager().BuildPlan(
            FromProfile(profile),
            reason,
            required);

    private static void AddIf(
        IReadOnlyCollection<string> values,
        ISet<string> target,
        string capability,
        params string[] markers)
    {
        if (values.Any(value => markers.Any(marker =>
                value.Contains(marker, StringComparison.OrdinalIgnoreCase))))
        {
            target.Add(capability);
        }
    }

    private static void AddMediaOperationCapabilities(
        IEnumerable<string> values,
        ISet<string> target)
    {
        foreach (var value in values)
        {
            foreach (var modality in DetectMediaModalities(value))
            {
                if (ContainsAny(value, GenerationMarkers))
                {
                    target.Add($"generate.{modality}");
                }
                if (ContainsAny(value, EditingMarkers))
                {
                    target.Add($"edit.{modality}");
                }
                if (ContainsAny(value, AnalysisMarkers))
                {
                    target.Add(modality switch
                    {
                        "image" => "read.image_pixels",
                        "audio" => "read.audio",
                        _ => "read.video"
                    });
                }
            }
        }
    }

    private static IEnumerable<string> DetectMediaModalities(string value)
    {
        if (ContainsAny(value, ["image", "photo", "vision"]))
        {
            yield return "image";
        }
        if (ContainsAny(value, ["audio", "music", "song", "speech", "voice"]))
        {
            yield return "audio";
        }
        if (value.Contains("video", StringComparison.OrdinalIgnoreCase))
        {
            yield return "video";
        }
    }

    private static bool ContainsAny(string value, IEnumerable<string> markers) =>
        markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static bool IsCapabilityIdentifier(string value)
    {
        var separator = value.IndexOf('.');
        if (separator <= 0 || separator >= value.Length - 1 || value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterOrDigit(character)
            || character is '.' or '_' or '-');
    }
}
