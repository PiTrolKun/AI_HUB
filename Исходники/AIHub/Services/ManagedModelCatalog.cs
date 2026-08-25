using AIHub.Models;
using System.IO;

namespace AIHub.Services;

public static class ManagedModelCatalog
{
    public const string CoreArtifactId = "model-qwen3-8b-core-q4km";
    public const string KimiLegacyArtifactId = "model-kimi-vl-a3b-thinking-2506-q4km";
    public const string KimiMediumArtifactId = "model-kimi-vl-a3b-thinking-2506-chatllm-q4_1";
    public const string FlorenceLargeArtifactId = "model-florence-2-large-ft";

    public const string KimiRepository = "judd2024/chatllm_quantized_kimi-vl";
    public const string KimiRevision = "master";
    public const string FlorenceRepository = "microsoft/Florence-2-large-ft";
    public const string FlorenceRevision = "4a12a2b54b7016a48a22037fbd62da90cd566f2a";

    public static IReadOnlyList<ManagedModelArtifactCard> CreatePredefined(StorageSettings settings)
    {
        var modelsRoot = settings.Models.Locations
            .Select(location => location.Path?.Trim())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path))
            ?? string.Empty;
        return
        [
            CreateCore(modelsRoot),
            CreateKimiMedium(modelsRoot),
            CreateFlorenceLarge(modelsRoot)
        ];
    }

    public static ManagedModelArtifactCard CreateCore(string modelsRoot) => new()
    {
        ModelArtifactId = CoreArtifactId,
        Family = "Qwen3-8B",
        DisplayName = CoreModelManager.CoreModelDisplayName,
        Role = ManagedModelRoles.Core,
        Provider = "Hugging Face",
        RepositoryId = "Qwen/Qwen3-8B-GGUF",
        Revision = "7c41481f57cb95916b40956ab2f0b139b296d974",
        Format = "GGUF",
        Architecture = "qwen3",
        Quantization = "Q4_K_M",
        ParameterCount = 8_000_000_000,
        License = "Apache-2.0",
        SourcePage = "https://huggingface.co/Qwen/Qwen3-8B-GGUF",
        IsManaged = true,
        IsSystem = true,
        IsPinned = false,
        CanRemoveFiles = true,
        ModelsRoot = modelsRoot,
        InstallDirectory = CombineIfRoot(modelsRoot, "Core", "Qwen3-8B"),
        Origin = ManagedModelOrigins.ExistingManifest,
        RuntimeBackend = LlamaBackendPaths.DisplayName,
        Consumers =
        [
            Consumer("ai-hub-core", "Ядро AI HUB", "system"),
            Consumer("image-analysis-medium", "Анализ изображений — Средний", "scenario_bundle")
        ],
        Files =
        [
            File(
                CoreModelManager.CoreModelFileName,
                "https://huggingface.co/Qwen/Qwen3-8B-GGUF/resolve/7c41481f57cb95916b40956ab2f0b139b296d974/Qwen3-8B-Q4_K_M.gguf",
                CoreModelManager.CoreModelTotalBytes,
                "d98cdcbd03e17ce47681435b5150e34c1417f50b5c0019dd560e4882c5745785",
                "main_model")
        ]
    };

    public static ManagedModelArtifactCard CreateKimiMedium(string modelsRoot) => new()
    {
        ModelArtifactId = KimiMediumArtifactId,
        Family = "Kimi-VL-A3B-Thinking-2506",
        DisplayName = "Kimi-VL-A3B-Thinking-2506 GGMM Q4_1",
        Role = ManagedModelRoles.Vision,
        Provider = "ModelScope",
        RepositoryId = KimiRepository,
        Revision = KimiRevision,
        Format = "GGMM",
        Architecture = "kimi-vl",
        Quantization = "Q4_1",
        License = "MIT (upstream Moonshot AI model)",
        SourcePage = $"https://modelscope.cn/models/{KimiRepository}",
        IsManaged = true,
        CanRemoveFiles = true,
        ModelsRoot = modelsRoot,
        InstallDirectory = CombineIfRoot(modelsRoot, "Vision", "Kimi-VL-A3B-Thinking-2506", "chatllm-v24-q4_1"),
        Origin = ManagedModelOrigins.PredefinedScenario,
        RuntimeBackend = ChatLlmBackendPaths.DisplayName,
        Consumers =
        [
            Consumer("image-analysis-medium", "Анализ изображений — Средний", "scenario_bundle")
        ],
        Files =
        [
            File(
                "kimi-vl-thinking-2506-q4_1.bin",
                ResolveModelScope(KimiRepository, "kimi-vl-thinking-2506-q4_1.bin"),
                10_447_149_104,
                "33700ea2f4c8467fbcc4efa060c763e035a8e73003424634125b5a3c64ce02c9",
                "main_model")
        ]
    };

    public static ManagedModelArtifactCard CreateFlorenceLarge(string modelsRoot) => new()
    {
        ModelArtifactId = FlorenceLargeArtifactId,
        Family = "Florence-2-large-ft",
        DisplayName = "Florence-2-large-ft",
        Role = ManagedModelRoles.Localizer,
        Provider = "Hugging Face",
        RepositoryId = FlorenceRepository,
        Revision = FlorenceRevision,
        Format = "Safetensors",
        Architecture = "florence2",
        ParameterCount = 770_000_000,
        License = "MIT",
        SourcePage = $"https://huggingface.co/{FlorenceRepository}",
        IsManaged = true,
        CanRemoveFiles = true,
        ModelsRoot = modelsRoot,
        InstallDirectory = CombineIfRoot(modelsRoot, "Shared", "Florence-2-large-ft", FlorenceRevision[..12]),
        Origin = ManagedModelOrigins.PredefinedScenario,
        RuntimeBackend = "Python Transformers (local-only, pinned code)",
        Consumers =
        [
            Consumer("image-analysis-medium", "Анализ изображений — Средний", "scenario_bundle"),
            Consumer("image-analysis-heavy", "Анализ изображений — Тяжёлый", "future_bundle")
        ],
        Files =
        [
            FlorenceFile("config.json", 2_445, "fa081841369aa9c6e42faf5c52368d673b561e2c5f8fa03d1256e7408cb4130e", "configuration"),
            FlorenceFile("configuration_florence2.py", 15_125, "653bafddc9651eaff1583a16db4a2bb27d33ec7d541dfab7201aaa4ecaa1cfbf", "pinned_model_code"),
            FlorenceFile("generation_config.json", 51, "30e9865458ecc8ee931eeeb43f44f1d169c5ab95be39e0072142a7a6b8f31990", "generation_configuration"),
            FlorenceFile("model.safetensors", 1_540_980_506, "8b4e610c952eef90a836c56cda0f398a672a3a6ca7b4d96b0e09a86dee42e2c3", "model_weights"),
            FlorenceFile("modeling_florence2.py", 127_415, "5bb7aa72c6ba62e96e1bbae6bc1aaf7b4e8e28cdfc62e670de3d5b67eeab1fdf", "pinned_model_code"),
            FlorenceFile("preprocessor_config.json", 806, "2f5921bbc53c7cc04251e1027b45b1cec726276be6db23d1bb40641bfbe2cf29", "processor_configuration"),
            FlorenceFile("processing_florence2.py", 46_372, "4bd7158536cbf1c7891fc8efd94437d79fd09f07f539c7398fab8a885d7d8bca", "pinned_processor_code"),
            FlorenceFile("tokenizer.json", 1_355_863, "847bbeab6174d66a88898f729d52fa8d355fafe1bea101cf960dd404581df70e", "tokenizer"),
            FlorenceFile("tokenizer_config.json", 34, "79ffcf43af8ebda99d165f61d243180da2e2639952e41e71e11611c18770489c", "tokenizer_configuration"),
            FlorenceFile("vocab.json", 1_099_884, "394fdc63c71aabe0a9b97117f5d62fb5fcc4d59b2b3ea929a3929e6a53217b3c", "vocabulary")
        ]
    };

    private static ManagedModelArtifactFile FlorenceFile(string name, long size, string sha256, string purpose) =>
        File(name, Resolve(FlorenceRepository, FlorenceRevision, name), size, sha256, purpose);

    private static ManagedModelArtifactFile File(string path, string source, long size, string sha256, string purpose) => new()
    {
        RelativePath = path,
        SourceUrl = source,
        SizeBytes = size,
        Sha256 = sha256,
        Purpose = purpose
    };

    private static ManagedModelConsumer Consumer(string id, string name, string kind) => new()
    {
        Id = id,
        DisplayName = name,
        Kind = kind
    };

    private static string Resolve(string repository, string revision, string file) =>
        $"https://huggingface.co/{repository}/resolve/{revision}/{Uri.EscapeDataString(file)}";

    private static string ResolveModelScope(string repository, string file) =>
        $"https://modelscope.cn/api/v1/models/{repository}/repo?Revision=master&FilePath={Uri.EscapeDataString(file)}";

    private static string CombineIfRoot(string root, params string[] parts) => string.IsNullOrWhiteSpace(root)
        ? string.Empty
        : parts.Aggregate(root, Path.Combine);
}
