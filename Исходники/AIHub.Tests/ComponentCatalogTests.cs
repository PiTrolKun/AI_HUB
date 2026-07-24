using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ComponentCatalogTests
{
    [TestMethod]
    public void ViewerCatalog_IsNeverVisibleAsAiCapability()
    {
        Assert.IsTrue(ComponentCatalog.Viewers.Count > 0);
        Assert.IsTrue(ComponentCatalog.Viewers.All(entry => !entry.IsVisibleToAi));
        Assert.IsTrue(ComponentCatalog.Viewers.All(entry => entry.Capabilities.Count == 0));
        Assert.IsFalse(new ComponentManager().GetAvailableCapabilities()
            .Any(capability => capability.Contains("viewer", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void DependencyResolver_PutsJavaBeforeTika()
    {
        var resolved = ComponentCatalog.ResolveDependencies(["runtime.apache-tika"]);

        CollectionAssert.AreEqual(
            new[] { "runtime.java.temurin21", "runtime.apache-tika" },
            resolved.Select(entry => entry.Id).ToArray());
    }

    [TestMethod]
    public void CapabilityMapper_BuildsVideoRuntimePlanWithoutDuplicateProvider()
    {
        var profile = new ChoiceCapabilityProfile
        {
            Dimensions =
            [
                new ChoiceCapabilityDimension
                {
                    Dimension = "input_modality",
                    Status = ChoiceDimensionStatuses.Resolved,
                    Values = ["video_analysis"],
                    Evidence = "unit test"
                }
            ]
        };

        var plan = ComponentCapabilityMapper.BuildPlan(profile, "unit test");

        Assert.AreEqual(
            1,
            plan.Items.Count(item => item.ComponentId == "runtime.ffmpeg"));
        Assert.IsTrue(plan.TotalDownloadBytes >= 0);
    }

    [TestMethod]
    public void FileViewer_PerExtensionPreferenceOverridesGlobalPreference()
    {
        var settings = new FileViewerSettings
        {
            PreferInternalViewers = true,
            PreferInternalByExtension =
            {
                [".pdf"] = false
            }
        };

        Assert.IsFalse(FileViewerService.CanOpenInternally(".pdf", settings));
        Assert.IsTrue(FileViewerService.CanOpenInternally(".json", settings));
        Assert.IsFalse(FileViewerService.CanOpenInternally(".unknown", settings));
    }

    [TestMethod]
    public void ExecutorParser_AcceptsTrustedCapabilityAndRejectsUnknownCapability()
    {
        var trusted = CapabilityResponse("read.video");
        var unknown = CapabilityResponse("viewer.libvlc");

        Assert.IsTrue(ExecutorResultParser.TryReadTurn(trusted, out var turn));
        Assert.AreEqual(ExecutorTurnActions.RequestCapability, turn.Action);
        Assert.AreEqual("read.video", turn.RequestedCapability);
        Assert.IsFalse(ExecutorResultParser.TryReadTurn(unknown, out _));
    }

    private static string CapabilityResponse(string capability) => $$"""
        {
          "status":"working",
          "action":"request_capability",
          "stageId":"practical_clarification",
          "stageSummary":"Нужно прочитать входной файл.",
          "thought":"Проверяю доступные возможности.",
          "question":"",
          "options":[],
          "allowCustom":false,
          "currentResultSummary":"Подготовка продолжается.",
          "workingResultFragment":"",
          "canFinalize":false,
          "completionReason":"",
          "requestedTools":[],
          "requestedCapability":"{{capability}}",
          "capabilityReason":"Нужно обработать входные данные.",
          "capabilityRequired":true,
          "missingCriticalInputs":[],
          "assumptions":[],
          "result":"",
          "sources":[],
          "warnings":[]
        }
        """;
}
