using System.IO;
using AIHub.Models;

namespace AIHub.Services;

public sealed class CapabilityInventoryService
{
    private readonly CoreModelManager _coreModelManager = new();
    private readonly ToolModelManager _toolModelManager = new();
    private readonly DebugModelDiscoveryService _modelDiscoveryService = new();
    private readonly ComponentManager _componentManager = new();

    public CapabilityInventoryResponse Create(StorageSettings storageSettings)
    {
        var items = new List<CapabilityInventoryItem>
        {
            CreateCoreItem(storageSettings),
            CreateStaticTool("web_search", "AI HUB Web Search", true, "DuckDuckGo Lite + diagnostics"),
            CreateStaticTool("web_read", "AI HUB Web Page Reader", true, "HTML text extractor"),
            CreateStaticTool("web_download", "AI HUB Web Download", true, "Direct URL downloader"),
            CreateStaticTool("hf_provider", "Hugging Face API provider", true, "Model search through Hugging Face API"),
            CreateBackendItem()
        };

        items.Add(CreateRerankerItem(storageSettings));
        items.AddRange(_modelDiscoveryService.Discover(storageSettings)
            .Where(model => string.Equals(model.Role, "executor", StringComparison.OrdinalIgnoreCase))
            .Select(model => new CapabilityInventoryItem
            {
                Role = "executor",
                Name = model.Name,
                Status = model.Status,
                IsInstalled = true,
                IsRunnable = model.IsRunnable,
                Format = model.Format,
                Path = model.Path,
                Source = "executor-model.json",
                Details = !string.IsNullOrWhiteSpace(model.SemanticDescriptionEn)
                    ? model.SemanticDescriptionEn
                    : "Installed model executor available for prepared tasks.",
                SemanticDescriptionRu = model.SemanticDescriptionRu,
                SemanticDescriptionEn = model.SemanticDescriptionEn
            }));
        items.Add(CreateMissingRole("embedding", "Embedding model", "Needed later for RAG and semantic document search."));
        items.AddRange(_componentManager.GetAvailableCapabilities()
            .Where(ComponentAdapterRegistry.IsCallable)
            .Select(capability => CreateStaticTool(
                "component_capability",
                capability,
                true,
                "Verified package plus trusted callable AI HUB adapter.")));

        return new CapabilityInventoryResponse
        {
            Items = NormalizeItems(items)
        };
    }

    public static List<CapabilityInventoryItem> NormalizeItems(
        IEnumerable<CapabilityInventoryItem> items) => items
        .GroupBy(
            item => $"{item.Role}|{(string.IsNullOrWhiteSpace(item.Path) ? item.Name : item.Path)}",
            StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .OrderByDescending(item => item.IsInstalled)
        .ThenBy(item => item.Role, StringComparer.OrdinalIgnoreCase)
        .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    private CapabilityInventoryItem CreateCoreItem(StorageSettings storageSettings)
    {
        var check = _coreModelManager.Check(storageSettings);
        var installed = check.Availability == CoreModelAvailability.Installed;
        return new CapabilityInventoryItem
        {
            Role = "core",
            Name = CoreModelManager.CoreModelDisplayName,
            Status = check.Availability.ToString(),
            IsInstalled = installed,
            IsRunnable = installed,
            Format = "gguf",
            Path = check.ModelPath ?? string.Empty,
            Source = "core-model.json",
            Details = installed ? "Main local dispatcher/chat model." : "Core model is not ready."
        };
    }

    private CapabilityInventoryItem CreateRerankerItem(StorageSettings storageSettings)
    {
        var directory = _toolModelManager.GetRerankerDirectory(storageSettings);
        var installed = !string.IsNullOrWhiteSpace(directory);
        return new CapabilityInventoryItem
        {
            Role = "reranker",
            Name = ToolModelManager.RerankerDisplayName,
            Status = installed ? "installed" : "missing",
            IsInstalled = installed,
            IsRunnable = installed,
            Format = "safetensors",
            Path = directory ?? string.Empty,
            Source = "tool-model.json",
            Details = installed ? "Improves ordering of web search results." : "Search works without it, but ranking is weaker."
        };
    }

    private static CapabilityInventoryItem CreateBackendItem()
    {
        var serverPath = LlamaBackendPaths.ServerExecutablePath;
        var cliPath = LlamaBackendPaths.CliExecutablePath;
        var installed = File.Exists(serverPath) || File.Exists(cliPath);
        return new CapabilityInventoryItem
        {
            Role = "llama_backend",
            Name = LlamaBackendPaths.DisplayName,
            Status = installed ? "installed" : "missing",
            IsInstalled = installed,
            IsRunnable = installed,
            Format = LlamaBackendPaths.Platform,
            Path = File.Exists(serverPath) ? serverPath : cliPath,
            Source = "Runtime/Backends",
            Details = installed ? "Local GGUF runtime backend." : "GGUF models cannot run without backend."
        };
    }

    private static CapabilityInventoryItem CreateStaticTool(string role, string name, bool runnable, string details)
    {
        return new CapabilityInventoryItem
        {
            Role = role,
            Name = name,
            Status = "available",
            IsInstalled = true,
            IsRunnable = runnable,
            Format = "tool",
            Source = "AI HUB",
            Details = details
        };
    }

    private static CapabilityInventoryItem CreateMissingRole(string role, string name, string details)
    {
        return new CapabilityInventoryItem
        {
            Role = role,
            Name = name,
            Status = "missing",
            IsInstalled = false,
            IsRunnable = false,
            Format = "model",
            Details = details
        };
    }
}
