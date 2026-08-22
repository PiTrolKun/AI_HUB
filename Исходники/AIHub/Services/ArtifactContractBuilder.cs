using AIHub.Models;

namespace AIHub.Services;

public sealed class ArtifactContractBuilder
{
    public ArtifactContract Build(
        IReadOnlyList<SandboxWorkPattern> patterns,
        SessionFilePromptManifest? fileManifest)
    {
        var primary = patterns.FirstOrDefault() ?? new SandboxWorkPattern
        {
            Id = "other.custom",
            ArtifactTypes = [ArtifactKinds.Text]
        };
        var artifactKind = SelectArtifactKind(patterns);
        var (extension, mimeType) = GetDefaultFormat(artifactKind, primary.Id);
        return new ArtifactContract
        {
            InputFileNames = fileManifest?.Files
                .Where(file => file.IsAvailable)
                .Select(file => file.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [],
            InputFormats = fileManifest?.Files
                .Where(file => file.IsAvailable)
                .Select(file => NormalizeExtension(file.Extension))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [],
            ArtifactKind = artifactKind,
            PreferredExtension = extension,
            MimeType = mimeType,
            RequiredProperties = patterns
                .SelectMany(pattern => pattern.ValidationRules)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList(),
            EmergencyAcceptableResult = BuildEmergencyResult(artifactKind),
            ValidationRules = patterns
                .SelectMany(pattern => pattern.ValidationRules)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList(),
            UserClarificationAllowed = true,
            QualityTarget = "best_effort"
        };
    }

    private static string SelectArtifactKind(
        IReadOnlyList<SandboxWorkPattern> patterns)
    {
        var ordered = patterns
            .SelectMany(pattern => pattern.ArtifactTypes)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
        return ordered.FirstOrDefault(value => value is
                ArtifactKinds.Image
                or ArtifactKinds.Audio
                or ArtifactKinds.Video
                or ArtifactKinds.Document
                or ArtifactKinds.Table
                or ArtifactKinds.Presentation
                or ArtifactKinds.Code
                or ArtifactKinds.Archive)
            ?? ordered.FirstOrDefault()
            ?? ArtifactKinds.Text;
    }

    private static (string Extension, string MimeType) GetDefaultFormat(
        string artifactKind,
        string patternId) => artifactKind switch
        {
            ArtifactKinds.Document => (
                ".docx",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            ArtifactKinds.Table => (
                ".xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            ArtifactKinds.Image => (".png", "image/png"),
            ArtifactKinds.Audio => (".wav", "audio/wav"),
            ArtifactKinds.Video => (".mp4", "video/mp4"),
            ArtifactKinds.Presentation => (
                ".pptx",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
            ArtifactKinds.Code when patternId == "code.modify" => (
                ".patch",
                "text/x-diff"),
            ArtifactKinds.Code => (".md", "text/markdown"),
            ArtifactKinds.Archive => (".zip", "application/zip"),
            ArtifactKinds.File => (".bin", "application/octet-stream"),
            _ => (".txt", "text/plain")
        };

    private static string BuildEmergencyResult(string artifactKind) =>
        artifactKind switch
        {
            ArtifactKinds.Image =>
                "A valid image file in the requested container, even if only a conservative conversion or unchanged safe copy is possible.",
            ArtifactKinds.Audio =>
                "A playable audio file, even if only a conservative conversion or unchanged safe copy is possible.",
            ArtifactKinds.Video =>
                "A playable video file, even if only a conservative remux, conversion or unchanged safe copy is possible.",
            ArtifactKinds.Document =>
                "A readable document containing the best available result and explicit limitations.",
            ArtifactKinds.Table =>
                "A readable workbook or delimited table containing the best available structured data.",
            ArtifactKinds.Presentation =>
                "A readable presentation with a minimal title and result structure.",
            ArtifactKinds.Code =>
                "A code file, patch or project note that contains a concrete best-effort implementation.",
            ArtifactKinds.Archive =>
                "A valid archive containing the available result files.",
            _ =>
                "A non-empty text artifact containing the best available result and explicit limitations."
        };

    private static string NormalizeExtension(string value)
    {
        var extension = value?.Trim() ?? string.Empty;
        if (extension.Length == 0)
        {
            return string.Empty;
        }

        return extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
    }
}
