using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class SemanticImageToolServiceTests
{
    [TestMethod]
    public void BuildArguments_ConnectsModelAndMultimodalProjector()
    {
        var arguments = SemanticImageToolService.BuildArguments(
            @"C:\models\vision.gguf",
            @"C:\models\projector.gguf",
            54321,
            99);

        CollectionAssert.Contains(arguments.ToList(), "--mmproj");
        CollectionAssert.Contains(arguments.ToList(), @"C:\models\projector.gguf");
        CollectionAssert.Contains(arguments.ToList(), @"C:\models\vision.gguf");
        CollectionAssert.Contains(arguments.ToList(), "54321");
    }

    [TestMethod]
    public void BuildRequestBody_ContainsImageAndGroundedUserInstruction()
    {
        const string dataUri = "data:image/png;base64,AQID";
        const string prompt = "Describe only what is visible.";

        var json = SemanticImageToolService.BuildRequestBody(dataUri, prompt, "ru");
        using var document = JsonDocument.Parse(json);
        var messages = document.RootElement.GetProperty("messages");
        var content = messages[1].GetProperty("content");

        Assert.AreEqual(prompt, content[0].GetProperty("text").GetString());
        Assert.AreEqual(
            dataUri,
            content[1].GetProperty("image_url").GetProperty("url").GetString());
        StringAssert.Contains(messages[0].GetProperty("content").GetString(), "Never invent");
        StringAssert.Contains(messages[0].GetProperty("content").GetString(), "Russian (ru)");
    }

    [TestMethod]
    public async Task VisionImagePayload_WebPIsNormalizedToPngInMemory()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "pixel.webp");
            File.WriteAllBytes(
                path,
                Convert.FromBase64String(
                    "UklGRhwAAABXRUJQVlA4TA8AAAAvAUAAAAcQ/Y/+ByKi/wEA"));
            var payload = await new VisionImagePayloadService().PrepareAsync(
                CreateImageReference(path, ".webp"),
                CancellationToken.None);

            Assert.IsTrue(payload.WasNormalized);
            Assert.AreEqual("image/png", payload.MimeType);
            CollectionAssert.AreEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47 },
                payload.Bytes.Take(4).ToArray());
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public async Task VisionImagePayload_PngIsPassedThroughWithoutReencoding()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "pixel.png");
            var expected = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9WlS8AAAAASUVORK5CYII=");
            File.WriteAllBytes(path, expected);

            var payload = await new VisionImagePayloadService().PrepareAsync(
                CreateImageReference(path, ".png"),
                CancellationToken.None);

            Assert.IsFalse(payload.WasNormalized);
            Assert.AreEqual("image/png", payload.MimeType);
            CollectionAssert.AreEqual(expected, payload.Bytes);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [TestMethod]
    public void VisionRuntimeDiagnostics_RedactsImagePayloadAndKeepsHttpFailure()
    {
        var buffer = new VisionRuntimeDiagnosticBuffer();
        buffer.Add("stderr", "decode failed for data:image/webp;base64,AQIDBA==");

        var summary = VisionRuntimeDiagnosticBuffer.CreateAttemptSummary(
            0,
            System.Net.HttpStatusCode.InternalServerError,
            "failed data:image/webp;base64,AQIDBA==",
            buffer.CreateExcerpt(),
            new HttpRequestException("response failed"));

        StringAssert.Contains(summary, "mode=cpu");
        StringAssert.Contains(summary, "http=500");
        StringAssert.Contains(summary, "[redacted]");
        Assert.IsFalse(summary.Contains("AQIDBA", StringComparison.Ordinal));
    }

    [TestMethod]
    public void InferenceFailure_CombinesGpuAndCpuAttemptsIntoSafeToolError()
    {
        var gpuDiagnostics = new VisionRuntimeDiagnosticBuffer();
        gpuDiagnostics.Add("stderr", "gpu decode failed");
        var cpuDiagnostics = new VisionRuntimeDiagnosticBuffer();
        cpuDiagnostics.Add("stderr", "cpu decode failed");

        var failure = SemanticImageToolService.CreateInferenceFailure(
        [
            new VisionRuntimeAttemptException(
                99,
                System.Net.HttpStatusCode.InternalServerError,
                "gpu response",
                gpuDiagnostics),
            new VisionRuntimeAttemptException(
                0,
                System.Net.HttpStatusCode.BadRequest,
                "cpu response",
                cpuDiagnostics)
        ]);

        Assert.AreEqual("semantic_vision_failed", failure.Code);
        StringAssert.Contains(failure.DiagnosticMessage, "mode=gpu");
        StringAssert.Contains(failure.DiagnosticMessage, "mode=cpu");
        StringAssert.Contains(failure.DiagnosticMessage, "http=500");
        StringAssert.Contains(failure.DiagnosticMessage, "http=400");
    }

    private static SessionFileReference CreateImageReference(string path, string extension) =>
        new()
        {
            Id = "image-1",
            SourcePath = path,
            DisplayName = Path.GetFileName(path),
            Extension = extension,
            Category = SessionFileCategories.Image,
            IsAvailable = true,
            SizeBytes = new FileInfo(path).Length
        };

    private static string CreateRoot() =>
        Path.Combine(
            Path.GetTempPath(),
            "AIHubSemanticVisionTests",
            Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
