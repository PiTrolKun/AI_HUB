using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AIHub.Models;
using ClosedXML.Excel;

namespace AIHub.Services;

public sealed class SandboxArtifactMaterializerService
{
    private readonly ArtifactValidationService _validationService = new();

    public SandboxArtifactMaterializationResult Materialize(
        ExecutorResultSnapshot snapshot,
        ExecutorHandoffPackage handoff,
        SessionFileManifest fileManifest,
        StorageSettings storageSettings,
        IReadOnlyCollection<ExecutionEvidenceReceipt>? evidenceReceipts = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(handoff);
        ArgumentNullException.ThrowIfNull(fileManifest);
        ArgumentNullException.ThrowIfNull(storageSettings);

        var contract = handoff.ArtifactContract ?? new ArtifactContract();
        var recipe = handoff.ExecutionBundle.Recipes.FirstOrDefault()
            ?? new SandboxExecutionRecipeService().Build([], contract)[0];
        var outputDirectory = CreateOutputDirectory(storageSettings);
        var baseName = $"result_{snapshot.Version}_{DateTime.Now:yyyyMMdd_HHmmss}";
        var result = new SandboxArtifactMaterializationResult
        {
            ArtifactKind = contract.ArtifactKind,
            MimeType = contract.MimeType,
            QualityLevel = ResolveQualityLevel(handoff.ExecutionBundle),
            RecipeId = recipe.Id
        };

        var producedArtifact = FindProducedArtifact(contract, evidenceReceipts);
        if (producedArtifact is not null)
        {
            result.FilePath = CopyProducedArtifact(
                producedArtifact,
                outputDirectory,
                baseName);
            result.SourceReceiptId = producedArtifact.Id;
        }
        else
        {
            result.FilePath = contract.ArtifactKind switch
            {
                ArtifactKinds.Document => WriteDocument(
                    snapshot,
                    Path.Combine(outputDirectory, baseName + ".docx")),
                ArtifactKinds.Table => WriteWorkbook(
                    snapshot,
                    Path.Combine(outputDirectory, baseName + ".xlsx")),
                ArtifactKinds.Image => CopyMatchingSourceOrCreateImage(
                    fileManifest,
                    SessionFileCategories.Image,
                    outputDirectory,
                    baseName,
                    result),
                ArtifactKinds.Audio => CopyMatchingSourceOrCreateAudio(
                    fileManifest,
                    outputDirectory,
                    baseName,
                    result),
                ArtifactKinds.Video => CopyMatchingSourceOrCreateVideo(
                    fileManifest,
                    outputDirectory,
                    baseName,
                    result),
                ArtifactKinds.Archive => WriteArchive(
                    snapshot,
                    Path.Combine(outputDirectory, baseName + ".zip")),
                ArtifactKinds.Code => WriteText(
                    snapshot.Markdown,
                    Path.Combine(
                        outputDirectory,
                        baseName + NormalizeTextExtension(contract.PreferredExtension, ".md"))),
                _ => WriteText(
                    snapshot.Markdown,
                    Path.Combine(
                        outputDirectory,
                        baseName + NormalizeTextExtension(contract.PreferredExtension, ".txt")))
            };
        }
        result.Validation = _validationService.Validate(result.FilePath, contract);
        result.MimeType = result.Validation.DetectedMimeType;
        return result;
    }

    private static ExecutionEvidenceReceipt? FindProducedArtifact(
        ArtifactContract contract,
        IReadOnlyCollection<ExecutionEvidenceReceipt>? receipts)
    {
        return receipts?
            .Where(receipt => receipt.Success)
            .Where(receipt => !string.IsNullOrWhiteSpace(receipt.OutputArtifactPath))
            .Where(receipt => File.Exists(receipt.OutputArtifactPath))
            .Where(receipt => IsCompatibleArtifact(contract, receipt.OutputArtifactPath))
            .OrderByDescending(receipt => receipt.CreatedAt)
            .FirstOrDefault();
    }

    private static bool IsCompatibleArtifact(ArtifactContract contract, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var preferredExtension = contract.PreferredExtension?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(preferredExtension)
            && string.Equals(extension, preferredExtension, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return contract.ArtifactKind switch
        {
            ArtifactKinds.Document => extension is ".docx" or ".pdf" or ".odt" or ".rtf",
            ArtifactKinds.Table => extension is ".xlsx" or ".csv" or ".ods",
            ArtifactKinds.Image => extension is ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp" or ".tiff",
            ArtifactKinds.Audio => extension is ".wav" or ".mp3" or ".flac" or ".ogg" or ".m4a",
            ArtifactKinds.Video => extension is ".mp4" or ".mkv" or ".webm" or ".avi" or ".mov",
            ArtifactKinds.Archive => extension is ".zip" or ".7z" or ".tar" or ".gz",
            ArtifactKinds.Code => extension is ".cs" or ".py" or ".js" or ".ts" or ".html" or ".css" or ".json" or ".xml",
            _ => extension is ".txt" or ".md" or ".json" or ".xml"
        };
    }

    private static string CopyProducedArtifact(
        ExecutionEvidenceReceipt receipt,
        string outputDirectory,
        string baseName)
    {
        var extension = Path.GetExtension(receipt.OutputArtifactPath).ToLowerInvariant();
        var destination = Path.Combine(outputDirectory, baseName + extension);
        File.Copy(receipt.OutputArtifactPath, destination, overwrite: true);
        return destination;
    }

    private static string CreateOutputDirectory(StorageSettings storageSettings)
    {
        var configuredRoot = storageSettings.Results.Locations
            .Select(location => location.Path?.Trim())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        var root = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(AppDataPaths.RuntimeDirectory, "Sandbox", "Artifacts")
            : Path.Combine(configuredRoot, "AI_HUB", "Sandbox", "Artifacts");
        var directory = Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string ResolveQualityLevel(ExecutionBundlePlan bundle) =>
        bundle.SelectedRouteLevel switch
        {
            ExecutionRouteLevels.Preferred => ArtifactQualityLevels.Preferred,
            ExecutionRouteLevels.Degraded => ArtifactQualityLevels.Degraded,
            _ => ArtifactQualityLevels.Emergency
        };

    private static string WriteDocument(
        ExecutorResultSnapshot snapshot,
        string path)
    {
        ExecutorDocxExporter.Export(snapshot, path);
        return path;
    }

    private static string WriteWorkbook(
        ExecutorResultSnapshot snapshot,
        string path)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Result");
        var lines = snapshot.Markdown
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            sheet.Cell(index + 1, 1).Value = lines[index];
        }

        sheet.Column(1).Width = 100;
        sheet.Column(1).Style.Alignment.WrapText = true;
        workbook.SaveAs(path);
        return path;
    }

    private static string CopyMatchingSourceOrCreateImage(
        SessionFileManifest manifest,
        string category,
        string outputDirectory,
        string baseName,
        SandboxArtifactMaterializationResult result)
    {
        var source = FindSource(manifest, category);
        if (source is not null)
        {
            result.QualityLevel = ArtifactQualityLevels.Emergency;
            result.Warnings.Add(
                "No safe image transformation completed; the source image was copied unchanged.");
            return CopySource(source, outputDirectory, baseName);
        }

        var path = Path.Combine(outputDirectory, baseName + ".png");
        var pixels = new byte[] { 31, 45, 68, 255 };
        var bitmap = BitmapSource.Create(
            1,
            1,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
        result.QualityLevel = ArtifactQualityLevels.Emergency;
        result.Warnings.Add(
            "No source image or safe image processor was available; a valid placeholder image was created.");
        return path;
    }

    private static string CopyMatchingSourceOrCreateAudio(
        SessionFileManifest manifest,
        string outputDirectory,
        string baseName,
        SandboxArtifactMaterializationResult result)
    {
        var source = FindSource(manifest, SessionFileCategories.Audio);
        if (source is not null)
        {
            result.QualityLevel = ArtifactQualityLevels.Emergency;
            result.Warnings.Add(
                "No safe audio transformation completed; the source audio was copied unchanged.");
            return CopySource(source, outputDirectory, baseName);
        }

        var path = Path.Combine(outputDirectory, baseName + ".wav");
        WriteSilentWav(path);
        result.QualityLevel = ArtifactQualityLevels.Emergency;
        result.Warnings.Add(
            "No source audio or safe audio processor was available; a valid silent WAV was created.");
        return path;
    }

    private static string CopyMatchingSourceOrCreateVideo(
        SessionFileManifest manifest,
        string outputDirectory,
        string baseName,
        SandboxArtifactMaterializationResult result)
    {
        var source = FindSource(manifest, SessionFileCategories.Video);
        if (source is null)
        {
            throw new InvalidOperationException(
                "A video artifact cannot be created without source video or a verified video generator.");
        }

        result.QualityLevel = ArtifactQualityLevels.Emergency;
        result.Warnings.Add(
            "No safe video transformation completed; the source video was copied unchanged.");
        return CopySource(source, outputDirectory, baseName);
    }

    private static string WriteArchive(
        ExecutorResultSnapshot snapshot,
        string path)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("result.md", CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(snapshot.Markdown);
        return path;
    }

    private static string WriteText(string content, string path)
    {
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }

    private static SessionFileReference? FindSource(
        SessionFileManifest manifest,
        string category) =>
        manifest.Files.FirstOrDefault(file =>
            file.IsAvailable
            && string.Equals(file.Category, category, StringComparison.OrdinalIgnoreCase)
            && File.Exists(file.SourcePath));

    private static string CopySource(
        SessionFileReference source,
        string outputDirectory,
        string baseName)
    {
        var extension = Path.GetExtension(source.SourcePath);
        var destination = Path.Combine(outputDirectory, baseName + extension.ToLowerInvariant());
        File.Copy(source.SourcePath, destination, overwrite: true);
        return destination;
    }

    private static string NormalizeTextExtension(
        string extension,
        string fallback)
    {
        var value = extension?.Trim().ToLowerInvariant() ?? string.Empty;
        if (value is ".txt" or ".md" or ".json" or ".xml" or ".patch" or ".diff"
            or ".cs" or ".py" or ".js" or ".ts" or ".html" or ".css")
        {
            return value;
        }

        return fallback;
    }

    private static void WriteSilentWav(string path)
    {
        const int sampleRate = 8000;
        const short channels = 1;
        const short bitsPerSample = 16;
        const int durationMilliseconds = 500;
        var sampleCount = sampleRate * durationMilliseconds / 1000;
        var dataLength = sampleCount * channels * bitsPerSample / 8;
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);
        writer.Write(new byte[dataLength]);
    }
}
