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
                            "request_capability",
                            "suggest_finalization",
                            "blocked")
                    },
                    ["stageSummary"] = StringSchema(),
                    ["thought"] = StringSchema(),
                    ["question"] = StringSchema(),
                    ["options"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["properties"] = new JsonObject
                            {
                                ["title"] = StringSchema(),
                                ["intent"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = new JsonArray(
                                        "answer",
                                        "approve_action",
                                        "decline_action")
                                },
                                ["action"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["enum"] = new JsonArray(
                                        "",
                                        "session_files_list",
                                        "session_file_inspect",
                                        "session_file_read")
                                },
                                ["targetId"] = StringSchema(),
                                ["effect"] = StringSchema(),
                                ["isRecommended"] = new JsonObject { ["type"] = "boolean" }
                            },
                            ["required"] = new JsonArray(
                                "title",
                                "intent",
                                "action",
                                "targetId",
                                "effect",
                                "isRecommended")
                        },
                        ["maxItems"] = 6
                    },
                    ["allowCustom"] = new JsonObject { ["type"] = "boolean" },
                    ["currentResultSummary"] = StringSchema(),
                    ["workingResultFragment"] = StringSchema(),
                    ["canFinalize"] = new JsonObject { ["type"] = "boolean" },
                    ["completionReason"] = StringSchema(),
                    ["requestedTools"] = StringArraySchema(3),
                    ["requestedCapabilities"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["properties"] = new JsonObject
                            {
                                ["id"] = StringSchema(),
                                ["purpose"] = StringSchema(),
                                ["required"] = new JsonObject { ["type"] = "boolean" },
                                ["alternatives"] = StringArraySchema(6)
                            },
                            ["required"] = new JsonArray(
                                "id",
                                "purpose",
                                "required",
                                "alternatives")
                        },
                        ["maxItems"] = 8
                    },
                    ["missingCriticalInputs"] = StringArraySchema(8),
                    ["assumptions"] = StringArraySchema(8),
                    ["result"] = StringSchema(),
                    ["sources"] = StringArraySchema(12),
                    ["warnings"] = StringArraySchema(8)
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
                    "workingResultFragment",
                    "canFinalize",
                    "completionReason",
                    "requestedTools",
                    "requestedCapabilities",
                    "missingCriticalInputs",
                    "assumptions",
                    "result",
                    "sources",
                    "warnings")
            }
        }
    };

    private static JsonObject StringSchema() => new()
    {
        ["type"] = "string"
    };

    private static JsonObject StringArraySchema(int maximumItems) => new()
    {
        ["type"] = "array",
        ["items"] = StringSchema(),
        ["maxItems"] = maximumItems
    };
}
