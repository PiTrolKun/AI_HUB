using System.IO;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ImageAnalysisFileValidationService
{
    private static readonly HashSet<string> SupportedExtensions = new(
        [".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tif", ".tiff"],
        StringComparer.OrdinalIgnoreCase);

    public async Task<ImageAnalysisFilePassport> ValidateAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var fullPath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The selected image no longer exists.", fullPath);
        }

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            throw new InvalidDataException("The selected file format is not supported for image analysis.");
        }

        var info = new FileInfo(fullPath);
        if (info.Length <= 0)
        {
            throw new InvalidDataException("The selected image is empty.");
        }

        int pixelWidth;
        int pixelHeight;
        string format;
        try
        {
            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 128 * 1024,
                useAsync: true);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            var frame = decoder.Frames.FirstOrDefault()
                ?? throw new InvalidDataException("The image contains no decodable frame.");
            pixelWidth = frame.PixelWidth;
            pixelHeight = frame.PixelHeight;
            format = string.IsNullOrWhiteSpace(decoder.CodecInfo?.FriendlyName)
                ? extension.TrimStart('.').ToUpperInvariant()
                : decoder.CodecInfo.FriendlyName;
        }
        catch (Exception ex) when (ex is FileFormatException
            or NotSupportedException
            or InvalidOperationException)
        {
            throw new InvalidDataException("The selected image could not be decoded.", ex);
        }

        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            throw new InvalidDataException("The selected image has invalid dimensions.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using var hashStream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            useAsync: true);
        var hash = await SHA256.HashDataAsync(hashStream, cancellationToken);

        return new ImageAnalysisFilePassport
        {
            SourcePath = fullPath,
            DisplayName = info.Name,
            Extension = extension,
            Format = format,
            SizeBytes = info.Length,
            PixelWidth = pixelWidth,
            PixelHeight = pixelHeight,
            Sha256 = Convert.ToHexString(hash).ToLowerInvariant(),
            LastWriteTimeUtc = info.LastWriteTimeUtc
        };
    }
}
