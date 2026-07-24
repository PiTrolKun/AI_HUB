using System.Text.Json.Nodes;
using AIHub.Models;

namespace AIHub.Services;

public static class ExecutorToolCatalog
{
    private static readonly string[] WebToolNames =
    [
        "web_search",
        "web_research",
        "web_read"
    ];

    private static readonly string[] SessionFileToolNames =
    [
        "session_files_list",
        "session_file_inspect",
        "session_file_read"
    ];

    public static List<StructuredToolDefinition> CreateDefinitions(
        bool includeWeb,
        bool includeSessionFiles)
    {
        var definitions = new List<StructuredToolDefinition>();
        if (includeWeb)
        {
            definitions.AddRange(ScenarioToolCatalog.CreateDefinitions()
                .Where(tool => WebToolNames.Contains(
                    tool.Function.Name,
                    StringComparer.Ordinal)));
        }

        if (includeSessionFiles)
        {
            definitions.Add(CreateSessionFilesListTool());
            definitions.Add(CreateSessionFileInspectTool());
            definitions.Add(CreateSessionFileReadTool());
        }

        return definitions;
    }

    public static bool IsWebTool(string name) =>
        WebToolNames.Contains(name, StringComparer.Ordinal);

    public static bool IsSessionFileTool(string name) =>
        SessionFileToolNames.Contains(name, StringComparer.Ordinal);

    private static StructuredToolDefinition CreateSessionFilesListTool() =>
        CreateTool(
            "session_files_list",
            "List files explicitly attached to this AI HUB session. Returns safe IDs and metadata, never absolute paths.",
            new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = new JsonObject()
            });

    private static StructuredToolDefinition CreateSessionFileInspectTool() =>
        CreateTool(
            "session_file_inspect",
            "Inspect one attached file by its safe session file ID before reading it. Returns verified metadata and available read mode, but never an absolute path.",
            new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = new JsonObject
                {
                    ["file_id"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Exact file ID returned by session_files_list or the trusted file manifest."
                    }
                },
                ["required"] = new JsonArray("file_id")
            });

    private static StructuredToolDefinition CreateSessionFileReadTool() =>
        CreateTool(
            "session_file_read",
            "Read a bounded text representation of one supported attached file by safe ID. Use next_offset to continue large files. Images, audio, and video are not semantically understood by this tool.",
            new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = new JsonObject
                {
                    ["file_id"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Exact file ID returned by session_files_list or the trusted file manifest."
                    },
                    ["offset"] = new JsonObject
                    {
                        ["type"] = "integer",
                        ["minimum"] = 0,
                        ["maximum"] = SessionFileToolService.MaximumOffset,
                        ["description"] = "Character offset in the safe extracted representation. Start with 0."
                    },
                    ["max_chars"] = new JsonObject
                    {
                        ["type"] = "integer",
                        ["minimum"] = 1,
                        ["maximum"] = SessionFileToolService.MaximumReturnedCharacters,
                        ["description"] = "Maximum characters to return in this call."
                    }
                },
                ["required"] = new JsonArray("file_id")
            });

    private static StructuredToolDefinition CreateTool(
        string name,
        string description,
        JsonObject parameters) =>
        new()
        {
            Function = new StructuredToolFunction
            {
                Name = name,
                Description = description,
                Parameters = parameters
            }
        };
}
