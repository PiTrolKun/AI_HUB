using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ChoiceExecutorCandidatePoolServiceTests
{
    [TestMethod]
    public void CreatePool_KeepsAllVerifiedCoordinatorAlternatives()
    {
        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            CreateCatalog(),
            CreateProfile(),
            UserWorkloadModes.Balanced,
            CreatePassport(),
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());

        Assert.AreEqual(1, pool.InstalledCandidates.Count);
        Assert.AreEqual("installed_1", pool.InstalledCandidates[0].Id);
        Assert.IsTrue(pool.AlternativeCandidates.Any(candidate => candidate.Family == "qwen"));
        CollectionAssert.AreEquivalent(
            new[] { "qwen", "gpt-oss", "gemma" },
            pool.AlternativeCandidates.Select(candidate => candidate.Family).ToArray());
    }

    [TestMethod]
    public void TryApplySelection_AllowsInstalledCandidateToBePreferred()
    {
        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            CreateCatalog(),
            CreateProfile(),
            UserWorkloadModes.Balanced,
            CreatePassport(),
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());
        var card = CreateCard(pool.InstalledCandidates[0].Id, pool.AlternativeCandidates[0].Id);
        card.ExecutorSelection.PreferredCandidateId = pool.InstalledCandidates[0].Id;

        Assert.IsTrue(TryApplySelection(card, pool, out var error), error);
        Assert.AreEqual("sm54/Qwen3.6-27B-Q4_K_M-GGUF", card.RecommendedExecutor);
        Assert.AreEqual(2, card.ExecutorCandidates.Count);
        Assert.AreEqual(1, card.ExecutorCandidates.Count(candidate => candidate.IsRecommended));
        Assert.AreNotSame(pool.ExecutionRoute, card.ExecutionRoute);
        CollectionAssert.Contains(
            card.ExecutionRoute.Requirements.Select(item => item.Request.Id).ToList(),
            "read.text");
    }

    [TestMethod]
    public void TryApplySelection_AllowsSingleTrustedCoordinator()
    {
        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            new ModelCatalogSearchResponse(),
            CreateProfile(),
            UserWorkloadModes.Balanced,
            CreatePassport(),
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());
        var card = CreateCard(pool.InstalledCandidates[0].Id, string.Empty);
        card.ExecutorSelection.PreferredCandidateId = pool.InstalledCandidates[0].Id;

        Assert.IsTrue(
            TryApplySelection(card, pool, out var applyError),
            applyError);
        Assert.IsTrue(
            ChoiceExecutorPairValidator.Validate(
                card,
                pool,
                UserWorkloadModes.Balanced,
                "Qwen3-8B-GGUF",
                CreatePassport(),
                out var validationError),
            validationError);
        Assert.AreEqual(1, card.ExecutorCandidates.Count);
    }

    [TestMethod]
    public void TryApplySelection_AppliesPreferredComponentFromTrustedCatalog()
    {
        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            CreateCatalog(),
            CreateProfile(),
            UserWorkloadModes.Balanced,
            CreatePassport(),
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());
        var provider = ComponentCatalog.FindProviders("read.text").First();
        var card = CreateCard(pool.InstalledCandidates[0].Id, pool.AlternativeCandidates[0].Id);
        card.ExecutionPlan.PreferredComponentIds = [provider.Id];

        Assert.IsTrue(
            TryApplySelection(card, pool, out var error),
            error);
        Assert.IsTrue(card.ExecutionRoute.Resolution.Bindings.Any(binding =>
            string.Equals(binding.ComponentId, provider.Id, StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void TryApplySelection_AllowsSameFamilyWhenCoreSelectsIt()
    {
        var pool = new ChoiceExecutorCandidatePool
        {
            InstalledCandidates =
            [
                new ChoiceExecutorPoolCandidate
                {
                    Id = "installed_1", Model = "Qwen/Qwen3.6-27B-GGUF", Family = "qwen",
                    Status = ChoiceExecutorCandidateStatuses.Installed, ParameterCount = 27_000_000_000
                }
            ],
            AlternativeCandidates =
            [
                new ChoiceExecutorPoolCandidate
                {
                    Id = "alternative_1", Model = "mirror/Qwen3.6-32B-GGUF", Family = "qwen",
                    Status = ChoiceExecutorCandidateStatuses.NotInstalled, ParameterCount = 32_000_000_000
                }
            ]
        };
        var card = CreateCard("installed_1", "alternative_1");

        Assert.IsTrue(TryApplySelection(card, pool, out var error), error);
        Assert.AreEqual(2, card.ExecutorCandidates.Count);
    }

    [TestMethod]
    public void PairValidator_UsesTrustedParameterMetadataWhenNameHasNoSize()
    {
        var pool = new ChoiceExecutorCandidatePool
        {
            InstalledCandidates =
            [
                new ChoiceExecutorPoolCandidate
                {
                    Id = "installed_1", Model = "local/General-27B-GGUF", Family = "general",
                    Status = ChoiceExecutorCandidateStatuses.Installed, ParameterCount = 27_000_000_000,
                    PipelineTag = "text-generation", RuntimeCompatible = true
                }
            ],
            AlternativeCandidates =
            [
                new ChoiceExecutorPoolCandidate
                {
                    Id = "alternative_1", Model = "org/research-pro", Family = "research",
                    Status = ChoiceExecutorCandidateStatuses.NotInstalled, ParameterCount = 20_000_000_000,
                    PipelineTag = "text-generation", RuntimeCompatible = true, HardwareStatus = "fit"
                }
            ]
        };
        var card = CreateCard("installed_1", "alternative_1");

        Assert.IsTrue(TryApplySelection(card, pool, out var applyError), applyError);
        Assert.IsTrue(
            ChoiceExecutorPairValidator.Validate(
                card,
                pool,
                UserWorkloadModes.Balanced,
                "Qwen3-8B-GGUF",
                CreatePassport(),
                out var validationError),
            validationError);
    }

    [TestMethod]
    public void BuildCatalogRequest_UsesCapabilitiesInsteadOfLiteralTopic()
    {
        var request = ChoiceExecutorCandidatePoolService.BuildCatalogRequest(
            CreateProfile(),
            UserWorkloadModes.Balanced);

        CollectionAssert.Contains(request.Directions, "science_professional");
        Assert.AreEqual("deep_research", request.TaskType);
        Assert.AreEqual("optimal", request.LoadLevel);
    }

    [TestMethod]
    public void CreatePool_RejectsSpecialistPipelineWithoutRegisteredCoordinatorRuntime()
    {
        var catalog = CreateCatalog();
        catalog.Candidates.Insert(0, new ModelCatalogCandidate
        {
            RepoId = "org/video-specialist-20b",
            ModelType = "video_transformer",
            PipelineTag = "video-to-video",
            ParameterCount = 20_000_000_000,
            Directions = ["video"],
            Hardware = new ModelHardwareCompatibility
            {
                Status = "fit",
                IsCompatible = true
            },
            MatchReasons = ["direction: video"]
        });

        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            catalog,
            CreateProfile(),
            UserWorkloadModes.Balanced,
            CreatePassport(),
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());

        Assert.IsFalse(pool.AlternativeCandidates.Any(candidate =>
            candidate.Model == "org/video-specialist-20b"));
        Assert.IsTrue(pool.AlternativeCandidates.All(candidate =>
            candidate.RuntimeCompatible
            && candidate.RuntimeBackend == ExecutionCompatibilityService.LlamaRuntime));
    }

    [TestMethod]
    public void CreatePool_KeepsCoordinatorChoicesAndOffersDegradedRouteForUnresolvedCapability()
    {
        var profile = CreateProfile();
        profile.Dimensions.Add(new ChoiceCapabilityDimension
        {
            Dimension = ChoiceDecisionDimensions.SpecializationNeed,
            Status = ChoiceDimensionStatuses.Resolved,
            Values = ["audio_generation"],
            Evidence = "user choice"
        });

        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            CreateCatalog(),
            profile,
            UserWorkloadModes.Balanced,
            CreatePassport(),
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());

        Assert.IsTrue(pool.HasCandidatePair);
        Assert.IsTrue(pool.IsExecutionReady);
        Assert.IsFalse(pool.ExecutionBundle.PreferredRoute.IsStartable);
        Assert.IsTrue(pool.ExecutionBundle.DegradedRoute.IsStartable);
        CollectionAssert.Contains(pool.UnresolvedCapabilities, "generate.audio");
        Assert.IsTrue(pool.InstalledCandidates.All(candidate =>
            candidate.RuntimeCompatible
            && candidate.UnresolvedCapabilities.Contains("generate.audio")));

        var card = CreateCard(
            pool.InstalledCandidates[0].Id,
            string.Empty);
        Assert.IsTrue(
            TryApplySelection(card, pool, out var applyError),
            applyError);
        Assert.IsTrue(
            ChoiceExecutorPairValidator.Validate(
                card,
                pool,
                UserWorkloadModes.Balanced,
                "Qwen3-8B-GGUF",
                CreatePassport(),
                out var validationError),
            validationError);
        CollectionAssert.Contains(
            card.ExecutorCandidates[0].UnresolvedCapabilities,
            "generate.audio");
    }

    [TestMethod]
    public void CreatePool_UsesConcreteFileFormatAndCarriesExecutionRouteIntoCard()
    {
        var profile = new ChoiceCapabilityProfile
        {
            Dimensions =
            [
                new ChoiceCapabilityDimension
                {
                    Dimension = ChoiceDecisionDimensions.InputModality,
                    Status = ChoiceDimensionStatuses.Resolved,
                    Values = ["file:image"],
                    Evidence = "unit test"
                },
                new ChoiceCapabilityDimension
                {
                    Dimension = ChoiceDecisionDimensions.TaskType,
                    Status = ChoiceDimensionStatuses.Resolved,
                    Values = ["information_interpretation"],
                    Evidence = "unit test"
                }
            ]
        };
        var manifest = new SessionFilePromptManifest
        {
            Intent = SessionFileIntentStatuses.Selected,
            FileCount = 1,
            Files =
            [
                new SessionFilePromptItem
                {
                    Name = "source.webp",
                    Extension = ".webp",
                    Category = SessionFileCategories.Image,
                    IsAvailable = true
                }
            ]
        };

        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            CreateCatalog(),
            profile,
            UserWorkloadModes.Balanced,
            CreatePassport(),
            fileManifest: manifest,
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());

        Assert.IsTrue(pool.HasCandidatePair);
        Assert.IsTrue(pool.IsExecutionReady);
        Assert.IsFalse(pool.ExecutionBundle.PreferredRoute.IsStartable);
        Assert.IsTrue(pool.ExecutionBundle.DegradedRoute.IsStartable);
        CollectionAssert.Contains(pool.RequiredCapabilities, "read.image_extended");
        CollectionAssert.Contains(pool.RequiredCapabilities, "analyze.image.semantic");
        CollectionAssert.Contains(pool.MissingCapabilities, "analyze.image.semantic");
        CollectionAssert.DoesNotContain(pool.UnresolvedCapabilities, "analyze.image.semantic");
        Assert.IsTrue(pool.ExecutionRoute.Resolution.Bindings.Any(binding =>
            binding.RequestedCapabilityId == "read.image_extended"
            && binding.AdapterAvailable
            && binding.Status != CapabilityBindingStatuses.AdapterMissing));

        var card = CreateCard(
            pool.InstalledCandidates[0].Id,
            string.Empty);
        Assert.IsTrue(
            TryApplySelection(card, pool, out var error),
            error);
        Assert.AreNotSame(pool.ExecutionRoute, card.ExecutionRoute);
        CollectionAssert.Contains(
            card.ExecutionRoute.Requirements.Select(item => item.Request.Id).ToList(),
            "read.text");
    }

    [TestMethod]
    public void TryApplySelection_ReplacesFreeFormCapabilityClaimsWithVerifiedFacts()
    {
        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            CreateCatalog(),
            CreateProfile(),
            UserWorkloadModes.Balanced,
            CreatePassport(),
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());
        var card = CreateCard(pool.InstalledCandidates[0].Id, pool.AlternativeCandidates[0].Id);
        card.ExecutorSelection.InstalledAssessment.Advantage = "Can directly edit every media format.";

        Assert.IsTrue(TryApplySelection(card, pool, out var error), error);
        Assert.AreEqual("verified_installed_coordinator", card.ExecutorCandidates[0].Advantage);
        Assert.AreEqual(
            ExecutionCompatibilityService.CoordinatorRole,
            card.ExecutorCandidates[0].Role);
    }

    [TestMethod]
    public void TryApplySelection_RejectsDownloadableCandidateWithBlockedBundle()
    {
        var profile = CreateProfile();
        profile.Dimensions.Add(new ChoiceCapabilityDimension
        {
            Dimension = ChoiceDecisionDimensions.SpecializationNeed,
            Status = ChoiceDimensionStatuses.Resolved,
            Values = ["audio_generation"],
            Evidence = "user choice"
        });
        var pool = ChoiceExecutorCandidatePoolService.CreatePool(
            CreateInventory(),
            CreateCatalog(),
            profile,
            UserWorkloadModes.Balanced,
            CreatePassport(),
            routePlanner: TestComponentManagerFactory.CreateEmptyRoutePlanner());
        var card = CreateCard(
            pool.InstalledCandidates[0].Id,
            pool.AlternativeCandidates[0].Id);
        card.ExecutionPlan.RequiredCapabilities = ["generate.audio"];

        Assert.IsFalse(
            TryApplySelection(card, pool, out var error));
        StringAssert.Contains(error, "complete execution bundle");
    }

    private static ChoiceTaskCard CreateCard(string installedId, string alternativeId) => new()
    {
        CapabilityProfile = CreateProfile(),
        ExecutionPlan = new ChoiceExecutionPlan
        {
            RequiredCapabilities = ["read.text"],
            ExecutorRole = "Research coordinator",
            Rationale = "Coordinate the verified task and use the selected execution capabilities."
        },
        ExecutorSelection = new ChoiceExecutorSelection
        {
            InstalledCandidateId = installedId,
            AlternativeCandidateId = alternativeId,
            PreferredCandidateId = string.IsNullOrWhiteSpace(alternativeId)
                ? installedId
                : alternativeId,
            InstalledAssessment = new ChoiceExecutorAssessment
            {
                Advantage = "Запускается сразу",
                Limitation = "Более общий профиль",
                Reason = "Подходит для первичного выполнения"
            },
            AlternativeAssessment = new ChoiceExecutorAssessment
            {
                Advantage = "Лучше соответствует протоколам",
                Limitation = "Требует загрузки",
                Reason = "Полезная альтернатива из другого семейства"
            }
        }
    };

    private static ChoiceCapabilityProfile CreateProfile() => new()
    {
        Dimensions =
        [
            new ChoiceCapabilityDimension
            {
                Dimension = ChoiceDecisionDimensions.TaskType,
                Status = ChoiceDimensionStatuses.Resolved,
                Values = ["deep_research"],
                Evidence = "user choice"
            },
            new ChoiceCapabilityDimension
            {
                Dimension = ChoiceDecisionDimensions.DomainSpecialization,
                Status = ChoiceDimensionStatuses.Resolved,
                Values = ["science"],
                Evidence = "user choice"
            }
        ]
    };

    private static CapabilityInventoryResponse CreateInventory() => new()
    {
        Items =
        [
            new CapabilityInventoryItem
            {
                Role = "executor",
                Name = "sm54/Qwen3.6-27B-Q4_K_M-GGUF",
                Status = "installed",
                IsInstalled = true,
                IsRunnable = true,
                Format = "gguf"
            }
        ]
    };

    private static ModelCatalogSearchResponse CreateCatalog() => new()
    {
        Candidates =
        [
            CreateCandidate("Qwen/Qwen3.6-27B", "qwen3", 27_000_000_000),
            CreateCandidate("openai/gpt-oss-20b", "gpt_oss", 20_000_000_000),
            CreateCandidate("google/gemma-4-31B-it", "gemma4", 31_000_000_000)
        ]
    };

    private static ModelCatalogCandidate CreateCandidate(string repoId, string modelType, long parameters) => new()
    {
        RepoId = repoId,
        ModelType = modelType,
        PipelineTag = "text-generation",
        ParameterCount = parameters,
        Directions = ["science_professional"],
        Roles = ["deep_research"],
        Hardware = new ModelHardwareCompatibility
        {
            Status = "fit",
            IsCompatible = true
        },
        MatchReasons = ["direction match"]
    };

    private static bool TryApplySelection(
        ChoiceTaskCard card,
        ChoiceExecutorCandidatePool pool,
        out string error) =>
        ChoiceExecutorCandidatePoolService.TryApplySelection(
            card,
            pool,
            out error,
            TestComponentManagerFactory.CreateEmptyRoutePlanner());

    private static ComputerPassport CreatePassport() => new()
    {
        RamTotalGb = 128,
        Gpus = [new GpuPassport { Name = "RTX 4090", VramGb = 24 }],
        Drives = [new DrivePassport { Name = "H", FreeGb = 500, TotalGb = 1000 }]
    };
}
