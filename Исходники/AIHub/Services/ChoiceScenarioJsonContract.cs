using System.Text.Json.Nodes;
using AIHub.Models;

namespace AIHub.Services;

public static class ChoiceScenarioJsonContract
{
    public static JsonObject CreateResponseFormat()
    {
        return new JsonObject
        {
            ["type"] = "json_schema",
            ["json_schema"] = new JsonObject
            {
                ["name"] = "uncertainty_scenario_step",
                ["strict"] = true,
                ["schema"] = CreateSchema()
            }
        };
    }

    private static JsonObject CreateSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["stepType"] = StringEnum("question_step", "final_task_card"),
                ["question"] = String(),
                ["coreThought"] = String(),
                ["decisionDimension"] = StringEnum(["", .. ChoiceDecisionDimensions.All]),
                ["selectionImpact"] = StringEnumArray(ChoiceSelectionImpacts.All),
                ["profileUpdate"] = CapabilityDimensionsArray(),
                ["revisitReason"] = String(),
                ["options"] = new JsonObject
                {
                    ["type"] = "array",
                    ["maxItems"] = 6,
                    ["items"] = CreateOptionSchema()
                },
                ["allowCustom"] = Boolean(),
                ["isFinal"] = Boolean(),
                ["summaryLines"] = StringArray(),
                ["taskCard"] = new JsonObject
                {
                    ["anyOf"] = new JsonArray(CreateTaskCardSchema(), new JsonObject { ["type"] = "null" })
                }
            },
            ["required"] = new JsonArray("stepType", "question", "coreThought", "decisionDimension", "selectionImpact", "profileUpdate", "revisitReason", "options", "allowCustom", "isFinal", "summaryLines")
        };
    }

    private static JsonObject CreateOptionSchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["properties"] = new JsonObject
        {
            ["id"] = String(),
            ["title"] = new JsonObject { ["type"] = "string", ["maxLength"] = 32 },
            ["description"] = new JsonObject { ["type"] = "string", ["maxLength"] = 140 },
            ["isRecommended"] = Boolean(),
            ["recommendationReason"] = new JsonObject { ["type"] = "string", ["maxLength"] = 120 }
            ,
            ["profileEffects"] = CapabilityDimensionsArray(minimumItems: 1)
        },
        ["required"] = new JsonArray("id", "title", "description", "isRecommended", "recommendationReason", "profileEffects")
    };

    private static JsonObject CreateTaskCardSchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["properties"] = new JsonObject
        {
            ["goal"] = String(),
            ["area"] = String(),
            ["criteria"] = StringArray(),
            ["constraints"] = StringArray(),
            ["capabilityProfile"] = CreateCapabilityProfileSchema(),
            ["executorSelection"] = CreateExecutorSelectionSchema(),
            ["promptForExecutor"] = String()
        },
        ["required"] = new JsonArray("goal", "area", "criteria", "constraints", "capabilityProfile", "executorSelection", "promptForExecutor")
    };

    private static JsonObject CreateExecutorSelectionSchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["properties"] = new JsonObject
        {
            ["installedCandidateId"] = String(),
            ["alternativeCandidateId"] = String(),
            ["preferredCandidateId"] = String()
        },
        ["required"] = new JsonArray(
            "installedCandidateId",
            "alternativeCandidateId",
            "preferredCandidateId")
    };

    private static JsonObject CreateCapabilityProfileSchema() => new()
    {
        ["type"] = "object",
        ["additionalProperties"] = false,
        ["properties"] = new JsonObject
        {
            ["dimensions"] = CapabilityDimensionsArray(minimumItems: 1)
        },
        ["required"] = new JsonArray("dimensions")
    };

    private static JsonObject CapabilityDimensionsArray(int minimumItems = 0) => new()
    {
        ["type"] = "array",
        ["minItems"] = minimumItems,
        ["items"] = new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["dimension"] = StringEnum([.. ChoiceDecisionDimensions.All]),
                ["status"] = StringEnum([.. ChoiceDimensionStatuses.All]),
                ["values"] = new JsonObject
                {
                    ["type"] = "array",
                    ["maxItems"] = 8,
                    ["items"] = new JsonObject { ["type"] = "string", ["maxLength"] = 80 }
                },
                ["evidence"] = new JsonObject { ["type"] = "string", ["maxLength"] = 200 }
            },
            ["required"] = new JsonArray("dimension", "status", "values", "evidence")
        }
    };

    private static JsonObject String() => new() { ["type"] = "string" };

    private static JsonObject Boolean() => new() { ["type"] = "boolean" };

    private static JsonObject StringArray() => new()
    {
        ["type"] = "array",
        ["items"] = String()
    };

    private static JsonObject StringEnumArray(IEnumerable<string> values) => new()
    {
        ["type"] = "array",
        ["items"] = StringEnum([.. values])
    };

    private static JsonObject StringEnum(params string[] values) => new()
    {
        ["type"] = "string",
        ["enum"] = new JsonArray(values.Select(value => JsonValue.Create(value)).ToArray())
    };
}
