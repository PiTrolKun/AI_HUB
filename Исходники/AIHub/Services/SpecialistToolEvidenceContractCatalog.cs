using AIHub.Models;

namespace AIHub.Services;

internal static class SpecialistToolEvidenceContractCatalog
{
    private static readonly IReadOnlyDictionary<string, SpecialistToolEvidenceContract> Contracts =
        new Dictionary<string, SpecialistToolEvidenceContract>(StringComparer.OrdinalIgnoreCase)
        {
            ["session_files_list"] = ToolResult(),
            ["session_file_inspect"] = ToolResult(),
            ["session_file_read"] = ToolResult(),
            ["session_image_inspect_pixels"] = ToolResult("read.image_pixels"),
            ["session_image_inspect_extended"] = ToolResult("read.image_extended"),
            ["session_image_describe"] = ToolResult(
                "read.image_pixels",
                "analyze.image.semantic"),
            ["session_image_extract_text"] = ToolResult(
                "read.image_pixels",
                "extract.image_ocr"),
            ["session_image_transform"] = new(
                ExecutionEvidenceTypes.ProducedArtifact,
                ["edit.image"]),
            ["session_audio_transcribe"] = ToolResult(
                "read.audio",
                "extract.audio_transcript")
        };

    public static SpecialistToolEvidenceContract? Find(string toolName) =>
        Contracts.TryGetValue(toolName?.Trim() ?? string.Empty, out var contract)
            ? contract
            : null;

    private static SpecialistToolEvidenceContract ToolResult(params string[] capabilities) =>
        new(ExecutionEvidenceTypes.ToolResult, capabilities);
}

internal sealed record SpecialistToolEvidenceContract(
    string EvidenceType,
    IReadOnlyList<string> ConfirmedCapabilities);
