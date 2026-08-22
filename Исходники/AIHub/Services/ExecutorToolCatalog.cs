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
        bool includeSessionFiles,
        IEnumerable<string>? adapterToolNames = null)
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

        foreach (var toolName in adapterToolNames?
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.Ordinal)
                 ?? [])
        {
            var definition = CreateAdapterTool(toolName);
            if (definition is not null)
            {
                definitions.Add(definition);
            }
        }

        return definitions;
    }

    public static bool IsWebTool(string name) =>
        WebToolNames.Contains(name, StringComparer.Ordinal);

    public static bool IsSessionFileTool(string name) =>
        SessionFileToolNames.Contains(name, StringComparer.Ordinal);

    public static bool IsAdapterTool(string name) =>
        ComponentAdapterRegistry.FindByToolName(name) is not null;

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

    private static StructuredToolDefinition? CreateAdapterTool(string name) =>
        name switch
        {
            "session_image_inspect_pixels" => CreateTool(
                name,
                "Inspect deterministic pixel properties of one attached image by safe file ID. Returns dimensions and bounded color statistics, not semantic vision.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["file_id"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Exact image file ID from session_files_list."
                        }
                    },
                    ["required"] = new JsonArray("file_id")
                }),
            "session_image_inspect_extended" => CreateTool(
                name,
                "Inspect verified extended metadata of one attached image with ImageMagick. This does not provide semantic vision.",
                CreateFileIdParameters("image")),
            "session_image_describe" => CreateTool(
                name,
                "Describe visible semantic content of one attached image with the installed and verified local vision model. Report uncertainty and never infer identity or invisible details.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["file_id"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Exact image file ID from session_files_list."
                        },
                        ["prompt"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Optional focused question about visible image content."
                        }
                    },
                    ["required"] = new JsonArray("file_id")
                }),
            "session_image_extract_text" => CreateTool(
                name,
                "Extract printed text from one attached image with the installed and verified Tesseract OCR runtime. This does not identify general objects or scene meaning.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["file_id"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Exact image file ID from session_files_list."
                        },
                        ["language"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Tesseract language code such as eng, rus, or eng+rus.",
                            ["default"] = "eng"
                        }
                    },
                    ["required"] = new JsonArray("file_id")
                }),
            "session_image_transform" => CreateTool(
                name,
                "Create a new image artifact from an attached image with ImageMagick. Supports safe conversion, resizing, fitting, and metadata removal; never overwrites the source.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["file_id"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Exact image file ID from session_files_list."
                        },
                        ["output_format"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray("png", "jpg", "webp", "tiff", "bmp"),
                            ["default"] = "png"
                        },
                        ["width"] = new JsonObject
                        {
                            ["type"] = "integer",
                            ["minimum"] = 1,
                            ["maximum"] = 32768
                        },
                        ["height"] = new JsonObject
                        {
                            ["type"] = "integer",
                            ["minimum"] = 1,
                            ["maximum"] = 32768
                        },
                        ["fit"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray("contain", "cover", "stretch"),
                            ["default"] = "contain"
                        },
                        ["strip_metadata"] = new JsonObject
                        {
                            ["type"] = "boolean",
                            ["default"] = false
                        }
                    },
                    ["required"] = new JsonArray("file_id")
                }),
            "session_audio_transcribe" => CreateTool(
                name,
                "Transcribe one attached audio file with the installed and verified local whisper.cpp runtime and model.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["additionalProperties"] = false,
                    ["properties"] = new JsonObject
                    {
                        ["file_id"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "Exact audio file ID from session_files_list."
                        },
                        ["language"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "ISO language code such as ru or en, or auto.",
                            ["default"] = "auto"
                        }
                    },
                    ["required"] = new JsonArray("file_id")
                }),
            _ => null
        };

    private static JsonObject CreateFileIdParameters(string category) =>
        new()
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["file_id"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = $"Exact {category} file ID from session_files_list."
                }
            },
            ["required"] = new JsonArray("file_id")
        };

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
