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
                                ["title"] = StringSchema(120),
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
                                ["targetId"] = StringSchema(160),
                                ["effect"] = StringSchema(240),
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
                    ["currentResultSummary"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["maxLength"] = ExecutorResultSummaryPolicy.MaximumCharacters
                    },
                    ["workingResultFragment"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["maxLength"] = ExecutorWorkingResultPolicy.MaximumCharacters
                    },
                    ["canFinalize"] = new JsonObject { ["type"] = "boolean" },
                    ["completionReason"] = StringSchema(600),
                    ["requestedTools"] = StringArraySchema(3, 80),
                    ["requestedCapabilities"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["properties"] = new JsonObject
                            {
                                ["id"] = StringSchema(120),
                                ["purpose"] = StringSchema(400),
                                ["required"] = new JsonObject { ["type"] = "boolean" },
                                ["alternatives"] = StringArraySchema(6, 120)
                            },
                            ["required"] = new JsonArray(
                                "id",
                                "purpose",
                                "required",
                                "alternatives")
                        },
                        ["maxItems"] = 8
                    },
                    ["requestedCapability"] = StringSchema(120),
                    ["capabilityReason"] = StringSchema(600),
                    ["capabilityRequired"] = new JsonObject { ["type"] = "boolean" },
                    ["missingCriticalInputs"] = StringArraySchema(8, 500),
                    ["assumptions"] = StringArraySchema(8, 500),
                    ["result"] = StringSchema(4000),
                    ["sources"] = StringArraySchema(12, 800),
                    ["warnings"] = StringArraySchema(8, 500)
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
                    "requestedCapability",
                    "capabilityReason",
                    "capabilityRequired",
                    "missingCriticalInputs",
                    "assumptions",
                    "result",
                    "sources",
                    "warnings")
            }
        }
    };

    private static JsonObject StringSchema(int maximumLength = 2000) => new()
    {
        ["type"] = "string",
        ["maxLength"] = maximumLength
    };

    private static JsonObject StringArraySchema(int maximumItems, int maximumLength) => new()
    {
        ["type"] = "array",
        ["items"] = StringSchema(maximumLength),
        ["maxItems"] = maximumItems
    };
}
