using System.Text;
using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class SandboxOrchestrationTests
{
    [TestMethod]
    public void WorkPatternResolution_DropsWeakSecondaryPatternFromExecution()
    {
        var selected = new WorkPatternCatalogService().ResolveSelected(
            new WorkPatternSelectionResult
            {
                Selections =
                [
                    new WorkPatternSelection
                    {
                        PatternId = "image.describe",
                        MatchPercent = 95
                    },
                    new WorkPatternSelection
                    {
                        PatternId = "image.ocr",
                        MatchPercent = 5
                    }
                ]
            });

        CollectionAssert.Contains(selected.Select(pattern => pattern.Id).ToList(), "image.describe");
        CollectionAssert.DoesNotContain(selected.Select(pattern => pattern.Id).ToList(), "image.ocr");
    }

    [TestMethod]
    public void WorkPatternResolution_KeepsMultipleConfidentPatterns()
    {
        var selected = new WorkPatternCatalogService().ResolveSelected(
            new WorkPatternSelectionResult
            {
                Selections =
                [
                    new WorkPatternSelection
                    {
                        PatternId = "image.describe",
                        MatchPercent = 90
                    },
                    new WorkPatternSelection
                    {
                        PatternId = "image.ocr",
                        MatchPercent = 70
                    }
                ]
            });

        CollectionAssert.Contains(selected.Select(pattern => pattern.Id).ToList(), "image.describe");
        CollectionAssert.Contains(selected.Select(pattern => pattern.Id).ToList(), "image.ocr");
    }

    [TestMethod]
    public void WorkPatternCatalog_ContainsExpectedSandboxPatterns()
    {
        var catalog = new WorkPatternCatalogService().Load();

        Assert.AreEqual(1, catalog.SchemaVersion);
        Assert.AreEqual(28, catalog.Patterns.Count);
        Assert.AreEqual(
            catalog.Patterns.Count,
            catalog.Patterns.Select(pattern => pattern.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
        Assert.IsTrue(catalog.Patterns.Any(pattern =>
            string.Equals(pattern.Id, "other.custom", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void WorkPatternSelectionValidation_RemovesNoiseAndClampsPercent()
    {
        var validated = new WorkPatternCatalogService().ValidateSelection(new()
        {
            Source = "core",
            Selections =
            [
                new WorkPatternSelection
                {
                    PatternId = "image.restore",
                    MatchPercent = 140,
                    Reason = "primary"
                },
                new WorkPatternSelection
                {
                    PatternId = "IMAGE.RESTORE",
                    MatchPercent = 80,
                    Reason = "duplicate"
                },
                new WorkPatternSelection
                {
                    PatternId = "not.in.catalog",
                    MatchPercent = 100,
                    Reason = "unknown"
                }
            ]
        });

        Assert.AreEqual(1, validated.Selections.Count);
        Assert.AreEqual("image.restore", validated.Selections[0].PatternId);
        Assert.AreEqual(100, validated.Selections[0].MatchPercent);
        Assert.IsFalse(validated.UsedFallback);
    }

    [TestMethod]
    public void WorkPatternSelectionValidation_UsesCustomFallback()
    {
        var validated = new WorkPatternCatalogService().ValidateSelection(new()
        {
            Selections =
            [
                new WorkPatternSelection
                {
                    PatternId = "unknown",
                    MatchPercent = 50
                }
            ]
        });

        Assert.AreEqual(1, validated.Selections.Count);
        Assert.AreEqual("other.custom", validated.Selections[0].PatternId);
        Assert.IsTrue(validated.UsedFallback);
        Assert.AreEqual("program_fallback", validated.Source);
    }

    [TestMethod]
    public void ArtifactContractBuilder_UsesPatternAndFileManifest()
    {
        var pattern = new WorkPatternCatalogService().Load().Patterns.Single(item =>
            string.Equals(item.Id, "image.restore", StringComparison.Ordinal));
        var contract = new ArtifactContractBuilder().Build(
            [pattern],
            new SessionFilePromptManifest
            {
                Intent = SessionFileIntentStatuses.Selected,
                Files =
                [
                    new SessionFilePromptItem
                    {
                        Name = "old-photo.webp",
                        Extension = ".webp",
                        Category = SessionFileCategories.Image,
                        IsAvailable = true
                    }
                ]
            });

        Assert.AreEqual(ArtifactKinds.Image, contract.ArtifactKind);
        Assert.AreEqual(".png", contract.PreferredExtension);
        CollectionAssert.Contains(contract.InputFileNames, "old-photo.webp");
        CollectionAssert.Contains(contract.InputFormats, ".webp");
        Assert.IsFalse(string.IsNullOrWhiteSpace(contract.EmergencyAcceptableResult));
    }

    [TestMethod]
    public void ExecutionBundle_BlockedPreferredRouteStillHasStartableFallbacks()
    {
        var selection = new WorkPatternSelectionResult
        {
            Source = "core",
            Selections =
            [
                new WorkPatternSelection
                {
                    PatternId = "image.restore",
                    MatchPercent = 92
                }
            ]
        };
        var route = CreateBlockedRoute("analyze.image.semantic");
        var contract = new ArtifactContract
        {
            ArtifactKind = ArtifactKinds.Image,
            PreferredExtension = ".png",
            MimeType = "image/png",
            EmergencyAcceptableResult = "A readable image file."
        };

        var bundle = new ExecutionBundlePlannerService().Build(
            selection,
            contract,
            route);

        Assert.IsFalse(bundle.PreferredRoute.IsStartable);
        Assert.IsTrue(bundle.DegradedRoute.IsStartable);
        Assert.IsTrue(bundle.EmergencyRoute.IsStartable);
        Assert.IsTrue(bundle.CanStart);
        Assert.AreEqual(ExecutionRouteLevels.Preferred, bundle.SelectedRouteLevel);
        CollectionAssert.Contains(
            bundle.PreferredRoute.MissingCapabilities,
            "analyze.image.semantic");
        Assert.AreEqual(1, bundle.Recipes.Count);
        CollectionAssert.Contains(bundle.Recipes[0].PatternIds, "image.restore");
    }

    [TestMethod]
    public void CoordinatorScoring_IsDeterministicAndTaskAware()
    {
        var pattern = new SandboxWorkPattern
        {
            Id = "image.restore",
            NameEn = "Image restoration",
            DescriptionEn = "Restore and upscale damaged images.",
            Signals = ["image", "restore", "upscale"],
            RequiredCapabilities = ["read.image.pixels"]
        };
        var profile = new ChoiceCapabilityProfile
        {
            Dimensions =
            [
                new ChoiceCapabilityDimension
                {
                    Dimension = ChoiceDecisionDimensions.TaskType,
                    Status = ChoiceDimensionStatuses.Resolved,
                    Values = ["image restoration"]
                }
            ]
        };
        var visionCandidate = CreateCandidate(
            "vision/image-restorer-20b",
            "Vision image restoration upscale pixels");
        var unrelatedCandidate = CreateCandidate(
            "legal/contract-translator-20b",
            "Legal language translation contracts");
        var service = new CoordinatorMatchScoringService();

        var first = service.Score(visionCandidate, [pattern], profile, installed: false);
        var repeated = service.Score(visionCandidate, [pattern], profile, installed: false);
        var unrelated = service.Score(unrelatedCandidate, [pattern], profile, installed: false);

        Assert.AreEqual(first.Percent, repeated.Percent);
        Assert.AreEqual(first.Reason, repeated.Reason);
        Assert.IsTrue(
            first.Percent > unrelated.Percent,
            $"Expected task-aware candidate to win: {first.Percent} <= {unrelated.Percent}.");
    }

    [TestMethod]
    public void ExecutorHandoffPackage_JsonRoundTripPreservesSandboxPlan()
    {
        var source = new ExecutorHandoffPackage
        {
            Goal = "Restore a photo",
            WorkPatterns = new WorkPatternSelectionResult
            {
                Selections =
                [
                    new WorkPatternSelection
                    {
                        PatternId = "image.restore",
                        MatchPercent = 88
                    }
                ]
            },
            ArtifactContract = new ArtifactContract
            {
                ArtifactKind = ArtifactKinds.Image,
                PreferredExtension = ".png"
            },
            ExecutionBundle = new ExecutionBundlePlan
            {
                PatternIds = ["image.restore"],
                SelectedRouteLevel = ExecutionRouteLevels.Degraded,
                Recipes =
                [
                    new SandboxExecutionRecipe
                    {
                        Id = "sandbox.image.best_effort.v1",
                        PatternIds = ["image.restore"],
                        ArtifactKind = ArtifactKinds.Image
                    }
                ]
            }
        };

        var json = JsonSerializer.Serialize(source);
        var restored = JsonSerializer.Deserialize<ExecutorHandoffPackage>(json);

        Assert.IsNotNull(restored);
        Assert.AreEqual("image.restore", restored.WorkPatterns.Selections[0].PatternId);
        Assert.AreEqual(ArtifactKinds.Image, restored.ArtifactContract.ArtifactKind);
        Assert.AreEqual(
            ExecutionRouteLevels.Degraded,
            restored.ExecutionBundle.SelectedRouteLevel);
        Assert.AreEqual(
            "sandbox.image.best_effort.v1",
            restored.ExecutionBundle.Recipes[0].Id);
    }

    [TestMethod]
    public void ArtifactValidation_RejectsEmptyTextAndAcceptsReadableText()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var emptyPath = Path.Combine(root, "empty.txt");
            var textPath = Path.Combine(root, "result.txt");
            File.WriteAllText(emptyPath, string.Empty);
            File.WriteAllText(textPath, "A concrete result.", Encoding.UTF8);
            var contract = new ArtifactContract
            {
                ArtifactKind = ArtifactKinds.Text,
                PreferredExtension = ".txt",
                MimeType = "text/plain"
            };
            var service = new ArtifactValidationService();

            var empty = service.Validate(emptyPath, contract);
            var readable = service.Validate(textPath, contract);

            Assert.IsFalse(empty.IsValid);
            Assert.IsTrue(readable.IsValid);
            Assert.AreEqual("text/plain", readable.DetectedMimeType);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void ArtifactMaterializer_CreatesAndValidatesRequestedFiles()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var service = new SandboxArtifactMaterializerService();
            var storage = CreateStorage(root);
            var snapshot = CreateSnapshot();

            var document = service.Materialize(
                snapshot,
                CreateHandoff(ArtifactKinds.Document, ".docx"),
                new SessionFileManifest(),
                storage);
            var audio = service.Materialize(
                snapshot,
                CreateHandoff(ArtifactKinds.Audio, ".wav"),
                new SessionFileManifest(),
                storage);
            var image = service.Materialize(
                snapshot,
                CreateHandoff(ArtifactKinds.Image, ".png"),
                new SessionFileManifest(),
                storage);

            AssertMaterialized(document, ".docx");
            AssertMaterialized(audio, ".wav");
            AssertMaterialized(image, ".png");
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void ArtifactMaterializer_VideoFallbackCopiesSourceWithoutChangingIt()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(root, "source.mp4");
            File.WriteAllBytes(
                sourcePath,
                [
                    0x00, 0x00, 0x00, 0x18,
                    0x66, 0x74, 0x79, 0x70,
                    0x69, 0x73, 0x6F, 0x6D,
                    0x00, 0x00, 0x00, 0x00
                ]);
            var manifest = new SessionFileManifest
            {
                Intent = SessionFileIntentStatuses.Selected,
                Files =
                [
                    new SessionFileReference
                    {
                        Id = "video-1",
                        SourcePath = sourcePath,
                        DisplayName = "source.mp4",
                        Extension = ".mp4",
                        Category = SessionFileCategories.Video,
                        SizeBytes = new FileInfo(sourcePath).Length,
                        IsAvailable = true
                    }
                ]
            };

            var result = new SandboxArtifactMaterializerService().Materialize(
                CreateSnapshot(),
                CreateHandoff(ArtifactKinds.Video, ".mp4"),
                manifest,
                CreateStorage(root));

            AssertMaterialized(result, ".mp4");
            Assert.AreNotEqual(
                Path.GetFullPath(sourcePath),
                Path.GetFullPath(result.FilePath));
            CollectionAssert.AreEqual(
                File.ReadAllBytes(sourcePath),
                File.ReadAllBytes(result.FilePath));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [TestMethod]
    public void CapabilityResolver_MapsAudioTranscriptionToSpecialistModelAndRuntime()
    {
        var plan = new CapabilityResolverService(TestComponentManagerFactory.CreateEmpty()).Resolve(
            [
                new ExecutorCapabilityRequest
                {
                    Id = "transcribe.audio",
                    Purpose = "Create a transcript.",
                    Required = true
                }
            ],
            "Test specialist route.");

        Assert.AreEqual(1, plan.Bindings.Count);
        Assert.AreEqual(
            "extract.audio_transcript.multilingual",
            plan.Bindings[0].CapabilityId);
        Assert.AreEqual("model.whisper.small", plan.Bindings[0].ComponentId);
        Assert.AreEqual("adapter.whisper.transcribe", plan.Bindings[0].AdapterId);
        CollectionAssert.Contains(
            plan.Bindings[0].ToolNames,
            "session_audio_transcribe");
        CollectionAssert.Contains(
            plan.Acquisition.Items.Select(item => item.ComponentId).ToList(),
            "runtime.whisper.cpu");
        CollectionAssert.Contains(
            plan.Acquisition.Items.Select(item => item.ComponentId).ToList(),
            "model.whisper.small");
    }

    [TestMethod]
    public void ExecutorToolCatalog_ExposesOnlyExplicitlyEnabledSpecialistTools()
    {
        var withoutAdapters = ExecutorToolCatalog.CreateDefinitions(
            includeWeb: false,
            includeSessionFiles: true);
        var withAdapter = ExecutorToolCatalog.CreateDefinitions(
            includeWeb: false,
            includeSessionFiles: true,
            adapterToolNames: ["session_image_inspect_pixels"]);

        Assert.IsFalse(withoutAdapters.Any(tool =>
            tool.Function.Name == "session_image_inspect_pixels"));
        Assert.IsTrue(withAdapter.Any(tool =>
            tool.Function.Name == "session_image_inspect_pixels"));
        Assert.IsFalse(withAdapter.Any(tool =>
            tool.Function.Name == "session_audio_transcribe"));
    }

    [TestMethod]
    public void ExecutorToolCatalog_ExposesPreparedImageToolsOnlyWhenEnabled()
    {
        var definitions = ExecutorToolCatalog.CreateDefinitions(
            includeWeb: false,
            includeSessionFiles: true,
            adapterToolNames:
            [
                "session_image_inspect_extended",
                "session_image_extract_text",
                "session_image_transform"
            ]);

        CollectionAssert.IsSubsetOf(
            new[]
            {
                "session_image_inspect_extended",
                "session_image_extract_text",
                "session_image_transform"
            },
            definitions.Select(tool => tool.Function.Name).ToArray());
        Assert.IsFalse(definitions.Any(tool =>
            tool.Function.Name == "session_audio_transcribe"));
    }

    [TestMethod]
    public void ImagePixelAdapter_ReturnsVerifiedPropertiesWithoutSemanticClaim()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var imagePath = Path.Combine(root, "pixel.png");
            File.WriteAllBytes(
                imagePath,
                Convert.FromBase64String(
                    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9WlS8AAAAASUVORK5CYII="));
            var manifest = new SessionFileManifest
            {
                Intent = SessionFileIntentStatuses.Selected,
                Files =
                [
                    new SessionFileReference
                    {
                        Id = "image-1",
                        SourcePath = imagePath,
                        DisplayName = "pixel.png",
                        Extension = ".png",
                        Category = SessionFileCategories.Image,
                        SizeBytes = new FileInfo(imagePath).Length,
                        IsAvailable = true
                    }
                ]
            };

            var json = new SpecialistComponentToolService()
                .InspectImagePixels(manifest, "image-1");
            using var document = JsonDocument.Parse(json);

            Assert.IsTrue(document.RootElement.GetProperty("success").GetBoolean());
            Assert.AreEqual(1, document.RootElement.GetProperty("width").GetInt32());
            Assert.AreEqual(1, document.RootElement.GetProperty("height").GetInt32());
            Assert.IsFalse(
                document.RootElement
                    .GetProperty("semantic_content_understood")
                    .GetBoolean());
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static ExecutionRoutePlan CreateBlockedRoute(string capabilityId)
    {
        var request = new ExecutorCapabilityRequest
        {
            Id = capabilityId,
            Purpose = "Understand the source image.",
            Required = true
        };
        return new ExecutionRoutePlan
        {
            Requirements =
            [
                new ExecutionRouteRequirement
                {
                    Layer = ExecutionRouteLayers.SemanticAnalysis,
                    Request = request
                }
            ],
            Resolution = new CapabilityResolutionPlan
            {
                Requests = [request],
                Bindings =
                [
                    new CapabilityAdapterBinding
                    {
                        RequestedCapabilityId = capabilityId,
                        Required = true,
                        Status = CapabilityBindingStatuses.UnknownCapability,
                        PackageAvailable = false,
                        AdapterAvailable = false
                    }
                ]
            }
        };
    }

    private static ChoiceExecutorPoolCandidate CreateCandidate(
        string model,
        string semanticDescription) => new()
        {
            Model = model,
            Family = model.Split('/')[0],
            PipelineTag = "text-generation",
            ModelType = "coordinator",
            ParameterCount = 20_000_000_000,
            SemanticDescriptionEn = semanticDescription,
            RuntimeCompatible = true,
            HardwareStatus = "fit"
        };

    private static StorageSettings CreateStorage(string root) => new()
    {
        Results = new StorageCategorySettings
        {
            Locations =
            [
                new StorageLocationSettings
                {
                    Path = root,
                    LimitGb = 1
                }
            ]
        }
    };

    private static ExecutorResultSnapshot CreateSnapshot() => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Version = 1,
        CreatedAt = DateTimeOffset.UtcNow,
        StageId = "result",
        Title = "Sandbox result",
        Markdown = "# Result\n\nA concrete best-effort artifact.",
        IsFinal = true
    };

    private static ExecutorHandoffPackage CreateHandoff(
        string artifactKind,
        string extension) => new()
        {
            ArtifactContract = new ArtifactContract
            {
                ArtifactKind = artifactKind,
                PreferredExtension = extension,
                MimeType = artifactKind switch
                {
                    ArtifactKinds.Document =>
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ArtifactKinds.Image => "image/png",
                    ArtifactKinds.Audio => "audio/wav",
                    ArtifactKinds.Video => "video/mp4",
                    _ => "application/octet-stream"
                },
                EmergencyAcceptableResult = "A valid best-effort artifact."
            },
            ExecutionBundle = new ExecutionBundlePlan
            {
                SelectedRouteLevel = ExecutionRouteLevels.Emergency,
                Recipes =
            [
                new SandboxExecutionRecipe
                {
                    Id = $"sandbox.{artifactKind}.test.v1",
                    ArtifactKind = artifactKind
                }
            ]
            }
        };

    private static void AssertMaterialized(
        SandboxArtifactMaterializationResult result,
        string extension)
    {
        Assert.IsTrue(File.Exists(result.FilePath), result.FilePath);
        Assert.AreEqual(extension, Path.GetExtension(result.FilePath));
        Assert.IsTrue(result.Validation.IsValid, string.Join("; ", result.Validation.Errors));
        Assert.IsTrue(new FileInfo(result.FilePath).Length > 0);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "AIHubSandboxOrchestrationTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
