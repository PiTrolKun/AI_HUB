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
                        ["enum"] = new JsonArray("clarification_step", "final_result", "cannot_continue")
                    },
                    ["thought"] = StringSchema(),
                    ["question"] = StringSchema(),
                    ["options"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = StringSchema(),
                        ["maxItems"] = 6
                    },
                    ["allowCustom"] = new JsonObject { ["type"] = "boolean" },
                    ["result"] = StringSchema(),
                    ["sources"] = StringArraySchema(),
                    ["warnings"] = StringArraySchema()
                },
                ["required"] = new JsonArray(
                    "status",
                    "thought",
                    "question",
                    "options",
                    "allowCustom",
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
