using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutorToolGateway
{
    private static readonly HashSet<string> ImageFileTools =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "session_image_inspect_pixels",
            "session_image_inspect_extended",
            "session_image_describe",
            "session_image_extract_text",
            "session_image_transform"
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ToolGateway _sharedGateway = new();
    private readonly SessionFileToolService _fileTools = new();
    private readonly SpecialistComponentToolService _specialistTools = new();
    private readonly SemanticImageToolService _semanticImageTools = new();

    public async Task<ExecutorToolExecution> ExecuteAsync(
        StructuredToolCall toolCall,
        StorageSettings storageSettings,
        SessionFileManifest fileManifest,
        string languageCode,
        ISessionEventLog sessionLog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(toolCall);
        var name = toolCall.Function.Name.Trim();
        if (ExecutorToolCatalog.IsWebTool(name))
        {
            try
            {
                var command = ScenarioToolCatalog.BuildCommand(toolCall);
                var content = await _sharedGateway.ExecuteAsync(
                    command,
                    storageSettings,
                    sessionLog,
                    cancellationToken);
                return new ExecutorToolExecution(command, content, Success: true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return CreateSafeError(
                    name,
                    new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase),
                    name,
                    "invalid_web_tool_call",
                    "The web tool call was malformed and could not be executed.",
                    ex.GetType().Name,
                    sessionLog);
            }
        }

        if (!ExecutorToolCatalog.IsSessionFileTool(name)
            && !ExecutorToolCatalog.IsAdapterTool(name))
        {
            return CreateSafeError(
                name,
                new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase),
                string.IsNullOrWhiteSpace(name) ? "unknown_tool" : name,
                "tool_not_allowed",
                "The requested tool is not exposed by AI HUB.",
                nameof(InvalidOperationException),
                sessionLog);
        }

        Dictionary<string, JsonElement> safeArguments = [];
        var commandDescription = name;
        try
        {
            safeArguments = ParseArguments(toolCall.Function.Arguments);
            RepairUniqueImageFileId(
                name,
                safeArguments,
                toolCall,
                fileManifest,
                sessionLog);
            commandDescription = BuildSafeCommandDescription(name, safeArguments);
            sessionLog.Write("tool_request", new
            {
                Tool = name,
                Arguments = safeArguments
            });
            cancellationToken.ThrowIfCancellationRequested();
            var content = name switch
            {
                "session_files_list" => _fileTools.ListFiles(fileManifest),
                "session_file_inspect" => _fileTools.Inspect(
                    fileManifest,
                    GetRequiredString(safeArguments, "file_id")),
                "session_file_read" => _fileTools.Read(
                    fileManifest,
                    GetRequiredString(safeArguments, "file_id"),
                    GetOptionalInteger(safeArguments, "offset", 0),
                    GetOptionalInteger(
                        safeArguments,
                        "max_chars",
                        SessionFileToolService.DefaultReturnedCharacters),
                    cancellationToken),
                "session_image_inspect_pixels" => _specialistTools.InspectImagePixels(
                    fileManifest,
                    GetRequiredString(safeArguments, "file_id")),
                "session_image_inspect_extended" => await _specialistTools.InspectImageExtendedAsync(
                    fileManifest,
                    GetRequiredString(safeArguments, "file_id"),
                    cancellationToken),
                "session_image_describe" => await _semanticImageTools.DescribeAsync(
                    fileManifest,
                    GetRequiredString(safeArguments, "file_id"),
                    GetOptionalString(safeArguments, "prompt", string.Empty),
                    languageCode,
                    cancellationToken),
                "session_image_extract_text" => await _specialistTools.ExtractImageTextAsync(
                    fileManifest,
                    GetRequiredString(safeArguments, "file_id"),
                    GetOptionalString(safeArguments, "language", "eng"),
                    cancellationToken),
                "session_image_transform" => await _specialistTools.TransformImageAsync(
                    fileManifest,
                    GetRequiredString(safeArguments, "file_id"),
                    storageSettings,
                    GetOptionalString(safeArguments, "output_format", "png"),
                    GetOptionalNullableInteger(safeArguments, "width"),
                    GetOptionalNullableInteger(safeArguments, "height"),
                    GetOptionalString(safeArguments, "fit", "contain"),
                    GetOptionalBoolean(safeArguments, "strip_metadata", false),
                    cancellationToken),
                "session_audio_transcribe" => await _specialistTools.TranscribeAudioAsync(
                    fileManifest,
                    GetRequiredString(safeArguments, "file_id"),
                    GetOptionalString(safeArguments, "language", "auto"),
                    cancellationToken),
                _ => throw new InvalidOperationException($"Executor tool is not allowed: {name}")
            };
            sessionLog.Write("tool_result", new
            {
                Tool = name,
                Arguments = safeArguments,
                ResultCharacters = content.Length
            });
            return new ExecutorToolExecution(commandDescription, content, Success: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SessionFileToolException ex)
        {
            return CreateSafeError(
                name,
                safeArguments,
                commandDescription,
                ex.Code,
                ex.SafeMessage,
                ex.GetType().Name,
                sessionLog,
                ex.DiagnosticMessage);
        }
        catch (JsonException ex)
        {
            return CreateSafeError(
                name,
                safeArguments,
                commandDescription,
                "invalid_arguments",
                "The tool arguments are not valid JSON.",
                ex.GetType().Name,
                sessionLog);
        }
        catch (Exception ex)
        {
            return CreateSafeError(
                name,
                safeArguments,
                commandDescription,
                "tool_failed",
                "The file tool could not safely complete this request.",
                ex.GetType().Name,
                sessionLog);
        }
    }

    private static ExecutorToolExecution CreateSafeError(
        string toolName,
        IReadOnlyDictionary<string, JsonElement> arguments,
        string commandDescription,
        string code,
        string safeMessage,
        string errorType,
        ISessionEventLog sessionLog,
        string diagnosticMessage = "")
    {
        sessionLog.Write("tool_error", new
        {
            Tool = toolName,
            Arguments = arguments,
            Code = code,
            ErrorType = errorType,
            Diagnostic = string.IsNullOrWhiteSpace(diagnosticMessage)
                ? null
                : diagnosticMessage
        });
        var content = JsonSerializer.Serialize(new
        {
            success = false,
            error_code = code,
            message = safeMessage,
            instruction = "Do not claim that the file was read. Use another available tool, request a capability, or ask the user for a safe fallback."
        }, JsonOptions);
        return new ExecutorToolExecution(commandDescription, content, Success: false);
    }

    private static void RepairUniqueImageFileId(
        string toolName,
        Dictionary<string, JsonElement> arguments,
        StructuredToolCall toolCall,
        SessionFileManifest manifest,
        ISessionEventLog sessionLog)
    {
        if (!ImageFileTools.Contains(toolName)
            || !arguments.TryGetValue("file_id", out var fileIdValue)
            || fileIdValue.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(fileIdValue.GetString()))
        {
            return;
        }

        var requestedFileId = fileIdValue.GetString()!.Trim();
        if (manifest.Files.Any(file =>
            string.Equals(file.Id, requestedFileId, StringComparison.Ordinal)))
        {
            return;
        }

        var availableImages = manifest.Files
            .Where(file => file.IsAvailable)
            .Where(file => string.Equals(
                file.Category,
                SessionFileCategories.Image,
                StringComparison.OrdinalIgnoreCase))
            .Where(file => File.Exists(file.SourcePath))
            .ToList();
        if (availableImages.Count != 1)
        {
            return;
        }

        var effectiveFileId = availableImages[0].Id;
        arguments["file_id"] = JsonSerializer.SerializeToElement(effectiveFileId);
        toolCall.Function.Arguments = JsonSerializer.Serialize(arguments);
        sessionLog.Write("tool_argument_repaired", new
        {
            Tool = toolName,
            Argument = "file_id",
            RequestedFileId = requestedFileId,
            EffectiveFileId = effectiveFileId,
            Reason = "single_available_image"
        });
    }

    private static Dictionary<string, JsonElement> ParseArguments(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return [];
        }

        using var document = JsonDocument.Parse(arguments);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Tool arguments must be an object.");
        }

        return document.RootElement.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.Clone(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string GetRequiredString(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string name)
    {
        if (!arguments.TryGetValue(name, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new SessionFileToolException(
                "missing_argument",
                $"The required argument '{name}' is missing.");
        }

        return value.GetString()!.Trim();
    }

    private static int GetOptionalInteger(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string name,
        int fallback)
    {
        if (!arguments.TryGetValue(name, out var value))
        {
            return fallback;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
        {
            throw new SessionFileToolException(
                "invalid_argument",
                $"The argument '{name}' must be an integer.");
        }

        return result;
    }

    private static int? GetOptionalNullableInteger(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string name)
    {
        if (!arguments.TryGetValue(name, out var value)
            || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
        {
            throw new SessionFileToolException(
                "invalid_argument",
                $"The argument '{name}' must be an integer.");
        }

        return result;
    }

    private static bool GetOptionalBoolean(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string name,
        bool fallback)
    {
        if (!arguments.TryGetValue(name, out var value))
        {
            return fallback;
        }

        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            throw new SessionFileToolException(
                "invalid_argument",
                $"The argument '{name}' must be a boolean.");
        }

        return value.GetBoolean();
    }

    private static string GetOptionalString(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string name,
        string fallback)
    {
        if (!arguments.TryGetValue(name, out var value))
        {
            return fallback;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new SessionFileToolException(
                "invalid_argument",
                $"The argument '{name}' must be a string.");
        }

        return string.IsNullOrWhiteSpace(value.GetString())
            ? fallback
            : value.GetString()!.Trim();
    }

    private static string BuildSafeCommandDescription(
        string toolName,
        IReadOnlyDictionary<string, JsonElement> arguments)
    {
        var parts = arguments
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}");
        var suffix = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(suffix)
            ? toolName
            : $"{toolName}: {suffix}";
    }
}

public sealed record ExecutorToolExecution(
    string Command,
    string Content,
    bool Success);
