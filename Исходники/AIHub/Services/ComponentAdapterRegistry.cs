using AIHub.Models;

namespace AIHub.Services;

public sealed record ComponentAdapterDescriptor(
    string Id,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> ToolNames,
    string UsageSummary);

public static class ComponentAdapterRegistry
{
    private static readonly IReadOnlyList<ComponentAdapterDescriptor> Adapters =
    [
        new(
            "adapter.session_files.read",
            [
                "read.text",
                "read.json",
                "read.xml",
                "read.office_openxml",
                "read.spreadsheet",
                "read.pdf_text",
                "read.archive",
                "read.csv",
                "read.html",
                "read.svg",
                "read.markdown",
                "read.yaml",
                "read.email",
                "read.database.sqlite"
            ],
            [
                "session_files_list",
                "session_file_inspect",
                "session_file_read"
            ],
            "Reads bounded, non-destructive representations of files explicitly attached to the current session."),
        new(
            "adapter.image.pixels",
            [
                "read.image_pixels",
                "inspect.image.pixels"
            ],
            [
                "session_image_inspect_pixels"
            ],
            "Decodes an attached image locally and returns verified dimensions, pixel format and bounded color statistics. It does not claim semantic vision."),
        new(
            "adapter.image.semantic",
            [
                "analyze.image.semantic",
                "describe.image",
                "caption.image"
            ],
            [
                "session_image_describe"
            ],
            "Runs the verified local SmolVLM2 vision recipe against one attached image and returns a grounded description of visible content."),
        new(
            "adapter.imagemagick.inspect",
            [
                "read.image_extended",
                "inspect.image.extended"
            ],
            [
                "session_image_inspect_extended"
            ],
            "Uses the verified ImageMagick runtime to inspect extended image metadata without claiming semantic understanding."),
        new(
            "adapter.imagemagick.transform",
            [
                "edit.image",
                "image.edit",
                "convert.image"
            ],
            [
                "session_image_transform"
            ],
            "Uses a constrained ImageMagick adapter to convert, resize or strip metadata from an attached image and writes a new artifact without modifying the source."),
        new(
            "adapter.tesseract.ocr",
            [
                "extract.image_ocr",
                "ocr.image",
                "image.ocr"
            ],
            [
                "session_image_extract_text"
            ],
            "Uses the verified Tesseract runtime to extract printed text from an attached image. It does not identify general objects or scene meaning."),
        new(
            "adapter.whisper.transcribe",
            [
                "transcribe.audio",
                "speech.to_text",
                "extract.audio_transcript.multilingual"
            ],
            [
                "session_audio_transcribe"
            ],
            "Runs the verified local whisper.cpp runtime and model against an attached audio file and returns the generated transcript.")
    ];

    public static ComponentAdapterDescriptor? Find(string capability) =>
        Adapters.FirstOrDefault(adapter =>
            adapter.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase));

    public static ComponentAdapterDescriptor? FindByToolName(string toolName) =>
        Adapters.FirstOrDefault(adapter =>
            adapter.ToolNames.Contains(toolName, StringComparer.Ordinal));

    public static bool IsCallable(string capability) => Find(capability) is not null;

    public static IReadOnlyList<string> GetCallableCapabilities() => Adapters
        .SelectMany(adapter => adapter.Capabilities)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToList();

    public static IReadOnlyList<string> GetToolNames() => Adapters
        .SelectMany(adapter => adapter.ToolNames)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToList();
}
