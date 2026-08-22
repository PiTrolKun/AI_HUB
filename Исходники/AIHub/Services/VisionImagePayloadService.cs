using System.IO;
using System.Windows.Media.Imaging;
using AIHub.Models;

namespace AIHub.Services;

internal sealed record VisionImagePayload(
    byte[] Bytes,
    string MimeType,
    string SourceExtension,
    bool WasNormalized);

internal sealed class VisionImagePayloadService
{
    private static readonly HashSet<string> DirectExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

    public async Task<VisionImagePayload> PrepareAsync(
        SessionFileReference image,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(image);
        cancellationToken.ThrowIfCancellationRequested();

        var extension = NormalizeExtension(image.Extension, image.SourcePath);
        if (DirectExtensions.Contains(extension))
        {
            var bytes = await File.ReadAllBytesAsync(image.SourcePath, cancellationToken);
            return new VisionImagePayload(
                bytes,
                extension is ".jpg" or ".jpeg" ? "image/jpeg" : "image/png",
                extension,
                WasNormalized: false);
        }

        try
        {
            using var source = new FileStream(
                image.SourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            var decoder = BitmapDecoder.Create(
                source,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            var frame = decoder.Frames.FirstOrDefault()
                ?? throw new InvalidDataException("The image contains no decodable frame.");
            cancellationToken.ThrowIfCancellationRequested();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(frame));
            using var normalized = new MemoryStream();
            encoder.Save(normalized);
            cancellationToken.ThrowIfCancellationRequested();
            return new VisionImagePayload(
                normalized.ToArray(),
                "image/png",
                extension,
                WasNormalized: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidDataException
            or InvalidOperationException
            or NotSupportedException
            or FileFormatException)
        {
            throw new SessionFileToolException(
                "image_decode_failed",
                "The attached image could not be decoded into a format supported by the local vision model.",
                ex.GetType().Name);
        }
    }

    private static string NormalizeExtension(string extension, string sourcePath)
    {
        var value = string.IsNullOrWhiteSpace(extension)
            ? Path.GetExtension(sourcePath)
            : extension;
        value = value.Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        return value.StartsWith('.') ? value.ToLowerInvariant() : $".{value.ToLowerInvariant()}";
    }
}
