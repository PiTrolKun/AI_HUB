using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public static class ExecutorModelManifestStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static ExecutorModelManifest? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<ExecutorModelManifest>(
                File.ReadAllText(path),
                JsonOptions);
            if (manifest is not null)
            {
                manifest.SemanticPassport ??= new ModelSemanticPassport();
            }

            return manifest;
        }
        catch
        {
            return null;
        }
    }

    public static void Save(string path, ExecutorModelManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Executor manifest directory is not available.");
        Directory.CreateDirectory(directory);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(manifest, JsonOptions),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static ModelSemanticPassport ResolvePassport(ExecutorModelManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (IsKnownQwenExecutor(manifest))
        {
            return CreateKnownQwenPassport(manifest);
        }

        manifest.SemanticPassport ??= new ModelSemanticPassport();
        return manifest.SemanticPassport;
    }

    public static ModelSemanticPassport PreparePassport(
        ExecutorModelManifest manifest,
        ModelSemanticPassport? previous = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (IsKnownQwenExecutor(manifest))
        {
            return CreateKnownQwenPassport(manifest);
        }

        var factsHash = ComputeFactsHash(manifest);
        var passport = previous ?? manifest.SemanticPassport ?? new ModelSemanticPassport();
        if (passport.Status == ModelSemanticPassportStatuses.Generated
            && string.Equals(passport.FactsHash, factsHash, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(passport.DescriptionRu)
            && !string.IsNullOrWhiteSpace(passport.DescriptionEn))
        {
            return passport;
        }

        passport.Status = ModelSemanticPassportStatuses.Pending;
        passport.FactsHash = factsHash;
        passport.LastError = string.Empty;
        return passport;
    }

    public static string ComputeFactsHash(ExecutorModelManifest manifest)
    {
        var facts = string.Join(
            "\n",
            manifest.RepoId.Trim(),
            manifest.RequestedModel.Trim(),
            manifest.File.Trim(),
            manifest.Format.Trim(),
            manifest.Quantization.Trim(),
            manifest.License.Trim(),
            manifest.Architecture.Trim(),
            manifest.RuntimeBackend.Trim(),
            manifest.TotalBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(facts)))
            .ToLowerInvariant();
    }

    public static bool IsKnownQwenExecutor(ExecutorModelManifest manifest)
    {
        var identity = string.Join(
            " ",
            manifest.RepoId,
            manifest.RequestedModel,
            manifest.File);
        return identity.Contains("Qwen3.6-27B", StringComparison.OrdinalIgnoreCase);
    }

    private static ModelSemanticPassport CreateKnownQwenPassport(
        ExecutorModelManifest manifest) => new()
        {
            Status = ModelSemanticPassportStatuses.Generated,
            DescriptionRu =
                "Локальная модель-координатор для анализа текста, рассуждений и подготовки ответов. "
                + "Запускается через llama.cpp в формате GGUF Q4_K_M и проверена на этом ПК. "
                + "Доступ к файлам, интернету и системным действиям получает только через разрешённые инструменты AI HUB; "
                + "поддержка изображений, аудио и видео этой сборкой не подтверждена.",
            DescriptionEn =
                "Local coordinator model for text analysis, reasoning, and response preparation. "
                + "Runs through llama.cpp as a GGUF Q4_K_M build and is verified on this PC. "
                + "It accesses files, the internet, and system actions only through approved AI HUB tools; "
                + "image, audio, and video support is not confirmed for this build.",
            Source = "manual_catalog",
            GeneratorModel = "AI HUB",
            FactsHash = ComputeFactsHash(manifest),
            LastError = string.Empty
        };
}
