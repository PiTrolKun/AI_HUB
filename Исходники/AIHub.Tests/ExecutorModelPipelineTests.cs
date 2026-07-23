using System.Net;
using System.Security.Cryptography;
using AIHub.Models;
using AIHub.Services;

namespace AIHub.Tests;

[TestClass]
public sealed class ExecutorModelPipelineTests
{
    [TestMethod]
    public async Task Resolver_PrefersStandaloneQ4KmArtifact()
    {
        var root = Path.Combine(Path.GetTempPath(), "aihub-resolver-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = new FakeArtifactSource
        {
            Direct = new HuggingFaceModelCandidate
            {
                RepoId = "example/model-gguf",
                License = "apache-2.0",
                Files =
                [
                    CreateFile("model-Q8_0.gguf", 20),
                    CreateFile("model-Q4_K_M.gguf", 10),
                    CreateFile("model-Q4_K_M-00001-of-00002.gguf", 5)
                ]
            }
        };

        try
        {
            var artifact = await new ExecutorModelArtifactResolver(source).ResolveAsync(
                "example/model-gguf",
                CreateStorage(root),
                CancellationToken.None);

            Assert.AreEqual("model-Q4_K_M.gguf", artifact.FileName);
            Assert.AreEqual("Q4_K_M", artifact.Quantization);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task Installer_RequiresRuntimeValidationBeforeRegisteringModel()
    {
        var payload = CreateMinimalGguf("llama");
        var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        var root = Path.Combine(Path.GetTempPath(), "aihub-executor-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient(new StaticPayloadHandler(payload));
            using var installer = new ExecutorModelInstaller(client);
            var artifact = new ExecutorModelArtifact
            {
                RequestedModel = "example/model",
                RepoId = "example/model-gguf",
                FileName = "model-Q4_K_M.gguf",
                DownloadUrl = "https://unit.test/model.gguf",
                SizeBytes = payload.Length,
                Sha256 = hash,
                Quantization = "Q4_K_M"
            };

            var storage = CreateStorage(root);
            var downloaded = await installer.InstallAsync(
                artifact,
                storage,
                new Progress<ExecutorDownloadProgress>(),
                CancellationToken.None);

            Assert.IsFalse(downloaded.IsInstalled);
            Assert.AreEqual("llama", downloaded.Architecture);
            Assert.IsTrue(File.Exists(downloaded.InstalledPath));
            var manifestPath = Path.Combine(Path.GetDirectoryName(downloaded.InstalledPath)!, "executor-model.json");
            StringAssert.Contains(File.ReadAllText(manifestPath), "\"status\": \"downloaded_verified\"");
            Assert.IsFalse(File.Exists(downloaded.InstalledPath + ".part"));
            Assert.IsFalse(new DebugModelDiscoveryService().Discover(storage)
                .Any(model => model.Role == "executor" && model.Path == downloaded.InstalledPath));

            var installed = installer.MarkRuntimeVerified(downloaded);
            Assert.IsTrue(installed.IsInstalled);
            Assert.IsTrue(new DebugModelDiscoveryService().Discover(storage)
                .Any(model => model.Role == "executor" && model.Path == installed.InstalledPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public void ContextBudget_CompactsAtEightyPercentOfWorkingArea()
    {
        var below = ExecutorContextBudgetManager.Measure([new string('a', 2000)], 1000, 200);
        var above = ExecutorContextBudgetManager.Measure([new string('a', 2200)], 1000, 200);

        Assert.IsFalse(below.ShouldCompact);
        Assert.IsTrue(above.ShouldCompact);
    }

    [TestMethod]
    public void ResultParser_ReadsBoundedClarificationOptions()
    {
        const string response = "```json\n{\"status\":\"needs_clarification\",\"question\":\"Какой период?\",\"options\":[\"Месяц\",\"Год\",\"Все время\"]}\n```";

        var parsed = ExecutorResultParser.TryReadTurn(response, out var clarification);

        Assert.IsTrue(parsed);
        Assert.AreEqual("Какой период?", clarification.Question);
        Assert.AreEqual(3, clarification.Options.Count);
    }

    [TestMethod]
    public void ResultParser_RejectsProseQuestionAsFinalResult()
    {
        const string response = "Уточните, пожалуйста, предметную область и конкретную тему?";

        var parsed = ExecutorResultParser.TryReadTurn(response, out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void ResultParser_ReadsPracticalQuestionWithCurrentResultSummary()
    {
        const string response = """
            {
              "status":"working",
              "action":"ask_user",
              "stageSummary":"Требования подтверждены",
              "thought":"Проверяю важное ограничение.",
              "question":"Какой тон ответа предпочтителен?",
              "options":["Деловой","Нейтральный","Реши самостоятельно"],
              "allowCustom":true,
              "currentResultSummary":"Сейчас доступен нейтральный черновик рекомендации с двумя основными вариантами.",
              "requestedTools":[],
              "missingCriticalInputs":[],
              "assumptions":[],
              "result":"",
              "sources":[],
              "warnings":[]
            }
            """;

        var parsed = ExecutorResultParser.TryReadTurn(response, out var turn);

        Assert.IsTrue(parsed);
        Assert.AreEqual(ExecutorTurnActions.AskUser, turn.Action);
        StringAssert.Contains(turn.CurrentResultSummary, "нейтральный черновик");
        Assert.IsTrue(turn.AllowCustom);
    }

    [TestMethod]
    public void ResultParser_RejectsTerminalResultStatus()
    {
        const string response = "{\"status\":\"result_ready\",\"action\":\"present_result\",\"stageSummary\":\"Требования подтверждены\",\"thought\":\"\",\"question\":\"\",\"options\":[],\"allowCustom\":true,\"currentResultSummary\":\"\",\"requestedTools\":[],\"missingCriticalInputs\":[],\"assumptions\":[],\"result\":\"Готовый результат\",\"sources\":[],\"warnings\":[]}";

        var parsed = ExecutorResultParser.TryReadTurn(response, out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void ResultParser_StageReadyKeepsCustomResponseAvailable()
    {
        const string response = "{\"status\":\"stage_ready\",\"stageSummary\":\"Цель и аудитория подтверждены\",\"thought\":\"Этап достаточно проработан\",\"question\":\"Можно продолжить уточнение или перейти дальше.\",\"options\":[],\"allowCustom\":false,\"result\":\"\",\"sources\":[],\"warnings\":[]}";

        var parsed = ExecutorResultParser.TryReadTurn(response, out var turn);

        Assert.IsTrue(parsed);
        Assert.AreEqual(ExecutorTurnStatuses.StageReady, turn.Status);
        Assert.IsTrue(turn.AllowCustom);
    }

    [TestMethod]
    public void ResultParser_StageReadyUsesBriefConfirmationAndDropsTransitionOptions()
    {
        const string response = """
            {
              "status":"stage_ready",
              "action":"confirm_brief",
              "stageId":"task_definition",
              "stageSummary":"Нужно подготовить сравнение двух вариантов для владельца проекта.",
              "thought":"Стартовые данные собраны.",
              "question":"",
              "options":["Да, перейти дальше"],
              "allowCustom":false,
              "requestedTools":[],
              "missingCriticalInputs":[],
              "assumptions":[],
              "result":"",
              "sources":[],
              "warnings":[]
            }
            """;

        var parsed = ExecutorResultParser.TryReadTurn(response, out var turn);

        Assert.IsTrue(parsed);
        Assert.AreEqual(ExecutorTurnActions.ConfirmBrief, turn.Action);
        Assert.AreEqual(0, turn.Options.Count);
        Assert.IsTrue(turn.AllowCustom);
    }

    [TestMethod]
    public void ResultParser_RejectsAutonomousContinueAction()
    {
        const string response = """
            {
              "status":"working",
              "action":"continue_work",
              "stageId":"practical_clarification",
              "stageSummary":"Выбран сравнительный подход.",
              "thought":"Проверяю ограничения выбранного метода.",
              "question":"",
              "options":[],
              "allowCustom":false,
              "currentResultSummary":"Доступно предварительное сравнение.",
              "requestedTools":[],
              "missingCriticalInputs":[],
              "assumptions":["Сравнение должно быть нейтральным"],
              "result":"",
              "sources":[],
              "warnings":[]
            }
            """;

        var parsed = ExecutorResultParser.TryReadTurn(response, out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void ResultSummaryPolicy_ClampsOversizedPreview()
    {
        var value = string.Join(' ', Enumerable.Repeat("результат", 200));

        var clamped = ExecutorResultSummaryPolicy.Clamp(value);

        Assert.IsTrue(clamped.Length <= ExecutorResultSummaryPolicy.MaximumCharacters);
        StringAssert.EndsWith(clamped, "…");
    }

    [TestMethod]
    public void ExecutorContract_ContainsPreviewAndNoAutonomousResultActions()
    {
        var schema = ExecutorJsonContract.CreateResponseFormat().ToJsonString();

        StringAssert.Contains(schema, "currentResultSummary");
        Assert.IsFalse(schema.Contains("continue_work", StringComparison.Ordinal));
        Assert.IsFalse(schema.Contains("present_result", StringComparison.Ordinal));
        Assert.IsFalse(schema.Contains("result_ready", StringComparison.Ordinal));
    }

    [TestMethod]
    public void HandoffConsistency_OfflineRequirementRemovesWebAccess()
    {
        var handoff = new ExecutorHandoffPackage
        {
            NeedsWeb = true,
            RequiredTools = ["web_research", "local_notes"],
            UserSignals =
            [
                new ExecutorHandoffItem
                {
                    Name = ChoiceDecisionDimensions.ToolRequirements,
                    Value = "Без интернета",
                    Source = "user_selection",
                    IsAuthoritative = true
                }
            ]
        };

        var changed = ExecutorHandoffConsistencyPolicy.Normalize(handoff);

        Assert.IsTrue(changed);
        Assert.IsFalse(handoff.NeedsWeb);
        CollectionAssert.AreEqual(new[] { "local_notes" }, handoff.RequiredTools);
    }

    [TestMethod]
    public void StageFlow_HasOnlyTechnicalAndPracticalStages()
    {
        Assert.AreEqual(2, ExecutorStageFlow.ActiveStageIds.Count);
        Assert.AreEqual(
            ExecutorStageIds.PracticalClarification,
            ExecutorStageFlow.GetNext(ExecutorStageIds.TaskDefinition));
        Assert.IsTrue(ExecutorStageFlow.AreAdjacent(
            ExecutorStageIds.TaskDefinition,
            ExecutorStageIds.PracticalClarification));
        Assert.IsNull(ExecutorStageFlow.GetNext(ExecutorStageIds.PracticalClarification));
    }

    [TestMethod]
    public async Task Installer_HashMismatchDoesNotRegisterModelAsInstalled()
    {
        var payload = CreateMinimalGguf("llama");
        var root = Path.Combine(Path.GetTempPath(), "aihub-executor-hash-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient(new StaticPayloadHandler(payload));
            using var installer = new ExecutorModelInstaller(client);
            var artifact = new ExecutorModelArtifact
            {
                RequestedModel = "example/model",
                RepoId = "example/model-gguf",
                FileName = "model-Q4_K_M.gguf",
                DownloadUrl = "https://unit.test/model.gguf",
                SizeBytes = payload.Length,
                Sha256 = new string('0', 64),
                Quantization = "Q4_K_M"
            };

            await Assert.ThrowsExactlyAsync<InvalidDataException>(() => installer.InstallAsync(
                artifact,
                CreateStorage(root),
                new Progress<ExecutorDownloadProgress>(),
                CancellationToken.None));

            Assert.IsFalse(Directory.EnumerateFiles(root, "*.gguf", SearchOption.AllDirectories).Any());
            var manifest = File.ReadAllText(Directory.EnumerateFiles(root, "executor-model.json", SearchOption.AllDirectories).Single());
            StringAssert.Contains(manifest, "\"status\": \"invalid\"");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task Resolver_RejectsAuxiliaryGemmaFilesAndRanksAllRepositories()
    {
        var root = Path.Combine(Path.GetTempPath(), "aihub-resolver-role-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = new FakeArtifactSource
        {
            Direct = new HuggingFaceModelCandidate
            {
                RepoId = "bartowski/google_gemma-4-31B-it-GGUF",
                Files =
                [
                    CreateFile("mtp-google_gemma-4-31B-it-Q8_0.gguf", 515_000_000),
                    CreateFile("google_gemma-4-31B-it-imatrix.gguf", 14_000_000)
                ]
            },
            Search =
            [
                new HuggingFaceModelCandidate
                {
                    RepoId = "DevQuasar/google.gemma-4-31B-it-GGUF",
                    Files = [CreateFile("google.gemma-4-31B-it-Q4_K_M.gguf", 18_687_000_000)]
                }
            ]
        };

        try
        {
            var artifact = await new ExecutorModelArtifactResolver(source).ResolveAsync(
                "google/gemma-4-31B-it",
                CreateStorage(root),
                CancellationToken.None);

            Assert.AreEqual("DevQuasar/google.gemma-4-31B-it-GGUF", artifact.RepoId);
            Assert.AreEqual("google.gemma-4-31B-it-Q4_K_M.gguf", artifact.FileName);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task Installer_RejectsAuxiliaryArchitectureBeforeDownloadRegistration()
    {
        var payload = CreateMinimalGguf("gemma4-assistant");
        var root = Path.Combine(Path.GetTempPath(), "aihub-executor-architecture-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var client = new HttpClient(new StaticPayloadHandler(payload));
            using var installer = new ExecutorModelInstaller(client);
            var artifact = new ExecutorModelArtifact
            {
                RequestedModel = "google/gemma-4-31B-it",
                RepoId = "example/gemma-4-31B-it-GGUF",
                FileName = "gemma-4-31B-it-Q4_K_M.gguf",
                DownloadUrl = "https://unit.test/model.gguf",
                SizeBytes = payload.Length,
                Sha256 = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(),
                Quantization = "Q4_K_M"
            };

            await Assert.ThrowsExactlyAsync<InvalidDataException>(() => installer.InstallAsync(
                artifact,
                CreateStorage(root),
                new Progress<ExecutorDownloadProgress>(),
                CancellationToken.None));

            Assert.IsFalse(Directory.EnumerateFiles(root, "*.gguf", SearchOption.AllDirectories).Any());
            Assert.IsFalse(Directory.EnumerateFiles(root, "executor-model.json", SearchOption.AllDirectories).Any());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static HuggingFaceModelFile CreateFile(string name, long size) =>
        new()
        {
            FileName = name,
            SizeBytes = size,
            DownloadUrl = "https://unit.test/" + name,
            LfsOid = new string('a', 64)
        };

    private static StorageSettings CreateStorage(string root) =>
        new()
        {
            Models = new StorageCategorySettings
            {
                Locations = [new StorageLocationSettings { Path = root }]
            }
        };

    private static byte[] CreateMinimalGguf(string architecture)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(0x46554747u);
        writer.Write(3u);
        writer.Write(0ul);
        writer.Write(1ul);
        WriteGgufString(writer, "general.architecture");
        writer.Write(8u);
        WriteGgufString(writer, architecture);
        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteGgufString(BinaryWriter writer, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.Write((ulong)bytes.Length);
        writer.Write(bytes);
    }

    private sealed class FakeArtifactSource : IExecutorArtifactSource
    {
        public HuggingFaceModelCandidate Direct { get; set; } = new();
        public IReadOnlyList<HuggingFaceModelCandidate> Search { get; set; } = [];

        public Task<HuggingFaceModelCandidate> GetFilesAsync(
            string repoId,
            StorageSettings storageSettings,
            CancellationToken cancellationToken) => Task.FromResult(Direct);

        public Task<IReadOnlyList<HuggingFaceModelCandidate>> SearchGgufAsync(
            string requestedModel,
            StorageSettings storageSettings,
            CancellationToken cancellationToken) => Task.FromResult(Search);
    }

    private sealed class StaticPayloadHandler(byte[] payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            });
    }
}
