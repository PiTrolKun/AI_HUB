using AIHub.Models;

namespace AIHub.Services;

public sealed class SandboxExecutionRecipeService
{
    public List<SandboxExecutionRecipe> Build(
        IReadOnlyList<SandboxWorkPattern> patterns,
        ArtifactContract contract)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(contract);

        var selected = patterns.Count > 0
            ? patterns
            :
            [
                new SandboxWorkPattern
                {
                    Id = "other.custom",
                    NameEn = "Custom work",
                    ArtifactTypes = [contract.ArtifactKind]
                }
            ];
        var recipe = new SandboxExecutionRecipe
        {
            Id = $"sandbox.{contract.ArtifactKind}.best_effort.v1",
            PatternIds = selected
                .Select(pattern => pattern.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ArtifactKind = contract.ArtifactKind,
            Purpose = $"Create and validate a {contract.ArtifactKind} artifact using the best available route.",
            PreferredSteps = selected
                .SelectMany(pattern => pattern.PreferredRecipe)
                .Where(step => !string.IsNullOrWhiteSpace(step))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            DegradedSteps = selected
                .SelectMany(pattern => pattern.DegradedRecipe)
                .Where(step => !string.IsNullOrWhiteSpace(step))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            RequiredCapabilities = selected
                .SelectMany(pattern => pattern.RequiredCapabilities)
                .Where(capability => !string.IsNullOrWhiteSpace(capability))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ValidationRules = contract.ValidationRules
                .Where(rule => !string.IsNullOrWhiteSpace(rule))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
        recipe.EmergencySteps = BuildEmergencySteps(contract);
        if (recipe.PreferredSteps.Count == 0)
        {
            recipe.PreferredSteps =
            [
                "Use every ready specialist capability and trusted adapter.",
                "Create the artifact in the requested format.",
                "Validate the resulting file before reporting success."
            ];
        }

        if (recipe.DegradedSteps.Count == 0)
        {
            recipe.DegradedSteps =
            [
                "Use only installed and verified components.",
                "Preserve source data and produce a conservative best-effort artifact.",
                "Record limitations and validate the resulting file."
            ];
        }

        return [recipe];
    }

    private static List<string> BuildEmergencySteps(ArtifactContract contract) =>
        contract.ArtifactKind switch
        {
            ArtifactKinds.Image =>
            [
                "Copy a readable source image without modifying the original when no safe image processor is available.",
                "Otherwise create a valid PNG placeholder that records the limitation.",
                "Verify the image signature and non-zero dimensions."
            ],
            ArtifactKinds.Audio =>
            [
                "Copy a playable source audio file without modifying the original when no safe audio processor is available.",
                "Otherwise create a valid WAV container with a short silent track.",
                "Verify the audio signature and non-empty payload."
            ],
            ArtifactKinds.Video =>
            [
                "Copy a playable source video file without modifying the original when no safe video processor is available.",
                "Do not claim enhancement when only a safe copy was possible.",
                "Verify the video container signature and non-empty payload."
            ],
            ArtifactKinds.Document =>
            [
                "Render the best available textual result into a DOCX document.",
                "Include explicit limitations when specialist processing was unavailable.",
                "Verify that the Open XML package can be opened."
            ],
            ArtifactKinds.Table =>
            [
                "Render available structured values into a workbook.",
                "Include a limitations sheet when extraction was incomplete.",
                "Verify that the workbook can be opened."
            ],
            ArtifactKinds.Presentation =>
            [
                "Create a minimal readable presentation or a documented fallback package.",
                "Keep the requested subject and limitations visible.",
                "Verify the package before reporting success."
            ],
            ArtifactKinds.Code =>
            [
                "Write a concrete code, patch or project-note artifact.",
                "Keep unresolved assumptions explicit.",
                "Verify that the file is non-empty and readable."
            ],
            ArtifactKinds.Archive =>
            [
                "Package the available result files into a ZIP archive.",
                "Do not alter source files.",
                "Verify that the archive can be enumerated."
            ],
            _ =>
            [
                "Write the best available substantive result to a readable text artifact.",
                "State limitations instead of replacing the result with a plan.",
                "Verify that the file is non-empty and readable."
            ]
        };
}
