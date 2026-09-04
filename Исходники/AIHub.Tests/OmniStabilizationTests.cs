using System.IO;
using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class OmniStabilizationTests
{
    [TestMethod]
    public void ObservationWithoutWishes_IsExactTestedPrompt()
    {
        Assert.AreEqual("Опиши, что видишь на изображении.",
            ImageAnalysisOmniPromptBuilder.BuildObservationPrompt(new() { LanguageCode = "ru" }));
        Assert.AreEqual("Describe what you see in the image.",
            ImageAnalysisOmniPromptBuilder.BuildObservationPrompt(new() { LanguageCode = "en" }));
    }
    [TestMethod]
    public void DuplicateCloser_IsRecoveredWithoutChangingContentOrRawResponse()
    {
        const string raw = """
            ```json
            {"title":"Текст","paragraphs":["Кавычки: \"слово\" и скобки ] ] { внутри."]
            ],"review_items":["один"],"uncertainties":["а","б","в"]}
            ```
            """;
        var logs = new List<string>();
        var result = ImageAnalysisOmniResultParser.Parse("наблюдение", raw, [], 1, 2, logs.Add);
        Assert.AreEqual(raw, result.RawFinalResponse);
        Assert.HasCount(3, result.ReviewSummary.Uncertainties);
        StringAssert.Contains(result.Description, "Кавычки: \"слово\" и скобки ] ] { внутри.");
        Assert.HasCount(1, logs);
    }

    [TestMethod]
    [DataRow("{\"paragraphs\":[\"unfinished")]
    [DataRow("{\"paragraphs\":[\"text\"],\"review_items\":[],\"uncertainties\":[]}}")]
    [DataRow("{\"paragraphs\":[\"text\"]]],\"review_items\":[],\"uncertainties\":[]}")]
    [DataRow("{\"paragraphs\":[\"text\"],\"review_items\":[],\"uncertainties\":[]} {}")]
    public void AmbiguousOrIncompleteResponse_IsNotRepaired(string raw)
    {
        Assert.ThrowsExactly<InvalidDataException>(() => ImageAnalysisOmniResultParser.Parse("visual", raw, [], 0, 0));
    }

    [TestMethod]
    public void LengthAndFormAreInstructions_NotAcceptanceGates()
    {
        foreach (var language in new[] { "ru", "en" })
        foreach (var (length, expected) in new[] { ("brief", "1–2"), ("standard", "3–5"), ("detailed", "7–10") })
        {
            var prompt = ImageAnalysisOmniPromptBuilder.BuildComposePrompt(new()
            { LanguageCode = language, Length = length, Form = ImageAnalysisTextForms.WithTitle });
            StringAssert.Contains(prompt, expected);
            Assert.IsFalse(prompt.Contains("{{", StringComparison.Ordinal));
        }
        var accepted = ImageAnalysisOmniResultParser.Parse("visual",
            """{"title":null,"paragraphs":["one paragraph"],"review_items":[],"uncertainties":[]} """, [], 0, 0);
        Assert.AreEqual("one paragraph", accepted.Description);
    }

    [TestMethod]
    public void ExportPromptsForOptInModelReplay()
    {
        var directory = Environment.GetEnvironmentVariable("AIHUB_OMNI_FIXTURES_OUT");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        foreach (var language in new[] { "ru", "en" })
        {
            var settings = new ImageAnalysisLiterarySettings
            { LanguageCode = language, Accuracy = "strict", Style = "atmospheric", Length = "brief", Form = "with_title" };
            File.WriteAllText(Path.Combine(directory, $"prompts_{language}.json"), JsonSerializer.Serialize(new
            {
                observe = ImageAnalysisOmniPromptBuilder.BuildObservationPrompt(settings),
                compose = ImageAnalysisOmniPromptBuilder.BuildComposePrompt(settings), settings
            }));
        }
        var detailed = new ImageAnalysisLiterarySettings
        { LanguageCode = "ru", Accuracy = "free", Style = "fairy_tale", Length = "detailed", Form = "with_title" };
        File.WriteAllText(Path.Combine(directory, "prompts_detailed.json"), JsonSerializer.Serialize(new
        {
            observe = ImageAnalysisOmniPromptBuilder.BuildObservationPrompt(detailed),
            compose = ImageAnalysisOmniPromptBuilder.BuildComposePrompt(detailed), settings = detailed
        }));
    }

    [TestMethod]
    public void RawResponses_AreAppendOnlyAndDoNotReplaceSessionVersions()
    {
        var root = Directory.CreateTempSubdirectory("AIHubOmniResponseTest_");
        try
        {
            var storage = new StorageSettings
            { Results = new StorageCategorySettings { Locations = [new StorageLocationSettings { Path = root.FullName }] } };
            var session = new ImageAnalysisLiterarySession
            { Versions = [new ImageAnalysisLiteraryVersion { Text = "Previously accepted text" }] };
            var store = new ImageAnalysisSessionStore();
            const string invalidResponse = "{\"success\":true,\"content\":\"broken JSON ] ]\"}";
            var first = store.SaveOmniResponse(session, storage, "compose", [], invalidResponse);
            var second = store.SaveOmniResponse(session, storage, "compose", [], "next response");
            Assert.AreNotEqual(first, second);
            using var json = JsonDocument.Parse(File.ReadAllText(first));
            Assert.AreEqual(invalidResponse, json.RootElement.GetProperty("rawProtocol").GetString());
            Assert.AreEqual("Previously accepted text", session.Versions[0].Text);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                store.SaveOmniResponse(session, storage, "../outside", [], invalidResponse));
        }
        finally { root.Delete(recursive: true); }
    }

    [TestMethod]
    public void ParseOptInRealWorkerReplays()
    {
        var directory = Environment.GetEnvironmentVariable("AIHUB_OMNI_REPLAY_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        var count = 0;
        var checks = new List<object>();
        foreach (var path in Directory.EnumerateFiles(directory, "protocol.jsonl", SearchOption.AllDirectories))
        {
            var visual = "";
            var command = "";
            foreach (var line in File.ReadLines(path))
            {
                using var row = JsonDocument.Parse(line);
                var root = row.RootElement;
                if (!root.TryGetProperty("direction", out var direction)) continue;
                if (direction.GetString() == "request")
                    command = root.GetProperty("payload").GetProperty("command").GetString();
                else if (root.TryGetProperty("value", out var value)
                         && !value.TryGetProperty("event", out _)
                         && value.TryGetProperty("content", out var content))
                {
                    if (command == "analyze") visual = content.GetString()!;
                    if (command == "compose")
                    {
                        var repairs = new List<string>();
                        var result = ImageAnalysisOmniResultParser.Parse(visual, content.GetString()!, [], 0, 0, repairs.Add);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Description), path);
                        checks.Add(new { file = Path.GetRelativePath(directory, path), id = value.GetProperty("id").GetInt32(),
                            accepted = true, repairs, reviewItems = result.ReviewSummary.Items.Count,
                            uncertainties = result.ReviewSummary.Uncertainties.Count });
                        count++;
                    }
                }
            }
        }
        Assert.IsGreaterThan(0, count);
        File.WriteAllText(Path.Combine(directory, "csharp_parser_validation.json"), JsonSerializer.Serialize(checks));
    }
}
