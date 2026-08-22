using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ExecutionEvidenceTests
{
    [TestMethod]
    public void Validate_RejectsExternalTaskWithoutToolReceipt()
    {
        var node = new ExecutionActionNode
        {
            Layer = ExecutionRouteLayers.SemanticAnalysis,
            CapabilityId = "analyze.any.semantic",
            Purpose = "Understand the supplied domain input.",
            Required = true,
            Status = ExecutionActionStatuses.Ready,
            ToolNames = ["domain_analyze"]
        };
        var graph = new ExecutionActionGraph
        {
            RequiresExternalEvidence = true,
            Nodes = [node]
        };

        var result = new ExecutionEvidenceService().Validate(graph, []);

        Assert.AreEqual(EvidenceValidationStatuses.Invalid, result.Status);
        CollectionAssert.Contains(result.MissingActionIds, node.Id);
    }

    [TestMethod]
    public void Validate_AcceptsSuccessfulReceiptForRequiredAction()
    {
        var node = new ExecutionActionNode
        {
            Layer = ExecutionRouteLayers.Action,
            CapabilityId = "edit.any.output",
            Purpose = "Create the requested output.",
            Required = true,
            Status = ExecutionActionStatuses.Ready,
            ToolNames = ["domain_edit"]
        };
        var graph = new ExecutionActionGraph
        {
            RequiresExternalEvidence = true,
            Nodes = [node]
        };
        var receipt = new ExecutionEvidenceReceipt
        {
            ActionId = node.Id,
            ToolName = "domain_edit",
            Success = true,
            Capabilities = [node.CapabilityId]
        };

        var result = new ExecutionEvidenceService().Validate(graph, [receipt]);

        Assert.AreEqual(EvidenceValidationStatuses.Valid, result.Status);
        CollectionAssert.Contains(result.SatisfiedActionIds, node.Id);
    }

    [TestMethod]
    public void Validate_AcceptsCanonicalCapabilityAliasForOutcomeAction()
    {
        var node = new ExecutionActionNode
        {
            Layer = ExecutionRouteLayers.SemanticAnalysis,
            CapabilityId = "extract.image_ocr",
            OutcomeActionId = "understand-image-text",
            Purpose = "Extract text from the supplied image.",
            Required = true,
            Status = ExecutionActionStatuses.Ready,
            ExpectedEvidenceTypes = [ExecutionEvidenceTypes.FileInspection]
        };
        var graph = new ExecutionActionGraph
        {
            RequiresExternalEvidence = true,
            Nodes = [node]
        };
        var receipt = new ExecutionEvidenceReceipt
        {
            OutcomeActionIds = [node.OutcomeActionId],
            Success = true,
            EvidenceType = ExecutionEvidenceTypes.FileInspection,
            Capabilities = ["ocr.image"]
        };

        var result = new ExecutionEvidenceService().Validate(graph, [receipt]);

        Assert.AreEqual(EvidenceValidationStatuses.Valid, result.Status);
        CollectionAssert.Contains(result.SatisfiedActionIds, node.Id);
    }

    [TestMethod]
    public void Validate_RejectsReceiptWithWrongEvidenceType()
    {
        var node = new ExecutionActionNode
        {
            Layer = ExecutionRouteLayers.Action,
            CapabilityId = "edit.image",
            OutcomeActionId = "transform-image",
            Purpose = "Produce an edited image file.",
            Required = true,
            Status = ExecutionActionStatuses.Ready,
            ExpectedEvidenceTypes = [ExecutionEvidenceTypes.ProducedArtifact]
        };
        var graph = new ExecutionActionGraph
        {
            RequiresExternalEvidence = true,
            Nodes = [node]
        };
        var receipt = new ExecutionEvidenceReceipt
        {
            ActionId = node.Id,
            OutcomeActionIds = [node.OutcomeActionId],
            Success = true,
            EvidenceType = ExecutionEvidenceTypes.FileInspection,
            Capabilities = [node.CapabilityId]
        };

        var result = new ExecutionEvidenceService().Validate(graph, [receipt]);

        Assert.AreEqual(EvidenceValidationStatuses.Limited, result.Status);
        CollectionAssert.Contains(result.MissingActionIds, node.Id);
    }

    [TestMethod]
    public void EvidenceProgressGuard_StopsRepeatedToolsWithoutNewRequiredEvidence()
    {
        var node = new ExecutionActionNode
        {
            Layer = ExecutionRouteLayers.SemanticAnalysis,
            CapabilityId = "analyze.image.semantic",
            Required = true,
            Status = ExecutionActionStatuses.Ready
        };
        var graph = new ExecutionActionGraph { Nodes = [node] };
        var guard = new ExecutionEvidenceProgressGuard();
        guard.Reset(graph);

        Assert.IsTrue(guard.Observe(graph));
        Assert.IsFalse(guard.Observe(graph));

        node.Status = ExecutionActionStatuses.Succeeded;
        Assert.IsTrue(guard.Observe(graph));
        Assert.IsTrue(guard.Observe(graph));
    }

    [TestMethod]
    public void MergeCapabilities_AddsLateRequestedToolToActionGraph()
    {
        var graph = new ExecutionActionGraph
        {
            Nodes =
            [
                new ExecutionActionNode
                {
                    Layer = "artifact",
                    CapabilityId = "artifact.document",
                    Required = true
                },
                new ExecutionActionNode
                {
                    Layer = "validation",
                    CapabilityId = "artifact.validate",
                    Required = true
                }
            ]
        };
        var capability = new ExecutorCapabilityRequest
        {
            Id = "inspect.any.input",
            Purpose = "Inspect a newly supplied input.",
            Required = true
        };
        var binding = new CapabilityAdapterBinding
        {
            RequestedCapabilityId = capability.Id,
            CapabilityId = capability.Id,
            Status = CapabilityBindingStatuses.Ready,
            PackageAvailable = true,
            AdapterAvailable = true,
            ToolNames = ["inspect_input"]
        };

        new ExecutionActionGraphService().MergeCapabilities(
            graph,
            [capability],
            [binding],
            []);

        var node = graph.Nodes.Single(item =>
            string.Equals(item.CapabilityId, capability.Id, StringComparison.Ordinal));
        Assert.IsTrue(graph.RequiresExternalEvidence);
        Assert.AreEqual(ExecutionActionStatuses.Ready, node.Status);
        CollectionAssert.Contains(node.ToolNames, "inspect_input");
        CollectionAssert.Contains(
            graph.Nodes.Single(item => item.Layer == "artifact").DependencyIds,
            node.Id);
    }

    [TestMethod]
    public void BuildHonestLimitedMarkdown_DoesNotInventDomainResult()
    {
        var node = new ExecutionActionNode
        {
            Layer = ExecutionRouteLayers.SemanticAnalysis,
            CapabilityId = "understand.domain.input",
            Purpose = "Understand the supplied input.",
            Required = true
        };
        var graph = new ExecutionActionGraph
        {
            Goal = "Получить предметный результат",
            RequiresExternalEvidence = true,
            Nodes = [node]
        };
        var validation = new EvidenceValidationResult
        {
            Status = EvidenceValidationStatuses.Invalid,
            MissingActionIds = [node.Id]
        };

        var markdown = new ExecutionEvidenceService().BuildHonestLimitedMarkdown(
            new ExecutorHandoffPackage
            {
                Goal = graph.Goal,
                LanguageCode = "ru"
            },
            "Подтверждённая постановка задачи",
            graph,
            [],
            validation);

        StringAssert.Contains(markdown, "Подтверждённая постановка задачи");
        StringAssert.Contains(markdown, "understand.domain.input");
        StringAssert.Contains(markdown, "Технические JSON-payload в текст не включены");
    }

    [TestMethod]
    public void CreateReceipt_SemanticVisionConfirmsPixelsAndMeaningForExactFile()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var imagePath = Path.Combine(root, "scene.jpg");
            File.WriteAllBytes(imagePath, [1, 2, 3, 4]);
            var manifest = new SessionFileManifest
            {
                Files =
                [
                    new SessionFileReference
                    {
                        Id = "image-1",
                        SourcePath = imagePath,
                        DisplayName = "scene.jpg",
                        Extension = ".jpg",
                        Category = SessionFileCategories.Image,
                        IsAvailable = true
                    }
                ]
            };
            var pixels = CreateImageAction(
                "pixels",
                "outcome.read.image_pixels",
                "read.image_pixels",
                "image-1");
            var semantic = CreateImageAction(
                "semantic",
                "outcome.analyze.image.semantic",
                "analyze.image.semantic",
                "image-1");
            var graph = new ExecutionActionGraph
            {
                RequiresExternalEvidence = true,
                Nodes = [pixels, semantic]
            };
            new ExecutionActionGraphService().BindInputFiles(graph, manifest);
            var toolCall = new StructuredToolCall
            {
                Id = "vision-call",
                Function = new StructuredToolCallFunction
                {
                    Name = "session_image_describe",
                    Arguments = """{"file_id":"image-1"}"""
                }
            };
            var execution = new ExecutorToolExecution(
                "session_image_describe",
                """{"success":true,"evidence_type":"semantic_vision","source_file_id":"image-1","model":"SmolVLM2","description":"A woman stands before a castle."}""",
                Success: true);

            var receipt = new ExecutionEvidenceService().CreateReceipt(
                toolCall,
                execution,
                graph,
                manifest,
                new StorageSettings());
            var validation = new ExecutionEvidenceService().Validate(graph, [receipt]);

            Assert.AreEqual(ExecutionEvidenceTypes.ToolResult, receipt.EvidenceType);
            Assert.AreEqual("image-1", receipt.InputFileId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(receipt.InputSha256));
            CollectionAssert.Contains(receipt.OutcomeActionIds, pixels.OutcomeActionId);
            CollectionAssert.Contains(receipt.OutcomeActionIds, semantic.OutcomeActionId);
            CollectionAssert.Contains(receipt.Capabilities, "read.image_pixels");
            CollectionAssert.Contains(receipt.Capabilities, "analyze.image.semantic");
            Assert.AreEqual(EvidenceValidationStatuses.Valid, validation.Status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void Validate_SemanticReceiptCannotConfirmAnotherInputFile()
    {
        var node = CreateImageAction(
            "semantic-file-2",
            "outcome.semantic-file-2",
            "analyze.image.semantic",
            "image-2");
        var graph = new ExecutionActionGraph
        {
            RequiresExternalEvidence = true,
            Nodes = [node]
        };
        var receipt = new ExecutionEvidenceReceipt
        {
            ToolName = "session_image_describe",
            InputFileId = "image-1",
            InputSha256 = "hash-1",
            EvidenceType = ExecutionEvidenceTypes.ToolResult,
            Success = true,
            Capabilities = ["analyze.image.semantic"]
        };

        var validation = new ExecutionEvidenceService().Validate(graph, [receipt]);

        Assert.AreEqual(EvidenceValidationStatuses.Limited, validation.Status);
        CollectionAssert.Contains(validation.MissingActionIds, node.Id);
    }

    [TestMethod]
    public void Validate_SemanticReceiptCannotConfirmChangedFileWithSameId()
    {
        var node = CreateImageAction(
            "semantic-file",
            "outcome.semantic-file",
            "analyze.image.semantic",
            "image-1");
        node.InputSha256ByFileId["image-1"] = "expected-hash";
        var graph = new ExecutionActionGraph
        {
            RequiresExternalEvidence = true,
            Nodes = [node]
        };
        var receipt = new ExecutionEvidenceReceipt
        {
            ToolName = "session_image_describe",
            InputFileId = "image-1",
            InputSha256 = "different-hash",
            EvidenceType = ExecutionEvidenceTypes.ToolResult,
            Success = true,
            Capabilities = ["analyze.image.semantic"]
        };

        var validation = new ExecutionEvidenceService().Validate(graph, [receipt]);

        Assert.AreEqual(EvidenceValidationStatuses.Limited, validation.Status);
        CollectionAssert.Contains(validation.MissingActionIds, node.Id);
    }

    [TestMethod]
    public void CreateReceipt_EvidenceTypeMismatchPreservesIdentityAndDiagnostic()
    {
        var node = CreateImageAction(
            "semantic-artifact",
            "outcome.semantic-artifact",
            "analyze.image.semantic",
            "image-1");
        node.ExpectedEvidenceTypes = [ExecutionEvidenceTypes.ProducedArtifact];
        var graph = new ExecutionActionGraph { RequiresExternalEvidence = true, Nodes = [node] };
        var manifest = new SessionFileManifest
        {
            Files =
            [
                new SessionFileReference
                {
                    Id = "image-1",
                    DisplayName = "scene.jpg",
                    Category = SessionFileCategories.Image,
                    IsAvailable = true
                }
            ]
        };
        var receipt = new ExecutionEvidenceService().CreateReceipt(
            new StructuredToolCall
            {
                Id = "vision-call",
                Function = new StructuredToolCallFunction
                {
                    Name = "session_image_describe",
                    Arguments = """{"file_id":"image-1"}"""
                }
            },
            new ExecutorToolExecution(
                "session_image_describe",
                """{"success":true,"description":"Visible scene."}""",
                Success: true),
            graph,
            manifest,
            new StorageSettings());

        Assert.AreEqual(node.Id, receipt.ActionId);
        CollectionAssert.Contains(receipt.OutcomeActionIds, node.OutcomeActionId);
        StringAssert.Contains(receipt.DiagnosticMessage, "incompatible");
    }

    [TestMethod]
    public void BuildHonestLimitedMarkdown_NormalizesVisionPayloadAndKeepsSavedFragments()
    {
        const string rawPayload = """{"success":true,"evidence_type":"semantic_vision","description":"A moonlit castle scene."}""";
        var graph = new ExecutionActionGraph { RequiresExternalEvidence = true };
        var markdown = new ExecutionEvidenceService().BuildHonestLimitedMarkdown(
            new ExecutorHandoffPackage
            {
                Goal = "Старая гипотеза",
                LanguageCode = "ru"
            },
            "Выполнить художественный анализ изображения",
            graph,
            [
                new ExecutionEvidenceReceipt
                {
                    ToolName = "session_image_describe",
                    ResultExcerpt = rawPayload,
                    Success = true
                }
            ],
            new EvidenceValidationResult { Status = EvidenceValidationStatuses.Limited },
            ["Композиция строится вокруг контраста фигуры и замка."],
            "Атмосфера готическая и напряжённая.");

        StringAssert.Contains(markdown, "Выполнить художественный анализ изображения");
        StringAssert.Contains(markdown, "A moonlit castle scene.");
        StringAssert.Contains(markdown, "Композиция строится");
        StringAssert.Contains(markdown, "Атмосфера готическая");
        Assert.IsFalse(markdown.Contains(rawPayload, StringComparison.Ordinal));
        Assert.IsFalse(markdown.Contains("\"evidence_type\"", StringComparison.Ordinal));
        Assert.IsFalse(markdown.Contains("Старая гипотеза", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NoOcrProfile_RemovesOptionalOcrFromOutcomeRouteAndGraph()
    {
        var profile = CreateImageProfile("no_ocr");
        var manifest = new SessionFilePromptManifest
        {
            Intent = SessionFileIntentStatuses.Selected,
            FileCount = 1,
            Files =
            [
                new SessionFilePromptItem
                {
                    Id = "image-1",
                    Name = "scene.jpg",
                    Extension = ".jpg",
                    Category = SessionFileCategories.Image,
                    IsAvailable = true
                }
            ]
        };
        var pattern = new SandboxWorkPattern
        {
            Id = "image.describe",
            RequiredCapabilities = ["read.image_pixels", "analyze.image.semantic"],
            OptionalCapabilities = ["extract.image_ocr"]
        };
        var artifact = new ArtifactContract
        {
            ArtifactKind = ArtifactKinds.Document,
            PreferredExtension = ".docx",
            MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };
        var outcome = new ExecutionOutcomeContractService().Build(
            "Analyze the image",
            profile,
            manifest,
            [pattern],
            artifact);
        var route = TestComponentManagerFactory.CreateEmptyRoutePlanner().Build(
            profile,
            manifest,
            "unit test",
            [pattern],
            outcomeContract: outcome);
        var handoff = new ExecutorHandoffPackage
        {
            SuggestedDirection = "Analyze the image",
            FileManifest = manifest,
            ArtifactContract = artifact,
            OutcomeContract = outcome,
            ExecutionBundle = new ExecutionBundlePlan
            {
                SelectedRouteLevel = ExecutionRouteLevels.Preferred,
                PreferredRoute = new ExecutionRouteVariant
                {
                    Level = ExecutionRouteLevels.Preferred,
                    Route = route
                }
            }
        };
        var graph = new ExecutionActionGraphService().Build(handoff);

        Assert.IsFalse(outcome.Actions.Any(action => action.CapabilityIds.Contains(
            "extract.image_ocr",
            StringComparer.OrdinalIgnoreCase)));
        Assert.IsFalse(route.Requirements.Any(requirement =>
            requirement.Request.Id == "extract.image_ocr"));
        Assert.IsFalse(route.Resolution.Bindings.Any(binding =>
            binding.RequestedCapabilityId == "extract.image_ocr"));
        Assert.IsFalse(route.Resolution.Acquisition.Items.Any(item =>
            item.ComponentId.Contains("tesseract", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(graph.Nodes.Any(node => node.CapabilityId == "extract.image_ocr"));
        Assert.IsTrue(graph.Nodes.Any(node => node.CapabilityId == "read.image_pixels"));
        Assert.IsTrue(graph.Nodes.Any(node => node.CapabilityId == "analyze.image.semantic"));
    }

    [TestMethod]
    public void PositiveOcrProfile_AddsOcrWithoutReplacingSemanticVision()
    {
        var profile = CreateImageProfile("ocr_required");
        var capabilities = ComponentCapabilityMapper.FromProfile(profile);
        var combinedRequirements = capabilities
            .Append("analyze.image.semantic")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        CollectionAssert.Contains(capabilities.ToList(), "extract.image_ocr");
        CollectionAssert.Contains(capabilities.ToList(), "read.image_pixels");
        CollectionAssert.Contains(combinedRequirements, "analyze.image.semantic");
        CollectionAssert.Contains(combinedRequirements, "extract.image_ocr");
    }

    [TestMethod]
    [DataRow("no_ocr")]
    [DataRow("without_ocr")]
    [DataRow("ocr_not_required")]
    [DataRow("ocr_not_needed")]
    public void NegativeOcrAliases_DoNotActivateOcr(string profileValue)
    {
        var capabilities = ComponentCapabilityMapper.FromProfile(
            CreateImageProfile(profileValue));

        CollectionAssert.DoesNotContain(capabilities.ToList(), "extract.image_ocr");
    }

    [TestMethod]
    public void BuildEvidencePacket_ContainsOnlyRecordedToolFactsAndMissingActions()
    {
        var completedNode = new ExecutionActionNode
        {
            Id = "inspect-node",
            Layer = ExecutionRouteLayers.Decode,
            CapabilityId = "inspect.any.input",
            Purpose = "Inspect the supplied input.",
            Required = true,
            ReceiptIds = ["receipt-1"]
        };
        var missingNode = new ExecutionActionNode
        {
            Id = "semantic-node",
            Layer = ExecutionRouteLayers.SemanticAnalysis,
            CapabilityId = "understand.any.input",
            Purpose = "Understand the supplied input.",
            Required = true
        };
        var graph = new ExecutionActionGraph
        {
            Id = "graph-1",
            RequiresExternalEvidence = true,
            Nodes = [completedNode, missingNode]
        };
        var receipt = new ExecutionEvidenceReceipt
        {
            Id = "receipt-1",
            ActionId = completedNode.Id,
            ToolName = "inspect_input",
            ComponentIds = ["component-inspector"],
            InputFileName = "sample.bin",
            InputSha256 = "input-hash",
            OutputArtifactPath = "result.json",
            OutputSha256 = "output-hash",
            ResultHash = "result-hash",
            ResultExcerpt = "width=10 height=20",
            EvidenceType = ExecutionEvidenceTypes.FileInspection,
            ConfirmedClaimScopes = [completedNode.CapabilityId],
            Limitations = "No semantic interpretation.",
            Success = true
        };
        var validation = new EvidenceValidationResult
        {
            GraphId = graph.Id,
            Status = EvidenceValidationStatuses.Limited,
            SatisfiedActionIds = [completedNode.Id],
            MissingActionIds = [missingNode.Id],
            ReceiptIds = [receipt.Id]
        };

        var packet = new ExecutionEvidenceService().BuildEvidencePacket(
            graph,
            [receipt],
            validation);

        StringAssert.Contains(packet, "[AI_HUB_VERIFIED_EXECUTION_EVIDENCE]");
        StringAssert.Contains(packet, "inspect.any.input");
        StringAssert.Contains(packet, "sample.bin");
        StringAssert.Contains(packet, "component-inspector");
        StringAssert.Contains(packet, "width=10 height=20");
        StringAssert.Contains(packet, "No semantic interpretation");
        StringAssert.Contains(packet, "understand.any.input");
        StringAssert.Contains(packet, "Understand the supplied input");
    }

    [TestMethod]
    public void Materialize_PrefersArtifactProducedBySuccessfulTool()
    {
        var root = CreateTemporaryDirectory();
        try
        {
            var producedPath = Path.Combine(root, "tool-result.txt");
            File.WriteAllText(producedPath, "real tool output");
            var receipt = new ExecutionEvidenceReceipt
            {
                Id = "receipt-real-output",
                ToolName = "domain_export",
                OutputArtifactPath = producedPath,
                Success = true
            };
            var handoff = new ExecutorHandoffPackage
            {
                ArtifactContract = new ArtifactContract
                {
                    ArtifactKind = ArtifactKinds.Text,
                    PreferredExtension = ".txt",
                    MimeType = "text/plain"
                },
                ExecutionBundle = new ExecutionBundlePlan
                {
                    SelectedRouteLevel = ExecutionRouteLevels.Preferred,
                    Recipes =
                    [
                        new SandboxExecutionRecipe
                        {
                            Id = "test.real-output",
                            ArtifactKind = ArtifactKinds.Text
                        }
                    ]
                }
            };
            var storage = new StorageSettings
            {
                Results = new StorageCategorySettings
                {
                    Locations = [new StorageLocationSettings { Path = root, LimitGb = 1 }]
                }
            };

            var result = new SandboxArtifactMaterializerService().Materialize(
                new ExecutorResultSnapshot
                {
                    Version = 1,
                    Markdown = "model invented output"
                },
                handoff,
                new SessionFileManifest(),
                storage,
                [receipt]);

            Assert.AreEqual(receipt.Id, result.SourceReceiptId);
            Assert.AreEqual("real tool output", File.ReadAllText(result.FilePath));
            Assert.IsTrue(result.Validation.IsValid);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void ExecutorCheckpoint_RoundTripPreservesActionGraphAndEvidenceReceipts()
    {
        var checkpoint = new ExecutorSessionCheckpoint
        {
            ActionGraph = new ExecutionActionGraph
            {
                Id = "graph-round-trip",
                RequiresExternalEvidence = true,
                Nodes =
                [
                    new ExecutionActionNode
                    {
                        Id = "node-round-trip",
                        CapabilityId = "inspect.any.input",
                        ComponentIds = ["component-round-trip"],
                        ReceiptIds = ["receipt-round-trip"]
                    }
                ]
            },
            EvidenceReceipts =
            [
                new ExecutionEvidenceReceipt
                {
                    Id = "receipt-round-trip",
                    ActionId = "node-round-trip",
                    ToolName = "inspect_input",
                    ComponentIds = ["component-round-trip"],
                    EvidenceType = ExecutionEvidenceTypes.FileInspection,
                    ConfirmedClaimScopes = ["inspect.any.input"],
                    InputSha256 = "input-hash",
                    OutputSha256 = "output-hash",
                    Success = true
                }
            ]
        };

        var json = JsonSerializer.Serialize(checkpoint);
        var restored = JsonSerializer.Deserialize<ExecutorSessionCheckpoint>(json);

        Assert.IsNotNull(restored);
        Assert.IsNotNull(restored.ActionGraph);
        Assert.AreEqual("graph-round-trip", restored.ActionGraph.Id);
        Assert.AreEqual("component-round-trip", restored.ActionGraph.Nodes[0].ComponentIds[0]);
        Assert.AreEqual("receipt-round-trip", restored.EvidenceReceipts[0].Id);
        Assert.AreEqual(ExecutionEvidenceTypes.FileInspection, restored.EvidenceReceipts[0].EvidenceType);
        Assert.AreEqual("output-hash", restored.EvidenceReceipts[0].OutputSha256);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "AIHubExecutionEvidenceTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static ExecutionActionNode CreateImageAction(
        string id,
        string outcomeActionId,
        string capability,
        string inputFileId) =>
        new()
        {
            Id = id,
            Layer = ExecutionRouteLayers.SemanticAnalysis,
            CapabilityId = capability,
            OutcomeActionId = outcomeActionId,
            Purpose = capability,
            Required = true,
            Status = ExecutionActionStatuses.Ready,
            ToolNames = ["session_image_describe"],
            InputFileIds = [inputFileId],
            ExpectedEvidenceTypes = [ExecutionEvidenceTypes.ToolResult]
        };

    private static ChoiceCapabilityProfile CreateImageProfile(string toolRequirement) =>
        new()
        {
            Dimensions =
            [
                new ChoiceCapabilityDimension
                {
                    Dimension = ChoiceDecisionDimensions.InputModality,
                    Status = ChoiceDimensionStatuses.Resolved,
                    Values = ["file:image"]
                },
                new ChoiceCapabilityDimension
                {
                    Dimension = ChoiceDecisionDimensions.TaskType,
                    Status = ChoiceDimensionStatuses.Resolved,
                    Values = ["image_description"]
                },
                new ChoiceCapabilityDimension
                {
                    Dimension = ChoiceDecisionDimensions.ToolRequirements,
                    Status = ChoiceDimensionStatuses.Resolved,
                    Values = [toolRequirement]
                }
            ]
        };
}
