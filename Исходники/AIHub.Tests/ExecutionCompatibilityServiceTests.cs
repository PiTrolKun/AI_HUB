using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ExecutionCompatibilityServiceTests
{
    [TestMethod]
    public void ResolveCapabilities_DistinguishesCallableAndUnboundRequirements()
    {
        var profile = CreateCapabilityProfile(
            "read.csv",
            "extract.audio_transcript",
            "edit.video",
            "generate.audio");
        var inventory = new CapabilityInventoryResponse
        {
            Items =
            [
                new CapabilityInventoryItem
                {
                    Role = "component_capability",
                    Name = "read.csv",
                    IsInstalled = true,
                    IsRunnable = true
                }
            ]
        };

        var result = ExecutionCompatibilityService.ResolveCapabilities(profile, inventory);

        CollectionAssert.Contains(result.Available, "read.csv");
        Assert.AreEqual(0, result.Missing.Count);
        CollectionAssert.Contains(result.Unresolved, "extract.audio_transcript");
        CollectionAssert.Contains(result.Unresolved, "edit.video");
        CollectionAssert.Contains(result.Unresolved, "generate.audio");
    }

    [TestMethod]
    public void ResolveCapabilities_SeparatesAudioInputFromAudioGeneration()
    {
        var profile = new ChoiceCapabilityProfile
        {
            Dimensions =
            [
                Dimension(
                    ChoiceDecisionDimensions.InputModality,
                    "file:audio"),
                Dimension(
                    ChoiceDecisionDimensions.TaskType,
                    "song_creation"),
                Dimension(
                    ChoiceDecisionDimensions.SpecializationNeed,
                    "audio_generation")
            ]
        };

        var result = ExecutionCompatibilityService.ResolveCapabilities(
            profile,
            new CapabilityInventoryResponse());

        CollectionAssert.AreEquivalent(
            new[] { "read.audio", "generate.audio" },
            result.Required);
        Assert.AreEqual(0, result.Missing.Count);
        CollectionAssert.AreEquivalent(
            new[] { "read.audio", "generate.audio" },
            result.Unresolved);
    }

    [TestMethod]
    [DataRow("audio_generation", "generate.audio", "read.audio")]
    [DataRow("image_generation", "generate.image", "read.image_pixels")]
    [DataRow("video_generation", "generate.video", "read.video")]
    public void CapabilityMapper_DoesNotInventInputReadingForOutputOnlyGeneration(
        string profileValue,
        string expected,
        string unexpected)
    {
        var profile = new ChoiceCapabilityProfile
        {
            Dimensions =
            [
                Dimension(
                    ChoiceDecisionDimensions.SpecializationNeed,
                    profileValue)
            ]
        };

        var result = ComponentCapabilityMapper.FromProfile(profile);

        CollectionAssert.Contains(result.ToList(), expected);
        CollectionAssert.DoesNotContain(result.ToList(), unexpected);
    }

    [TestMethod]
    public void InventoryNormalization_PreservesDistinctCapabilitiesWithoutPaths()
    {
        var items = CapabilityInventoryService.NormalizeItems(
        [
            Capability("read.csv"),
            Capability("edit.csv"),
            Capability("read.csv")
        ]);

        CollectionAssert.AreEquivalent(
            new[] { "read.csv", "edit.csv" },
            items.Where(item => item.Role == "component_capability")
                .Select(item => item.Name)
                .ToArray());
    }

    [TestMethod]
    [DataRow("text-generation", true)]
    [DataRow("conversational", true)]
    [DataRow("video-to-video", false)]
    [DataRow("text-to-image", false)]
    [DataRow("", false)]
    public void RuntimePassport_AcceptsOnlyRegisteredCoordinatorPipelines(
        string pipeline,
        bool expected)
    {
        Assert.AreEqual(
            expected,
            ExecutionCompatibilityService.IsLlamaCoordinatorPipeline(pipeline));
    }

    private static ChoiceCapabilityProfile CreateCapabilityProfile(params string[] capabilities) => new()
    {
        Dimensions =
        [
            new ChoiceCapabilityDimension
            {
                Dimension = ChoiceDecisionDimensions.ToolRequirements,
                Status = ChoiceDimensionStatuses.Resolved,
                Values = capabilities.ToList(),
                Evidence = "unit test"
            }
        ]
    };

    private static ChoiceCapabilityDimension Dimension(string name, string value) => new()
    {
        Dimension = name,
        Status = ChoiceDimensionStatuses.Resolved,
        Values = [value],
        Evidence = "unit test"
    };

    private static CapabilityInventoryItem Capability(string name) => new()
    {
        Role = "component_capability",
        Name = name,
        IsInstalled = true,
        IsRunnable = true
    };
}
