using System.Text.Json.Nodes;

namespace AIHub.Services;

public static class ExecutorJsonContract
{
    public static JsonObject CreateResponseFormat() => new()
    {
        ["type"] = "json_schema",
        ["json_schema"] = new JsonObject
        {
            ["name"] = "uncertainty_executor_turn",
            ["strict"] = true,
            ["schema"] = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = new JsonObject
                {
                    ["status"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray("working", "stage_ready", "blocked")
                    },
                    ["action"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray(
                            "ask_user",
                            "confirm_brief",
                            "request_tool",
                            "blocked")
                    },
                    ["stageSummary"] = StringSchema(),
                    ["thought"] = StringSchema(),
                    ["question"] = StringSchema(),
                    ["options"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = StringSchema(),
                        ["maxItems"] = 6
                    },
                    ["allowCustom"] = new JsonObject { ["type"] = "boolean" },
                    ["currentResultSummary"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["maxLength"] = ExecutorResultSummaryPolicy.MaximumCharacters
                    },
                    ["requestedTools"] = StringArraySchema(),
                    ["missingCriticalInputs"] = StringArraySchema(),
                    ["assumptions"] = StringArraySchema(),
                    ["result"] = StringSchema(),
                    ["sources"] = StringArraySchema(),
                    ["warnings"] = StringArraySchema()
                },
                ["required"] = new JsonArray(
                    "status",
                    "action",
                    "stageSummary",
                    "thought",
                    "question",
                    "options",
                    "allowCustom",
                    "currentResultSummary",
                    "requestedTools",
                    "missingCriticalInputs",
                    "assumptions",
                    "result",
                    "sources",
                    "warnings")
            }
        }
    };

    private static JsonObject StringSchema() => new() { ["type"] = "string" };

    private static JsonObject StringArraySchema() => new()
    {
        ["type"] = "array",
        ["items"] = StringSchema()
    };
}
