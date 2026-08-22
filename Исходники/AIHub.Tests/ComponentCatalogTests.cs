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
        Assert.IsFalse(TestComponentManagerFactory.CreateEmpty().GetAvailableCapabilities()
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
        var resolver = new CapabilityResolverService(TestComponentManagerFactory.CreateEmpty());
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
        var resolver = new CapabilityResolverService(TestComponentManagerFactory.CreateEmpty());
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

    [TestMethod]
    public void CapabilityResolver_OffersTrustedOcrPackageAndCallableAdapter()
    {
        var resolver = new CapabilityResolverService(TestComponentManagerFactory.CreateEmpty());
        var plan = resolver.Resolve(
        [
            new ExecutorCapabilityRequest
            {
                Id = "ocr.image",
                Purpose = "Extract printed text from the attached image.",
                Required = true
            }
        ],
        "unit test");

        var binding = plan.Bindings.Single();
        Assert.AreEqual("extract.image_ocr", binding.CapabilityId);
        Assert.AreEqual("runtime.tesseract", binding.ComponentId);
        Assert.AreEqual("adapter.tesseract.ocr", binding.AdapterId);
        CollectionAssert.Contains(binding.ToolNames, "session_image_extract_text");
        CollectionAssert.Contains(
            plan.Acquisition.Items.Select(item => item.ComponentId).ToList(),
            "runtime.tesseract");
    }

    [TestMethod]
    public void CapabilityResolver_OffersTrustedImageTransformPackageAndCallableAdapter()
    {
        var resolver = new CapabilityResolverService(TestComponentManagerFactory.CreateEmpty());
        var plan = resolver.Resolve(
        [
            new ExecutorCapabilityRequest
            {
                Id = "image.edit",
                Purpose = "Create a resized image artifact.",
                Required = true
            }
        ],
        "unit test");

        var binding = plan.Bindings.Single();
        Assert.AreEqual("edit.image", binding.CapabilityId);
        Assert.AreEqual("runtime.imagemagick", binding.ComponentId);
        Assert.AreEqual("adapter.imagemagick.transform", binding.AdapterId);
        CollectionAssert.Contains(binding.ToolNames, "session_image_transform");
        CollectionAssert.Contains(
            plan.Acquisition.Items.Select(item => item.ComponentId).ToList(),
            "runtime.imagemagick");
    }

    [TestMethod]
    public void CapabilityResolver_ProvidesPinnedSemanticVisionRecipe()
    {
        var resolver = new CapabilityResolverService(TestComponentManagerFactory.CreateEmpty());
        var plan = resolver.Resolve(
        [
            new ExecutorCapabilityRequest
            {
                Id = "analyze.image.semantic",
                Purpose = "Understand objects and scene meaning.",
                Required = true
            }
        ],
        "unit test");

        var binding = plan.Bindings.Single();
        Assert.AreEqual("model.vision.smolvlm2.q4km", binding.ComponentId);
        Assert.AreEqual("adapter.image.semantic", binding.AdapterId);
        CollectionAssert.Contains(binding.ToolNames, "session_image_describe");
        CollectionAssert.Contains(
            plan.Acquisition.Items.Select(item => item.ComponentId).ToList(),
            "model.vision.smolvlm2.projector");
        CollectionAssert.Contains(
            plan.Acquisition.Items.Select(item => item.ComponentId).ToList(),
            "model.vision.smolvlm2.q4km");
        Assert.IsTrue(binding.AdapterAvailable);
    }

    [TestMethod]
    public void CapabilityResolver_KeepsSeveralSimultaneousRequestsInOnePlan()
    {
        var resolver = new CapabilityResolverService(TestComponentManagerFactory.CreateEmpty());
        var plan = resolver.Resolve(
        [
            new ExecutorCapabilityRequest
            {
                Id = "read.csv",
                Purpose = "Read measurements.",
                Required = true
            },
            new ExecutorCapabilityRequest
            {
                Id = "transcribe.audio",
                Purpose = "Transcribe the attached explanation.",
                Required = true
            }
        ],
        "unit test");

        Assert.AreEqual(2, plan.Requests.Count);
        Assert.AreEqual(2, plan.Bindings.Count);
        Assert.IsTrue(plan.Bindings.Any(binding =>
            binding.CapabilityId == "read.csv" && binding.IsExecutable));
        Assert.IsTrue(plan.Bindings.Any(binding =>
            binding.ComponentId == "model.whisper.small"));
        Assert.IsTrue(plan.Acquisition.Items.Any(item =>
            item.ComponentId == "runtime.whisper.cpu"));
        Assert.IsTrue(plan.Acquisition.Items.Any(item =>
            item.ComponentId == "model.whisper.small"));
    }

    [TestMethod]
    public void ComponentManager_ExplicitStateSeparatesMissingHealthyAndNeedsVerification()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "AIHubComponentStateTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var stateStore = new ComponentStateStore(Path.Combine(root, "component-state.json"));
            var manager = new ComponentManager(stateStore);
            var missing = manager.GetStatus().Single(status =>
                status.Entry.Id == "model.vision.smolvlm2.q4km");
            Assert.IsFalse(missing.IsInstalled);
            Assert.IsFalse(missing.IsAvailable);

            var projectorPath = Path.Combine(
                root,
                "mmproj-SmolVLM2-2.2B-Instruct-Q8_0.gguf");
            var modelPath = Path.Combine(
                root,
                "SmolVLM2-2.2B-Instruct-Q4_K_M.gguf");
            File.WriteAllText(projectorPath, "test projector");
            File.WriteAllText(modelPath, "test model");
            stateStore.Save(new ComponentStateDocument
            {
                Components =
                [
                    new ComponentInstallationRecord
                    {
                        ComponentId = "model.vision.smolvlm2.projector",
                        Status = ComponentInstallStatuses.Installed,
                        InstallPath = projectorPath
                    },
                    new ComponentInstallationRecord
                    {
                        ComponentId = "model.vision.smolvlm2.q4km",
                        Status = ComponentInstallStatuses.Installed,
                        InstallPath = modelPath
                    }
                ]
            });

            var healthy = manager.GetStatus().Single(status =>
                status.Entry.Id == "model.vision.smolvlm2.q4km");
            Assert.IsTrue(healthy.IsInstalled);
            Assert.IsTrue(healthy.IsHealthy);
            Assert.IsTrue(healthy.DependenciesAvailable);
            Assert.IsTrue(healthy.IsAvailable);
            var runnablePlan = new CapabilityResolverService(manager).Resolve(
                [
                    new ExecutorCapabilityRequest
                    {
                        Id = "analyze.image.semantic",
                        Purpose = "Analyze an image.",
                        Required = true
                    }
                ],
                "unit test");
            Assert.AreEqual(
                CapabilityBindingStatuses.Ready,
                runnablePlan.Bindings.Single().Status);
            Assert.IsTrue(runnablePlan.IsExecutable);

            var wrongModelDirectory = Path.Combine(root, "wrong-model-directory");
            Directory.CreateDirectory(wrongModelDirectory);
            var state = stateStore.Load();
            state.Components.Single(record =>
                record.ComponentId == "model.vision.smolvlm2.q4km").InstallPath =
                wrongModelDirectory;
            stateStore.Save(state);

            var needsVerification = manager.GetStatus().Single(status =>
                status.Entry.Id == "model.vision.smolvlm2.q4km");
            Assert.IsTrue(needsVerification.IsInstalled);
            Assert.IsFalse(needsVerification.IsHealthy);
            Assert.IsFalse(needsVerification.IsAvailable);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
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
