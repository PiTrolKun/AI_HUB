using System.Globalization;
using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ToolGateway
{
    private readonly WebSearchTool _webSearchTool = new();
    private readonly WebPageReaderTool _webPageReaderTool = new();
    private readonly WebDownloadTool _webDownloadTool = new();

    public bool IsToolCommand(string prompt)
    {
        return prompt.TrimStart().StartsWith("web_search:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("web_read:", StringComparison.OrdinalIgnoreCase)
            || prompt.TrimStart().StartsWith("web_download:", StringComparison.OrdinalIgnoreCase);
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
                "web_read" => await ExecuteReadAsync(argument, storageSettings, cancellationToken),
                "web_download" => await ExecuteDownloadAsync(argument, storageSettings, cancellationToken, downloadProgress),
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
}
