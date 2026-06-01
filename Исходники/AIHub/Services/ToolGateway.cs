using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ToolGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly WebSearchTool _webSearchTool = new();
    private readonly WebPageReaderTool _webPageReaderTool = new();
    private readonly WebDownloadTool _webDownloadTool = new();
    private readonly CapabilityInventoryService _inventoryService = new();
    private readonly TaskPlannerService _taskPlannerService = new();
    private readonly HuggingFaceProviderTool _huggingFaceProviderTool = new();
    private readonly SearchStrategyService _searchStrategyService = new();
    private readonly SessionLogReaderService _sessionLogReaderService = new();

    public bool IsToolCommand(string prompt)
    {
        return prompt.TrimStart().StartsWith("web_search:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("web_research:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("web_read:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("web_download:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("inventory:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("task_plan:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("session_log:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("hf_find_model:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("hf_model_files:", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExecuteAsync(
        string prompt,
        StorageSettings storageSettings,
        JsonlSessionLog sessionLog,
        CancellationToken cancellationToken,
        IProgress<WebDownloadProgress>? downloadProgress = null)
    {
        var trimmed = prompt.Trim();
        var separator = trimmed.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0 || separator == trimmed.Length - 1)
        {
            throw new InvalidOperationException("Tool command must look like web_search: query");
        }

        var command = trimmed[..separator].Trim();
        var argument = trimmed[(separator + 1)..].Trim();
        sessionLog.Write("tool_request", new { Tool = command, Argument = argument });

        try
        {
            var result = command.ToLowerInvariant() switch
            {
                "web_search" => await ExecuteSearchAsync(argument, storageSettings, cancellationToken),
                "web_research" => await ExecuteResearchAsync(argument, storageSettings, cancellationToken),
                "web_read" => await ExecuteReadAsync(argument, storageSettings, cancellationToken),
                "web_download" => await ExecuteDownloadAsync(argument, storageSettings, cancellationToken, downloadProgress),
                "inventory" => ExecuteInventory(storageSettings),
                "task_plan" => ExecuteTaskPlan(argument, storageSettings),
                "session_log" => ExecuteSessionLog(argument, sessionLog),
                "hf_find_model" => await ExecuteHfFindModelAsync(argument, storageSettings, cancellationToken),
                "hf_model_files" => await ExecuteHfModelFilesAsync(argument, storageSettings, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown tool command: {command}")
            };

            sessionLog.Write("tool_result", new { Tool = command, Argument = argument, Result = result });
            return result;
        }
        catch (Exception ex)
        {
            sessionLog.Write("tool_error", new { Tool = command, Argument = argument, Error = ex.Message });
            return string.Join(
                Environment.NewLine,
                "Tool error.",
                $"Tool: {command}",
                $"Argument: {argument}",
                $"Error: {ex.Message}",
                "Hint: try another source, another URL, or a broader search query.");
        }
    }

    private async Task<string> ExecuteSearchAsync(
        string query,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var result = await _webSearchTool.SearchAsync(query, storageSettings, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Web search: {result.Query}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Provider: {result.Provider}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Search status: {result.Status}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Results found: {result.ResultCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"HTTP status: {result.HttpStatusCode}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Possible reason: {result.PossibleReason}");
        if (result.RecommendedNextSteps.Count > 0)
        {
            builder.AppendLine("Recommended next steps:");
            foreach (var step in result.RecommendedNextSteps)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {step}");
            }
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"Rerank: {result.Rerank.Mode} | {result.Rerank.Message}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Saved: {result.SavedPath}");

        for (var index = 0; index < result.Results.Count; index++)
        {
            var item = result.Results[index];
            var scoreText = item.RerankScore is null
                ? "n/a"
                : item.RerankScore.Value.ToString("0.000", CultureInfo.InvariantCulture);
            builder.AppendLine(CultureInfo.InvariantCulture, $"{index + 1}. {item.Title}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"Rank: original {item.OriginalRank}, reranked {item.RerankedRank}, score {scoreText}");
            builder.AppendLine(item.Url);
            if (!string.IsNullOrWhiteSpace(item.Snippet))
            {
                builder.AppendLine(item.Snippet);
            }
        }

        return builder.ToString().Trim();
    }

    private async Task<string> ExecuteResearchAsync(
        string task,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var result = await _searchStrategyService.ResearchAsync(task, storageSettings, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Web research: {result.Task}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Research status: {result.Status}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Diagnosis: {result.Diagnosis}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Confirmed sources: {result.ConfirmedSourceCount}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Saved: {result.SavedPath}");

        builder.AppendLine("Generated queries:");
        foreach (var query in result.GeneratedQueries)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {query}");
        }

        builder.AppendLine("Search attempts:");
        foreach (var attempt in result.Attempts)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {attempt.Query}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"  provider: {attempt.Provider}; status: {attempt.Status}; results: {attempt.ResultCount}; http: {attempt.HttpStatusCode}; reason: {attempt.PossibleReason}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"  saved: {attempt.SavedPath}");
        }

        if (result.Sources.Count > 0)
        {
            builder.AppendLine("Candidate sources:");
            foreach (var source in result.Sources.Take(10))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {source.Title}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"  url: {source.Url}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"  provider: {source.Provider}; query: {source.Query}; read: {source.WasRead}");
                if (!string.IsNullOrWhiteSpace(source.Snippet))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"  snippet: {source.Snippet}");
                }
            }
        }

        if (result.DatedItems.Count > 0)
        {
            builder.AppendLine("Dated items:");
            foreach (var item in result.DatedItems.Take(12))
            {
                var dateLabel = string.IsNullOrWhiteSpace(item.DateIso)
                    ? item.DateText
                    : $"{item.DateText} ({item.DateIso})";
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {dateLabel} | {item.SourceTitle}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"  url: {item.SourceUrl}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"  text: {LimitForToolOutput(item.Text, 650)}");
            }
        }

        if (result.ReadPages.Count > 0)
        {
            builder.AppendLine("Read pages:");
            foreach (var page in result.ReadPages)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {page.Title}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"  url: {page.Url}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"  saved: {page.SavedPath}");
                if (!string.IsNullOrWhiteSpace(page.Error))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"  error: {page.Error}");
                }

                if (!string.IsNullOrWhiteSpace(page.TextPreview))
                {
                    builder.AppendLine("  preview:");
                    builder.AppendLine(LimitForToolOutput(page.TextPreview, 900));
                }
            }
        }

        if (result.RecommendedNextSteps.Count > 0)
        {
            builder.AppendLine("Recommended next steps:");
            foreach (var step in result.RecommendedNextSteps)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {step}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("JSON:");
        builder.AppendLine(JsonSerializer.Serialize(result, JsonOptions));
        return builder.ToString().Trim();
    }

    private string ExecuteInventory(StorageSettings storageSettings)
    {
        var inventory = _inventoryService.Create(storageSettings);
        var builder = new StringBuilder();
        builder.AppendLine("Capability inventory:");
        foreach (var item in inventory.Items)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {item.Role}: {item.Status}; installed={item.IsInstalled}; runnable={item.IsRunnable}; format={item.Format}");
            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  name: {item.Name}");
            }

            if (!string.IsNullOrWhiteSpace(item.Path))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  path: {item.Path}");
            }

            if (!string.IsNullOrWhiteSpace(item.Details))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  details: {item.Details}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("JSON:");
        builder.AppendLine(JsonSerializer.Serialize(inventory, JsonOptions));
        return builder.ToString().Trim();
    }

    private string ExecuteTaskPlan(string task, StorageSettings storageSettings)
    {
        var plan = _taskPlannerService.Plan(task, storageSettings);
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Task plan: {plan.Task}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Task type: {plan.TaskType}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Required roles: {string.Join(", ", plan.RequiredRoles)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Installed roles: {string.Join(", ", plan.InstalledRoles)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Missing roles: {(plan.MissingRoles.Count == 0 ? "none" : string.Join(", ", plan.MissingRoles))}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Can continue without download: {plan.CanContinueWithoutDownload}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Next action: {plan.NextAction}");
        if (plan.Notes.Count > 0)
        {
            builder.AppendLine("Notes:");
            foreach (var note in plan.Notes)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {note}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("JSON:");
        builder.AppendLine(JsonSerializer.Serialize(plan, JsonOptions));
        return builder.ToString().Trim();
    }

    private string ExecuteSessionLog(string request, JsonlSessionLog sessionLog)
    {
        return _sessionLogReaderService.Read(sessionLog.FilePath, request);
    }

    private async Task<string> ExecuteHfFindModelAsync(
        string argument,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var result = await _huggingFaceProviderTool.FindModelAsync(argument, storageSettings, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Hugging Face model search: role={result.Role}; query={result.Query}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Requested format: {EmptyAsNone(result.Format)}; license: {EmptyAsNone(result.License)}; max size: {FormatBytes(result.MaxSizeBytes)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Candidates found: {result.Candidates.Count}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Saved: {result.SavedPath}");
        foreach (var candidate in result.Candidates)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {candidate.RepoId}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"  pipeline: {EmptyAsNone(candidate.PipelineTag)}; license: {EmptyAsNone(candidate.License)}; downloads: {candidate.Downloads?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}; likes: {candidate.Likes?.ToString(CultureInfo.InvariantCulture) ?? "n/a"}");
            foreach (var file in candidate.Files.Take(5))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  file: {file.FileName}; size: {FormatBytes(file.SizeBytes)}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"  url: {file.DownloadUrl}");
            }

            foreach (var warning in candidate.Warnings)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  warning: {warning}");
            }
        }

        return builder.ToString().Trim();
    }

    private async Task<string> ExecuteHfModelFilesAsync(
        string repoId,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var result = await _huggingFaceProviderTool.GetModelFilesAsync(repoId, storageSettings, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Hugging Face model files: {result.RepoId}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"License: {EmptyAsNone(result.License)}; pipeline: {EmptyAsNone(result.PipelineTag)}");
        foreach (var file in result.Files)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {file.FileName}; size: {FormatBytes(file.SizeBytes)}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"  url: {file.DownloadUrl}");
        }

        foreach (var warning in result.Warnings)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"Warning: {warning}");
        }

        return builder.ToString().Trim();
    }

    private async Task<string> ExecuteReadAsync(
        string url,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var result = await _webPageReaderTool.ReadAsync(url, storageSettings, cancellationToken);
        return string.Join(
            Environment.NewLine,
            $"Web page: {result.Title}",
            result.Url,
            $"Saved: {result.SavedPath}",
            result.CandidateFileUrls.Count == 0
                ? "Candidate direct file URLs: none found."
                : "Candidate direct file URLs:" + Environment.NewLine + string.Join(Environment.NewLine, result.CandidateFileUrls),
            string.Empty,
            result.Text);
    }

    private async Task<string> ExecuteDownloadAsync(
        string url,
        StorageSettings storageSettings,
        CancellationToken cancellationToken,
        IProgress<WebDownloadProgress>? downloadProgress)
    {
        var result = await _webDownloadTool.DownloadAsync(url, storageSettings, cancellationToken, downloadProgress);
        var builder = new StringBuilder();
        builder.AppendLine("Web download complete.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"URL: {result.Url}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"File: {result.FilePath}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Size: {result.SizeBytes} bytes");
        builder.AppendLine(CultureInfo.InvariantCulture, $"SHA-256: {result.Sha256}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Content-Type: {result.ContentType}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Content-Kind: {result.ContentKind}");
        if (result.ExtensionWasAdded)
        {
            builder.AppendLine("File extension was added from Content-Type.");
        }

        if (!string.IsNullOrWhiteSpace(result.Warning))
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"Warning: {result.Warning}");
        }

        return builder.ToString().Trim();
    }

    private static string EmptyAsNone(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value;

    private static string LimitForToolOutput(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength] + Environment.NewLine + "...";
    }

    private static string FormatBytes(long? bytes)
    {
        if (bytes is null)
        {
            return "n/a";
        }

        var value = (double)bytes.Value;
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return value.ToString(unit == 0 ? "0" : "0.##", CultureInfo.InvariantCulture) + " " + units[unit];
    }
}
