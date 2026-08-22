using System.Text.Json;

namespace AIHub.Services;

public sealed class SpecialistToolResultNormalizer
{
    public NormalizedSpecialistToolResult Normalize(
        string toolName,
        string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new NormalizedSpecialistToolResult
            {
                ToolName = toolName,
                UserText = string.Empty
            };
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new NormalizedSpecialistToolResult
                {
                    ToolName = toolName,
                    WasStructured = true
                };
            }

            var description = ReadString(root, "description");
            var message = ReadString(root, "message");
            return new NormalizedSpecialistToolResult
            {
                ToolName = toolName,
                WasStructured = true,
                Success = ReadBoolean(root, "success"),
                EvidenceType = ReadString(root, "evidence_type"),
                SourceFileId = FirstNonEmpty(
                    ReadString(root, "source_file_id"),
                    ReadString(root, "file_id")),
                Model = ReadString(root, "model"),
                Description = description,
                UserText = FirstNonEmpty(description, message)
            };
        }
        catch (JsonException)
        {
            var trimmed = payload.TrimStart();
            return trimmed.StartsWith('{') || trimmed.StartsWith('[')
                ? new NormalizedSpecialistToolResult
                {
                    ToolName = toolName,
                    WasStructured = true
                }
                : PlainText(toolName, payload);
        }
    }

    private static NormalizedSpecialistToolResult PlainText(string toolName, string payload) =>
        new()
        {
            ToolName = toolName,
            UserText = payload.Trim()
        };

    private static string ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }

    private static bool ReadBoolean(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.True;

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}

public sealed class NormalizedSpecialistToolResult
{
    public string ToolName { get; set; } = string.Empty;

    public bool WasStructured { get; set; }

    public bool Success { get; set; }

    public string EvidenceType { get; set; } = string.Empty;

    public string SourceFileId { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string UserText { get; set; } = string.Empty;
}
