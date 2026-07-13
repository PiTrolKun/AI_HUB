using System.IO;
using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public static class OpenAiSseStreamParser
{
    public static async Task<StructuredChatResult> ReadAsync(
        Stream stream,
        IProgress<ModelStreamChunk>? progress,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var content = new StringBuilder();
        var finishReason = string.Empty;
        var toolCalls = new Dictionary<int, ToolCallBuilder>();
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data == "[DONE]")
            {
                break;
            }

            using var document = JsonDocument.Parse(data);
            if (!document.RootElement.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
            {
                continue;
            }

            var choice = choices[0];
            if (choice.TryGetProperty("finish_reason", out var finish)
                && finish.ValueKind == JsonValueKind.String)
            {
                finishReason = finish.GetString() ?? finishReason;
            }

            if (!choice.TryGetProperty("delta", out var delta) || delta.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (delta.TryGetProperty("content", out var contentElement)
                && contentElement.ValueKind == JsonValueKind.String)
            {
                var text = contentElement.GetString() ?? string.Empty;
                content.Append(text);
                if (text.Length > 0)
                {
                    progress?.Report(new ModelStreamChunk(text));
                }
            }

            if (delta.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array)
            {
                foreach (var call in calls.EnumerateArray())
                {
                    var index = call.TryGetProperty("index", out var indexElement) && indexElement.TryGetInt32(out var parsedIndex)
                        ? parsedIndex
                        : 0;
                    if (!toolCalls.TryGetValue(index, out var builder))
                    {
                        builder = new ToolCallBuilder();
                        toolCalls[index] = builder;
                    }

                    builder.Append(call);
                }
            }
        }

        progress?.Report(new ModelStreamChunk(string.Empty, true));
        return new StructuredChatResult
        {
            Content = content.ToString().Trim(),
            FinishReason = finishReason,
            ToolCalls = toolCalls.OrderBy(pair => pair.Key).Select(pair => pair.Value.Build()).ToList()
        };
    }

    private sealed class ToolCallBuilder
    {
        private readonly StringBuilder _name = new();
        private readonly StringBuilder _arguments = new();
        private string _id = string.Empty;
        private string _type = "function";

        public void Append(JsonElement call)
        {
            if (call.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            {
                _id += id.GetString();
            }

            if (call.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
            {
                _type = type.GetString() ?? _type;
            }

            if (!call.TryGetProperty("function", out var function) || function.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (function.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            {
                _name.Append(name.GetString());
            }

            if (function.TryGetProperty("arguments", out var arguments) && arguments.ValueKind == JsonValueKind.String)
            {
                _arguments.Append(arguments.GetString());
            }
        }

        public StructuredToolCall Build() =>
            new()
            {
                Id = _id,
                Type = _type,
                Function = new StructuredToolCallFunction
                {
                    Name = _name.ToString(),
                    Arguments = _arguments.Length == 0 ? "{}" : _arguments.ToString()
                }
            };
    }
}
