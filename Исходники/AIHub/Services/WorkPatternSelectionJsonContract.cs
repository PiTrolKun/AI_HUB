using System.Text.Json.Nodes;

namespace AIHub.Services;

public static class WorkPatternSelectionJsonContract
{
    public static JsonObject CreateResponseFormat() => new()
    {
        ["type"] = "json_schema",
        ["json_schema"] = new JsonObject
        {
            ["name"] = "sandbox_work_pattern_selection",
            ["strict"] = true,
            ["schema"] = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = new JsonObject
                {
                    ["selections"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["minItems"] = 1,
                        ["maxItems"] = 6,
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["properties"] = new JsonObject
                            {
                                ["patternId"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["maxLength"] = 80
                                },
                                ["matchPercent"] = new JsonObject
                                {
                                    ["type"] = "integer",
                                    ["minimum"] = 0,
                                    ["maximum"] = 100
                                },
                                ["reason"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["maxLength"] = 240
                                }
                            },
                            ["required"] = new JsonArray(
                                "patternId",
                                "matchPercent",
                                "reason")
                        }
                    },
                    ["missingData"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["maxItems"] = 8,
                        ["items"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["maxLength"] = 160
                        }
                    },
                    ["source"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["enum"] = new JsonArray("core")
                    },
                    ["usedFallback"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["enum"] = new JsonArray(false)
                    }
                },
                ["required"] = new JsonArray(
                    "selections",
                    "missingData",
                    "source",
                    "usedFallback")
            }
        }
    };
}
