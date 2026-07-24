using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ModelSemanticPassportService
{
    private const int MaximumDescriptionLength = 700;
    private readonly ConcurrentDictionary<string, byte> _running = new(StringComparer.OrdinalIgnoreCase);
    private readonly UserContextService _userContextService;
    private readonly CoreModelManager _coreModelManager = new();

    public ModelSemanticPassportService(UserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    public bool QueueGeneration(
        ExecutorModelArtifact artifact,
        StorageSettings storageSettings)
    {
        var manifestPath = GetManifestPath(artifact);
        var manifest = ExecutorModelManifestStore.Load(manifestPath);
        if (manifest is null || manifest.Status != "installed" || manifest.RuntimeVerifiedAt is null)
        {
            return false;
        }

        var passport = ExecutorModelManifestStore.ResolvePassport(manifest);
        var factsHash = ExecutorModelManifestStore.ComputeFactsHash(manifest);
        if (passport.Source == "manual_catalog"
            || passport.Status == ModelSemanticPassportStatuses.Generated
            && string.Equals(passport.FactsHash, factsHash, StringComparison.OrdinalIgnoreCase))
        {
            if (passport.Source == "manual_catalog")
            {
                manifest.SemanticPassport = passport;
                ExecutorModelManifestStore.Save(manifestPath, manifest);
            }

            return false;
        }

        if (!_running.TryAdd(manifestPath, 0))
        {
            return false;
        }

        manifest.SemanticPassport = ExecutorModelManifestStore.PreparePassport(
            manifest,
            passport);
        ExecutorModelManifestStore.Save(manifestPath, manifest);
        _ = Task.Run(async () =>
        {
            try
            {
                await GenerateAndSaveAsync(
                    manifestPath,
                    storageSettings,
                    CancellationToken.None);
            }
            finally
            {
                _running.TryRemove(manifestPath, out _);
            }
        });
        return true;
    }

    internal async Task GenerateAndSaveAsync(
        string manifestPath,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var manifest = ExecutorModelManifestStore.Load(manifestPath)
            ?? throw new InvalidOperationException("Executor manifest is unavailable.");
        try
        {
            var core = _coreModelManager.Check(storageSettings);
            if (core.Availability != CoreModelAvailability.Installed
                || string.IsNullOrWhiteSpace(core.ModelPath))
            {
                throw new InvalidOperationException("The core model is not available for passport generation.");
            }

            using var runtime = new LlamaServerRuntimeService(_userContextService);
            var coreModel = new DebugModelInfo
            {
                Name = CoreModelManager.CoreModelDisplayName,
                Path = core.ModelPath,
                SizeBytes = new FileInfo(core.ModelPath).Length,
                Role = "core",
                Status = "installed",
                Format = "gguf",
                IsCoreModel = true,
                IsRunnable = true
            };
            var response = await runtime.GenerateUtilityAsync(
                coreModel,
                BuildSystemPrompt(),
                BuildFactsPrompt(manifest),
                _ => { },
                cancellationToken);
            var descriptions = ParseDescriptions(response);
            var current = ExecutorModelManifestStore.Load(manifestPath) ?? manifest;
            current.SemanticPassport = new ModelSemanticPassport
            {
                Status = ModelSemanticPassportStatuses.Generated,
                DescriptionRu = descriptions.Ru,
                DescriptionEn = descriptions.En,
                Source = "core_generated",
                GeneratorModel = CoreModelManager.CoreModelDisplayName,
                FactsHash = ExecutorModelManifestStore.ComputeFactsHash(current),
                GeneratedAt = DateTimeOffset.Now,
                LastError = string.Empty
            };
            ExecutorModelManifestStore.Save(manifestPath, current);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var current = ExecutorModelManifestStore.Load(manifestPath) ?? manifest;
            current.SemanticPassport ??= new ModelSemanticPassport();
            current.SemanticPassport.Status = ModelSemanticPassportStatuses.Failed;
            current.SemanticPassport.FactsHash = ExecutorModelManifestStore.ComputeFactsHash(current);
            current.SemanticPassport.LastError = Limit(
                Normalize(ex.Message),
                500);
            ExecutorModelManifestStore.Save(manifestPath, current);
        }
    }

    internal static string BuildSystemPrompt() =>
        """
        You write compact technical capability passports for locally installed AI models.
        Use only the supplied verified facts. Never infer multimodal support, tool access, training data,
        quality, safety, speed, or task suitability unless explicitly stated.
        Explain that direct file, web, and system actions require separate approved AI HUB tools.
        Return one strict JSON object with exactly two string fields: "ru" and "en".
        Each value must be one technical paragraph of 2-4 short sentences and at most 650 characters.
        Do not use Markdown, headings, lists, comments, or text outside JSON.
        """;

    internal static string BuildFactsPrompt(ExecutorModelManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Write a semantic passport for this installed executor model.");
        builder.AppendLine($"Repository: {manifest.RepoId}");
        builder.AppendLine($"Requested model: {manifest.RequestedModel}");
        builder.AppendLine($"File: {manifest.File}");
        builder.AppendLine($"Format: {manifest.Format}");
        builder.AppendLine($"Quantization: {manifest.Quantization}");
        builder.AppendLine($"Architecture: {manifest.Architecture}");
        builder.AppendLine($"Size bytes: {manifest.TotalBytes}");
        builder.AppendLine($"License: {manifest.License}");
        builder.AppendLine($"Runtime backend: {manifest.RuntimeBackend}");
        builder.AppendLine($"Runtime verified: {manifest.RuntimeVerifiedAt is not null}");
        builder.AppendLine("Program role: local executor/coordinator model.");
        return builder.ToString().Trim();
    }

    internal static (string Ru, string En) ParseDescriptions(string response)
    {
        var json = ExtractJson(response);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var ru = root.TryGetProperty("ru", out var ruValue) ? ruValue.GetString() : null;
        var en = root.TryGetProperty("en", out var enValue) ? enValue.GetString() : null;
        ru = Limit(Normalize(ru), MaximumDescriptionLength);
        en = Limit(Normalize(en), MaximumDescriptionLength);
        if (ru.Length < 40 || en.Length < 40)
        {
            throw new InvalidDataException("Generated semantic passport is incomplete.");
        }

        return (ru, en);
    }

    private static string ExtractJson(string response)
    {
        var trimmed = response.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = Regex.Replace(trimmed, @"^```(?:json)?\s*", string.Empty, RegexOptions.IgnoreCase);
            trimmed = Regex.Replace(trimmed, @"\s*```$", string.Empty);
        }

        var first = trimmed.IndexOf('{');
        var last = trimmed.LastIndexOf('}');
        return first >= 0 && last > first
            ? trimmed[first..(last + 1)]
            : trimmed;
    }

    private static string Normalize(string? value) =>
        Regex.Replace(value?.Trim() ?? string.Empty, @"\s+", " ");

    private static string Limit(string value, int maximum) =>
        value.Length <= maximum ? value : value[..maximum].TrimEnd();

    private static string GetManifestPath(ExecutorModelArtifact artifact)
    {
        var directory = Path.GetDirectoryName(artifact.InstalledPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Executor artifact does not have an installation directory.");
        }

        return Path.Combine(directory, "executor-model.json");
    }
}
