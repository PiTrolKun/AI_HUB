using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutorModelArtifactResolver(IExecutorArtifactSource source)
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly string[] QuantizationPriority =
    [
        "Q4_K_M", "IQ4_XS", "Q5_K_M", "Q4_K_S", "Q5_K_S", "Q6_K", "Q8_0"
    ];
    private static readonly string[] AuxiliaryFileMarkers =
    [
        "mmproj", "mtp-", "-mtp-", "imatrix", "adapter", "lora", "projector",
        "tokenizer", "draft-", "-draft-", "speculator", "vision-tower"
    ];
    private static readonly Regex ParameterCountPattern = new(
        @"(?<![A-Za-z0-9])(?<value>\d+(?:\.\d+)?)\s*[Bb](?![A-Za-z])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<ExecutorModelArtifact> ResolveAsync(
        string requestedModel,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestedModel))
        {
            throw new InvalidOperationException("The task card does not contain an executor model.");
        }

        var normalizedRequest = requestedModel.Trim();
        var installed = FindInstalled(normalizedRequest, storageSettings);
        if (installed is not null)
        {
            return installed;
        }

        var candidates = new List<HuggingFaceModelCandidate>();
        try
        {
            var direct = await source.GetFilesAsync(normalizedRequest, storageSettings, cancellationToken);
            if (!string.IsNullOrWhiteSpace(direct.RepoId))
            {
                candidates.Add(direct);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            // A selected Transformers repository often has no GGUF files. Search quantized mirrors next.
        }

        var searchResults = await source.SearchGgufAsync(normalizedRequest, storageSettings, cancellationToken);
        candidates.AddRange(searchResults.Where(candidate =>
            SimilarityScore(normalizedRequest, candidate.RepoId) > 0));

        var artifact = candidates
            .GroupBy(candidate => candidate.RepoId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .SelectMany(candidate => SelectArtifacts(normalizedRequest, candidate))
            .OrderBy(candidate => QuantizationRank(candidate.FileName))
            .ThenByDescending(candidate => SimilarityScore(normalizedRequest, candidate.RepoId))
            .ThenBy(candidate => candidate.SizeBytes)
            .FirstOrDefault();
        if (artifact is not null)
        {
            return artifact;
        }

        throw new InvalidOperationException(
            "No plausible standalone main-weight GGUF artifact was found for the selected executor. Auxiliary and split files are rejected.");
    }

    private static ExecutorModelArtifact? FindInstalled(string requestedModel, StorageSettings storageSettings)
    {
        foreach (var root in storageSettings.Models.Locations.Select(value => value.Path))
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            IEnumerable<string> manifestPaths;
            try
            {
                manifestPaths = Directory.EnumerateFiles(root, "executor-model.json", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var manifestPath in manifestPaths)
            {
                try
                {
                    var manifest = JsonSerializer.Deserialize<ExecutorModelManifest>(File.ReadAllText(manifestPath), ManifestJsonOptions);
                    var directory = Path.GetDirectoryName(manifestPath);
                    var modelPath = directory is null ? null : Path.Combine(directory, manifest?.File ?? string.Empty);
                    if (manifest?.Status == "installed"
                        && manifest.RuntimeVerifiedAt is not null
                        && modelPath is not null
                        && File.Exists(modelPath)
                        && (string.Equals(manifest.RepoId, requestedModel, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(manifest.RequestedModel, requestedModel, StringComparison.OrdinalIgnoreCase)))
                    {
                        return FromManifest(manifest, modelPath);
                    }
                }
                catch (JsonException)
                {
                }
            }
        }

        return null;
    }

    private static IEnumerable<ExecutorModelArtifact> SelectArtifacts(
        string requestedModel,
        HuggingFaceModelCandidate candidate)
    {
        foreach (var file in candidate.Files
                     .Where(file => file.FileName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                     .Where(file => !file.FileName.Contains("-of-", StringComparison.OrdinalIgnoreCase))
                     .Where(file => !IsAuxiliaryFile(file.FileName))
                     .Where(file => file.SizeBytes is > 0)
                     .Where(file => IsPlausibleMainWeight(requestedModel, candidate.RepoId, file)))
        {
            yield return new ExecutorModelArtifact
            {
                RequestedModel = requestedModel,
                RepoId = candidate.RepoId,
                FileName = file.FileName,
                DownloadUrl = file.DownloadUrl,
                SizeBytes = file.SizeBytes ?? 0,
                Sha256 = NormalizeLfsOid(file.LfsOid),
                Quantization = ReadQuantization(file.FileName),
                License = candidate.License
            };
        }
    }

    internal static bool IsAuxiliaryFile(string fileName) =>
        AuxiliaryFileMarkers.Any(marker => fileName.Contains(marker, StringComparison.OrdinalIgnoreCase));

    internal static bool IsPlausibleMainWeight(
        string requestedModel,
        string repoId,
        HuggingFaceModelFile file)
    {
        var parameters = TryReadLargestParameterCount(requestedModel, repoId, file.FileName);
        if (parameters is null || file.SizeBytes is not > 0)
        {
            return true;
        }

        var bytesPerParameter = ReadQuantization(file.FileName) switch
        {
            "Q8_0" => 1.0,
            "Q6_K" => 0.80,
            "Q5_K_M" or "Q5_K_S" => 0.68,
            "Q4_K_M" or "Q4_K_S" or "IQ4_XS" => 0.55,
            _ => 0.30
        };
        var expectedBytes = parameters.Value * bytesPerParameter;
        return file.SizeBytes.Value >= expectedBytes * 0.45
            && file.SizeBytes.Value <= expectedBytes * 2.2;
    }

    private static long? TryReadLargestParameterCount(params string[] values)
    {
        double? billions = values
            .SelectMany(value => ParameterCountPattern.Matches(value).Select(match =>
                double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : 0))
            .Where(value => value is > 0 and < 2000)
            .OrderDescending()
            .Cast<double?>()
            .FirstOrDefault();
        return billions is null ? null : checked((long)(billions.Value * 1_000_000_000d));
    }

    private static ExecutorModelArtifact FromManifest(ExecutorModelManifest manifest, string path) =>
        new()
        {
            RequestedModel = manifest.RequestedModel,
            RepoId = manifest.RepoId,
            FileName = manifest.File,
            DownloadUrl = manifest.Source,
            SizeBytes = manifest.TotalBytes,
            Sha256 = manifest.Sha256,
            Quantization = manifest.Quantization,
            License = manifest.License,
            Architecture = manifest.Architecture,
            IsInstalled = true,
            InstalledPath = path
        };

    internal static int QuantizationRank(string fileName)
    {
        for (var index = 0; index < QuantizationPriority.Length; index++)
        {
            if (fileName.Contains(QuantizationPriority[index], StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return QuantizationPriority.Length;
    }

    private static string ReadQuantization(string fileName) =>
        QuantizationPriority.FirstOrDefault(value => fileName.Contains(value, StringComparison.OrdinalIgnoreCase))
        ?? "unknown";

    private static string NormalizeLfsOid(string value) =>
        value.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) ? value[7..] : value;

    private static int SimilarityScore(string requestedModel, string repoId)
    {
        var requestedParts = requestedModel.Split(['/', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return requestedParts.Count(part => repoId.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}
