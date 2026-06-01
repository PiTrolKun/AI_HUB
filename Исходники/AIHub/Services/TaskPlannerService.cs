using AIHub.Models;

namespace AIHub.Services;

public sealed class TaskPlannerService
{
    private readonly CapabilityInventoryService _inventoryService = new();

    public TaskPlanResponse Plan(string task, StorageSettings storageSettings)
    {
        var inventory = _inventoryService.Create(storageSettings);
        var text = task.ToLowerInvariant();
        var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "core" };
        var notes = new List<string>();
        var taskType = "general";

        if (ContainsAny(text, "интернет", "найди", "поиск", "новост", "актуаль", "сайт", "web", "search", "news"))
        {
            taskType = "web_research";
            required.Add("web_search");
            required.Add("web_read");
            required.Add("reranker");
            notes.Add("For current facts, search must be followed by reading selected pages.");
        }

        if (ContainsAny(text, "скач", "download", "загруз"))
        {
            taskType = "download";
            required.Add("web_search");
            required.Add("web_download");
            notes.Add("Download success requires a direct file URL and a Web download complete result.");
        }

        if (ContainsAny(text, "документ", "rag", "embedding", "семантическ", "поиск по документ"))
        {
            taskType = "document_rag";
            required.Add("embedding");
            notes.Add("Document/RAG search needs an embedding model before full implementation.");
        }

        if (ContainsAny(text, "модель", "hugging face", "hf_", "подбери модель"))
        {
            taskType = "model_selection";
            required.Add("hf_provider");
            notes.Add("Model selection should use structured Hugging Face facts, not general web pages.");
        }

        var installedRoles = inventory.Items
            .Where(item => item.IsInstalled)
            .Select(item => item.Role)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingRoles = required
            .Where(role => !installedRoles.Contains(role))
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new TaskPlanResponse
        {
            Task = task,
            TaskType = taskType,
            RequiredRoles = required.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToList(),
            InstalledRoles = required.Where(installedRoles.Contains).OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToList(),
            MissingRoles = missingRoles,
            CanContinueWithoutDownload = missingRoles.Count == 0 || missingRoles.All(role => string.Equals(role, "reranker", StringComparison.OrdinalIgnoreCase)),
            NextAction = BuildNextAction(taskType, missingRoles),
            Notes = notes
        };
    }

    private static string BuildNextAction(string taskType, IReadOnlyList<string> missingRoles)
    {
        if (missingRoles.Count > 0 && missingRoles.Any(role => !string.Equals(role, "reranker", StringComparison.OrdinalIgnoreCase)))
        {
            return "Find or install missing roles before the full scenario can run.";
        }

        return taskType switch
        {
            "web_research" => "Run web_search, then web_read on the best results, then summarize only read facts.",
            "download" => "Find a direct file URL, then run web_download and verify the saved file.",
            "document_rag" => "Use hf_find_model to choose an embedding model candidate.",
            "model_selection" => "Use hf_find_model and hf_model_files for structured model candidates.",
            _ => "The core can answer directly or ask for a more specific scenario."
        };
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(needle => text.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}
