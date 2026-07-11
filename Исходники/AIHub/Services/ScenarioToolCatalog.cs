using System.Text.Json;
using System.Text.Json.Nodes;
using AIHub.Models;

namespace AIHub.Services;

public static class ScenarioToolCatalog
{
    public static List<StructuredToolDefinition> CreateDefinitions() =>
    [
        CreateTool("web_search", "Search the web for current facts or candidate pages.", ("query", "Search query.")),
        CreateTool("web_research", "Run several searches and read selected pages for current evidence.", ("task", "Research task.")),
        CreateTool("web_read", "Read a specific web page.", ("url", "Page URL.")),
        CreateTool("inventory", "Inspect installed AI HUB models and tools.", ("status", "Use 'status'.")),
        CreateModelCatalogSearchTool(),
        CreateHfFindModelTool(),
        CreateTool("hf_model_files", "Inspect files of a Hugging Face model repository.", ("repo_id", "Repository id."))
    ];

    public static string BuildCommand(StructuredToolCall toolCall)
    {
        var name = toolCall.Function.Name.Trim();
        var args = ParseArguments(toolCall.Function.Arguments);
        return name.ToLowerInvariant() switch
        {
            "web_search" => "web_search: " + GetArgument(args, "query"),
            "web_research" => "web_research: " + GetArgument(args, "task", "query"),
            "web_read" => "web_read: " + GetArgument(args, "url"),
            "inventory" => "inventory: status",
            "model_catalog_search" => "model_catalog_search: " + toolCall.Function.Arguments,
            "hf_find_model" => BuildHfFindModelCommand(args),
            "hf_model_files" => "hf_model_files: " + GetArgument(args, "repo_id", "repo"),
            _ => throw new InvalidOperationException($"Scenario tool is not allowed: {name}")
        };
    }

    private static StructuredToolDefinition CreateTool(
        string name,
        string description,
        params (string Name, string Description)[] parameters)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (var parameter in parameters)
        {
            properties[parameter.Name] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = parameter.Description
            };
            required.Add(parameter.Name);
        }

        return new StructuredToolDefinition
        {
            Function = new StructuredToolFunction
            {
                Name = name,
                Description = description,
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = properties,
                    ["required"] = required
                }
            }
        };
    }

    private static StructuredToolDefinition CreateHfFindModelTool()
    {
        return new StructuredToolDefinition
        {
            Function = new StructuredToolFunction
            {
                Name = "hf_find_model",
                Description = "Find model repositories matching task capabilities, size, format, license, and the user's PC. Form the repository query independently from task requirements. Do not copy the user's subject literally and do not default to the current core family.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["role"] = new JsonObject { ["type"] = "string" },
                        ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Repository search based on required model capabilities or an independently selected architecture. AI HUB does not prescribe a family or publisher." },
                        ["format"] = new JsonObject { ["type"] = "string" },
                        ["license"] = new JsonObject { ["type"] = "string" },
                        ["max_size"] = new JsonObject { ["type"] = "string" }
                    },
                    ["required"] = new JsonArray("role", "query")
                }
            }
        };
    }

    private static StructuredToolDefinition CreateModelCatalogSearchTool()
    {
        return new StructuredToolDefinition
        {
            Function = new StructuredToolFunction
            {
                Name = "model_catalog_search",
                Description = "Query AI HUB's independent local model catalog. Returns up to six evidence-backed candidates but never chooses the winner. Use this before live Hugging Face search; call hf_find_model only when local candidates are missing or need current verification.",
                Parameters = new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["directions"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["maxItems"] = 4,
                            ["description"] = "Catalog capability groups: text_knowledge for general text/reasoning; science_professional for science/medical/professional work; agents_code for coding/tool agents; data_forecasting for analysis/time series; search_memory for embeddings/retrieval/reranking; vision_documents for image/document understanding; image_generation, video, audio_speech, spatial_robotics, or safety_control for their named specialist tasks.",
                            ["items"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["enum"] = new JsonArray(
                                    "agents_code", "audio_speech", "data_forecasting", "image_generation",
                                    "safety_control", "science_professional", "search_memory", "spatial_robotics",
                                    "text_knowledge", "video", "vision_documents")
                            }
                        },
                        ["taskType"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "The intellectual operation from the capability profile, not the user's subject wording."
                        },
                        ["requiredCapabilities"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["maxItems"] = 8,
                            ["items"] = new JsonObject { ["type"] = "string" }
                        },
                        ["loadLevel"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray("any", "light", "optimal", "extreme")
                        },
                        ["limit"] = new JsonObject
                        {
                            ["type"] = "integer",
                            ["minimum"] = 1,
                            ["maximum"] = 6
                        }
                    },
                    ["required"] = new JsonArray("directions", "taskType", "requiredCapabilities", "loadLevel", "limit")
                }
            }
        };
    }

    private static Dictionary<string, string> ParseArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return [];
        }

        using var document = JsonDocument.Parse(arguments);
        return document.RootElement.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string GetArgument(
        IReadOnlyDictionary<string, string> args,
        string primary,
        string? secondary = null)
    {
        if (args.TryGetValue(primary, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        if (secondary is not null && args.TryGetValue(secondary, out value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException($"Required tool argument is missing: {primary}");
    }

    private static string BuildHfFindModelCommand(IReadOnlyDictionary<string, string> args)
    {
        var parts = new List<string>
        {
            "role=" + GetArgument(args, "role"),
            "query=" + GetArgument(args, "query")
        };
        foreach (var key in new[] { "format", "license", "max_size" })
        {
            if (args.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{key}={value.Trim()}");
            }
        }

        return "hf_find_model: " + string.Join(' ', parts);
    }
}
