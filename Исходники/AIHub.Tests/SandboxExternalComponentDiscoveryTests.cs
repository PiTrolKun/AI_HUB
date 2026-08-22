using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class SandboxExternalComponentDiscoveryTests
{
    [TestMethod]
    public void BuildQuery_UsesHumanOutcomeInsteadOfInternalCapabilityOnly()
    {
        var binding = new CapabilityAdapterBinding
        {
            CapabilityId = "analyze.image.semantic",
            Purpose = "Understand the objects and scene in the supplied image."
        };
        var action = new ExecutionOutcomeAction
        {
            Purpose = "Describe what is actually visible in the image."
        };

        var query = SandboxExternalComponentDiscoveryService.BuildQuery(
            binding,
            SandboxExternalComponentDiscoveryService.DescribeCapability(binding.CapabilityId),
            action,
            null,
            "Prepare a factual description of the supplied photo.");

        StringAssert.Contains(query, "multimodal vision image captioning");
        StringAssert.Contains(query, "Describe what is actually visible");
        StringAssert.Contains(query, "Prepare a factual description");
        Assert.IsFalse(query.Contains("analyze.image.semantic", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RankCandidates_PrefersRelevantSpecialistFromTrustedHost()
    {
        var relevant = new WebSearchResult
        {
            OriginalRank = 2,
            Title = "Image captioning and visual question answering model",
            Url = "https://huggingface.co/example/vision-caption-model",
            Snippet = "Multimodal vision model for object and scene understanding."
        };
        var unrelated = new WebSearchResult
        {
            OriginalRank = 1,
            Title = "Fast local database runtime",
            Url = "https://example.com/database",
            Snippet = "Stores structured records on Windows."
        };

        var ranked = SandboxExternalComponentDiscoveryService.RankCandidates(
            [unrelated, relevant],
            "multimodal vision image captioning object scene understanding model",
            "Describe visible objects and the scene.",
            "Analyze a supplied photo.");

        Assert.AreSame(relevant, ranked[0].Result);
        Assert.IsTrue(ranked[0].Score > ranked[1].Score);
    }

    [TestMethod]
    public void Report_CoversSearchedCapabilitiesAndFindsBestCandidate()
    {
        var weak = new ExternalComponentDiscoveryCandidate
        {
            Title = "Generic runtime",
            Url = "https://example.com/generic",
            RelevanceScore = 3
        };
        var best = new ExternalComponentDiscoveryCandidate
        {
            Title = "Specialist vision model",
            Url = "https://huggingface.co/example/vision",
            RelevanceScore = 12
        };
        var report = new ExternalComponentDiscoveryReport
        {
            Searches =
            [
                new ExternalComponentDiscoverySearch
                {
                    CapabilityId = "analyze.image.semantic",
                    Candidates = [weak, best]
                },
                new ExternalComponentDiscoverySearch
                {
                    CapabilityId = "extract.image_ocr"
                }
            ]
        };

        Assert.IsTrue(report.CoversCapabilities(
            ["analyze.image.semantic", "extract.image_ocr"]));
        Assert.IsFalse(report.CoversCapabilities(
            ["analyze.image.semantic", "edit.image"]));
        Assert.AreSame(best, report.FindBestCandidate());
        Assert.AreEqual(2, report.CandidateCount);
    }

    [TestMethod]
    public void ReportPromptText_StatesThatCandidatesAreNotCallable()
    {
        var report = new ExternalComponentDiscoveryReport
        {
            Searches =
            [
                new ExternalComponentDiscoverySearch
                {
                    CapabilityId = "analyze.audio.semantic",
                    Query = "audio understanding model",
                    Provider = "test",
                    Candidates =
                    [
                        new ExternalComponentDiscoveryCandidate
                        {
                            Title = "Audio model",
                            Url = "https://huggingface.co/example/audio",
                            RelevanceScore = 10
                        }
                    ]
                }
            ]
        };

        var text = report.ToPromptText();

        StringAssert.Contains(text, "completed_unverified");
        StringAssert.Contains(text, "not installed, approved or callable");
        StringAssert.Contains(text, "analyze.audio.semantic");
    }

    [TestMethod]
    public void BenchmarkRepository_IsReferenceOnlyAndCannotBecomeInstallableCandidate()
    {
        var benchmark = new WebSearchResult
        {
            Title = "Multimodal Image Captioning Benchmark",
            Url = "https://github.com/example/multimodal-image-captioning-benchmark",
            Snippet = "A course project comparing image captioning models on a public dataset."
        };

        Assert.AreEqual(
            ExternalComponentCandidateKinds.InformationalReference,
            SandboxExternalComponentDiscoveryService.ClassifyCandidateKind(benchmark));
        Assert.AreEqual(
            ExternalComponentAcquisitionStatuses.ReferenceOnly,
            SandboxExternalComponentDiscoveryService.ClassifyAcquisitionStatus(benchmark));

        var report = new ExternalComponentDiscoveryReport
        {
            Searches =
            [
                new ExternalComponentDiscoverySearch
                {
                    CapabilityId = "analyze.image.semantic",
                    Candidates =
                    [
                        new ExternalComponentDiscoveryCandidate
                        {
                            Title = benchmark.Title,
                            Url = benchmark.Url,
                            CandidateKind = ExternalComponentCandidateKinds.InformationalReference,
                            AcquisitionStatus = ExternalComponentAcquisitionStatuses.ReferenceOnly,
                            RelevanceScore = 100
                        }
                    ]
                }
            ]
        };

        Assert.IsNull(report.FindBestInstallableCandidate());
    }
}
