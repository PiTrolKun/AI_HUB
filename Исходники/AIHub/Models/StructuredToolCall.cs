using System.Text.Json.Nodes;

namespace AIHub.Models;

public sealed class StructuredChatMessage
{
    public string Role { get; set; } = string.Empty;

    public string? Content { get; set; }

    public string? Name { get; set; }

    public string? ToolCallId { get; set; }

    public List<StructuredToolCall>? ToolCalls { get; set; }
}

public sealed class StructuredToolCall
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = "function";

    public StructuredToolCallFunction Function { get; set; } = new();
}

public sealed class StructuredToolCallFunction
{
    public string Name { get; set; } = string.Empty;

    public string Arguments { get; set; } = "{}";
}

public sealed class StructuredToolDefinition
{
    public string Type { get; set; } = "function";

    public StructuredToolFunction Function { get; set; } = new();
}

public sealed class StructuredToolFunction
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public JsonObject Parameters { get; set; } = [];
}

public sealed class StructuredChatResult
{
    public string Content { get; set; } = string.Empty;

    public string FinishReason { get; set; } = string.Empty;

    public List<StructuredToolCall> ToolCalls { get; set; } = [];

    public bool HasToolCalls => ToolCalls.Count > 0;
}
