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
    public void EveryFixedComponent_HasBilingualSemanticPassport()
    {
        foreach (var entry in ComponentCatalog.All)
        {
            Assert.IsTrue(
                ComponentSemanticPassportCatalog.HasPassport(entry.Id),
                $"Missing semantic passport for {entry.Id}.");
            var passport = ComponentSemanticPassportCatalog.Get(entry);
            Assert.IsFalse(string.IsNullOrWhiteSpace(passport.Ru), entry.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(passport.En), entry.Id);
        }
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
    public void ExecutorParser_AcceptsTrustedAndUnknownCapabilities()
    {
        var trusted = CapabilityResponse("read.video");
        var unknown = CapabilityResponse("analyze.specialized_sensor");

        Assert.IsTrue(ExecutorResultParser.TryReadTurn(trusted, out var turn));
        Assert.AreEqual(ExecutorTurnActions.RequestCapability, turn.Action);
        Assert.AreEqual("read.video", turn.RequestedCapability);
        Assert.AreEqual(1, turn.RequestedCapabilities.Count);
        Assert.AreEqual("read.video", turn.RequestedCapabilities[0].Id);

        Assert.IsTrue(ExecutorResultParser.TryReadTurn(unknown, out var unknownTurn));
        Assert.AreEqual(
            "analyze.specialized_sensor",
            unknownTurn.RequestedCapabilities.Single().Id);
    }

    [TestMethod]
    public void ExecutorParser_NormalizesCapabilityBundle()
    {
        var response = CapabilityResponse(
            "read.video",
            """
            [
              {
                "id":"read.video",
                "purpose":"Прочитать видеоряд.",
                "required":true,
                "alternatives":["read.video_frames"]
              },
              {
                "id":"transcribe.audio",
                "purpose":"Получить речь из звуковой дорожки.",
                "required":false,
                "alternatives":[]
              }
            ]
            """);

        Assert.IsTrue(ExecutorResultParser.TryReadTurn(response, out var turn));
        Assert.AreEqual(2, turn.RequestedCapabilities.Count);
        Assert.AreEqual("read.video", turn.RequestedCapabilities[0].Id);
        Assert.AreEqual("transcribe.audio", turn.RequestedCapabilities[1].Id);
        Assert.IsFalse(turn.RequestedCapabilities[1].Required);
    }

    [TestMethod]
    public void CoreAutonomySettings_ClampConfiguredTime()
    {
        var settings = new CoreAutonomySettings
        {
            MaximumIndependentSearchSeconds = 1
        };
        Assert.AreEqual(
            CoreAutonomySettings.MinimumSeconds,
            settings.MaximumIndependentSearchSeconds);

        settings.MaximumIndependentSearchSeconds = 999;
        Assert.AreEqual(
            CoreAutonomySettings.MaximumSeconds,
            settings.MaximumIndependentSearchSeconds);
    }

    [TestMethod]
    public void AutonomyBudget_StopsRepeatedIdenticalOperation()
    {
        var budget = new AutonomyExecutionBudget(
            CoreAutonomySettings.DefaultSeconds);

        Assert.IsTrue(budget.RegisterProgress("same-call"));
        Assert.IsTrue(budget.RegisterProgress("same-call"));
        Assert.IsFalse(budget.RegisterProgress("same-call"));
    }

    [TestMethod]
    public void CapabilityResolver_UsesCallableAlternative()
    {
        var resolver = new CapabilityResolverService(new ComponentManager());
        var plan = resolver.Resolve(
        [
            new ExecutorCapabilityRequest
            {
                Id = "read.unknown_table",
                Purpose = "Прочитать табличные данные.",
                Required = true,
                Alternatives = ["read.csv"]
            }
        ],
        "unit test");

        var binding = plan.Bindings.Single();
        Assert.AreEqual("read.unknown_table", binding.RequestedCapabilityId);
        Assert.AreEqual("read.csv", binding.CapabilityId);
        Assert.AreEqual(CapabilityBindingStatuses.Ready, binding.Status);
        Assert.IsTrue(binding.IsExecutable);
    }

    [TestMethod]
    public void CapabilityResolver_DoesNotDownloadPackageWithoutAdapter()
    {
        var resolver = new CapabilityResolverService(new ComponentManager());
        var plan = resolver.Resolve(
        [
            new ExecutorCapabilityRequest
            {
                Id = "edit.video",
                Purpose = "Изменить видео.",
                Required = true
            }
        ],
        "unit test");

        var binding = plan.Bindings.Single();
        Assert.IsFalse(binding.AdapterAvailable);
        Assert.AreEqual(0, plan.Acquisition.Items.Count);
        Assert.IsFalse(plan.IsExecutable);
    }

    private static string CapabilityResponse(
        string capability,
        string? requestedCapabilities = null) => $$"""
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
          "requestedCapabilities":{{requestedCapabilities ?? $$"""
            [
              {
                "id":"{{capability}}",
                "purpose":"Нужно обработать входные данные.",
                "required":true,
                "alternatives":[]
              }
            ]
            """}},
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
