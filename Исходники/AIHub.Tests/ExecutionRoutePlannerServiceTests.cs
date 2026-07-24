using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ExecutionRoutePlannerServiceTests
{
    [TestMethod]
    public void Build_WebpInterpretationSeparatesDecoderFromSemanticUnderstanding()
    {
        var profile = CreateProfile(
            (ChoiceDecisionDimensions.InputModality, "file:image"),
            (ChoiceDecisionDimensions.TaskType, "information_interpretation"),
            (ChoiceDecisionDimensions.ToolRequirements, "use_multimodal_tools"));
        var manifest = new SessionFilePromptManifest
        {
            Intent = SessionFileIntentStatuses.Selected,
            FileCount = 1,
            Files =
            [
                new SessionFilePromptItem
                {
                    Name = "images.webp",
                    Extension = ".webp",
                    Category = SessionFileCategories.Image,
                    IsAvailable = true
                }
            ]
        };

        var route = new ExecutionRoutePlannerService().Build(
            profile,
            manifest,
            "unit test");

        CollectionAssert.Contains(route.SourceFormats, ".webp");
        AssertRequirement(route, ExecutionRouteLayers.Decode, "read.image_extended");
        AssertRequirement(route, ExecutionRouteLayers.SemanticAnalysis, "analyze.image.semantic");
        Assert.IsTrue(route.HasBlockedRequirements);
        Assert.IsFalse(route.IsExecutable);
        Assert.IsTrue(route.Resolution.Bindings.Any(binding =>
            binding.RequestedCapabilityId == "read.image_extended"
            && binding.Status == CapabilityBindingStatuses.AdapterMissing));
    }

    [TestMethod]
    public void Build_SupportedImageStillRequiresSeparateSemanticModule()
    {
        var route = new ExecutionRoutePlannerService().Build(
            CreateProfile(
                (ChoiceDecisionDimensions.InputModality, "file:image"),
                (ChoiceDecisionDimensions.TaskType, "image_description")),
            ImageManifest(".png"),
            "unit test");

        AssertRequirement(route, ExecutionRouteLayers.Decode, "read.image_pixels");
        AssertRequirement(route, ExecutionRouteLayers.SemanticAnalysis, "analyze.image.semantic");
    }

    [TestMethod]
    public void Build_TextFileDoesNotInventMediaRequirements()
    {
        var route = new ExecutionRoutePlannerService().Build(
            CreateProfile(
                (ChoiceDecisionDimensions.InputModality, "file:text"),
                (ChoiceDecisionDimensions.TaskType, "content_summarization")),
            new SessionFilePromptManifest
            {
                Intent = SessionFileIntentStatuses.Selected,
                FileCount = 1,
                Files =
                [
                    new SessionFilePromptItem
                    {
                        Name = "notes.txt",
                        Extension = ".txt",
                        Category = SessionFileCategories.Text,
                        IsAvailable = true
                    }
                ]
            },
            "unit test");

        AssertRequirement(route, ExecutionRouteLayers.FileAccess, "read.text");
        Assert.IsFalse(route.Requirements.Any(requirement =>
            requirement.Request.Id.Contains("image", StringComparison.OrdinalIgnoreCase)
            || requirement.Request.Id.Contains("audio", StringComparison.OrdinalIgnoreCase)
            || requirement.Request.Id.Contains("video", StringComparison.OrdinalIgnoreCase)));
    }

    private static SessionFilePromptManifest ImageManifest(string extension) => new()
    {
        Intent = SessionFileIntentStatuses.Selected,
        FileCount = 1,
        Files =
        [
            new SessionFilePromptItem
            {
                Name = $"image{extension}",
                Extension = extension,
                Category = SessionFileCategories.Image,
                IsAvailable = true
            }
        ]
    };

    private static ChoiceCapabilityProfile CreateProfile(
        params (string Dimension, string Value)[] values) => new()
        {
            Dimensions = values.Select(value => new ChoiceCapabilityDimension
            {
                Dimension = value.Dimension,
                Status = ChoiceDimensionStatuses.Resolved,
                Values = [value.Value],
                Evidence = "unit test"
            }).ToList()
        };

    private static void AssertRequirement(
        ExecutionRoutePlan route,
        string layer,
        string capability)
    {
        Assert.IsTrue(route.Requirements.Any(requirement =>
            requirement.Layer == layer
            && requirement.Request.Id == capability));
    }
}
