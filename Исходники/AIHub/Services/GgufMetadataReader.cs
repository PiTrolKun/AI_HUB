using System.IO;
using System.Text;

namespace AIHub.Services;

public sealed record GgufMetadata(string Architecture);

public static class GgufMetadataReader
{
    private const uint GgufMagic = 0x46554747;
    private const int MaxMetadataStringBytes = 16 * 1024 * 1024;

    public static GgufMetadata Read(ReadOnlyMemory<byte> data)
    {
        using var stream = new MemoryStream(data.ToArray(), writable: false);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        if (reader.ReadUInt32() != GgufMagic)
        {
            throw new InvalidDataException("The selected file is not a GGUF model.");
        }

        var version = reader.ReadUInt32();
        if (version is < 2 or > 3)
        {
            throw new InvalidDataException($"Unsupported GGUF version: {version}.");
        }

        _ = reader.ReadUInt64();
        var metadataCount = reader.ReadUInt64();
        for (ulong index = 0; index < metadataCount; index++)
        {
            var key = ReadString(reader);
            var valueType = reader.ReadUInt32();
            if (string.Equals(key, "general.architecture", StringComparison.Ordinal))
            {
                if (valueType != 8)
                {
                    throw new InvalidDataException("GGUF general.architecture has an invalid value type.");
                }

                return new GgufMetadata(ReadString(reader));
            }

            SkipValue(reader, valueType);
        }

        throw new InvalidDataException("GGUF metadata does not contain general.architecture.");
    }

    public static bool IsKnownUnsupportedArchitecture(string architecture) =>
        architecture.Contains("assistant", StringComparison.OrdinalIgnoreCase)
        || architecture.Contains("speculator", StringComparison.OrdinalIgnoreCase)
        || architecture.Contains("draft", StringComparison.OrdinalIgnoreCase)
        || architecture.Contains("mtp", StringComparison.OrdinalIgnoreCase);

    private static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadUInt64();
        if (length > MaxMetadataStringBytes || length > (ulong)(reader.BaseStream.Length - reader.BaseStream.Position))
        {
            throw new InvalidDataException("GGUF metadata string is incomplete or unreasonably large.");
        }

        return Encoding.UTF8.GetString(reader.ReadBytes((int)length));
    }

    private static void SkipValue(BinaryReader reader, uint valueType)
    {
        switch (valueType)
        {
            case 0:
            case 1:
            case 7:
                SkipBytes(reader, 1);
                return;
            case 2:
            case 3:
                SkipBytes(reader, 2);
                return;
            case 4:
            case 5:
            case 6:
                SkipBytes(reader, 4);
                return;
            case 8:
                _ = ReadString(reader);
                return;
            case 9:
                var elementType = reader.ReadUInt32();
                var count = reader.ReadUInt64();
                for (ulong index = 0; index < count; index++)
                {
                    SkipValue(reader, elementType);
                }
                return;
            case 10:
            case 11:
            case 12:
                SkipBytes(reader, 8);
                return;
            default:
                throw new InvalidDataException($"Unsupported GGUF metadata value type: {valueType}.");
        }
    }

    private static void SkipBytes(BinaryReader reader, long count)
    {
        if (count < 0 || reader.BaseStream.Position + count > reader.BaseStream.Length)
        {
            throw new InvalidDataException("GGUF metadata header is incomplete.");
        }

        reader.BaseStream.Seek(count, SeekOrigin.Current);
    }
}
