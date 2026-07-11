using System.Text.Json;
using AIHub.Models;
using AIHub.Services;

var options = ParseArguments(args);
var mode = ReadOption(options, "mode", "search");
var query = ReadOption(options, "query", "Qwen3-14B-GGUF");
var author = ReadOption(options, "author", string.Empty);
var projectRoot = FindProjectRoot(Environment.CurrentDirectory);
var seed = Path.GetFullPath(ReadOption(
    options,
    "seed",
    Path.Combine(projectRoot, "Каталоги", "huggingface-catalog-seed.json")));
var includeRadar = ReadOption(options, "radar", "true") is not "false" and not "0";
var output = Path.GetFullPath(ReadOption(
    options,
    "output",
    mode.Equals("sync", StringComparison.OrdinalIgnoreCase)
        ? Path.Combine(projectRoot, "Runtime", "Каталоги", "HuggingFace")
        : Path.Combine(Environment.CurrentDirectory, "CatalogProbe", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"))));
var limit = int.TryParse(ReadOption(options, "limit", "5"), out var parsedLimit)
    ? Math.Clamp(parsedLimit, 1, 50)
    : 5;

if (mode.Equals("local-search", StringComparison.OrdinalIgnoreCase))
{
    var catalogPath = Path.GetFullPath(ReadOption(
        options,
        "catalog",
        Path.Combine(projectRoot, "Runtime", "Каталоги", "HuggingFace", "catalog.json")));
    var request = new ModelCatalogSearchRequest
    {
        Directions = SplitList(ReadOption(options, "directions", string.Empty)),
        TaskType = ReadOption(options, "task", query),
        RequiredCapabilities = SplitList(ReadOption(options, "capabilities", string.Empty)),
        LoadLevel = ReadOption(options, "load", "any"),
        Limit = Math.Clamp(limit, 1, 6)
    };
    var currentCore = ReadOption(options, "core", string.Empty);
    var result = new LocalModelCatalogTool(catalogPath).Search(JsonSerializer.Serialize(request), currentCore);
    Console.WriteLine($"Status: {result.Status}; candidates: {result.Candidates.Count}; hardware rejected: {result.HardwareRejectedCount}; lineage rejected: {result.LineageRejectedCount}; live search required: {result.RequiresLiveSearch}");
    Console.WriteLine($"Catalog: {result.CatalogPath}; last sync: {result.LastSuccessfulSyncUtc?.ToString("O") ?? "none"}");
    foreach (var candidate in result.Candidates)
    {
        Console.WriteLine($"- {candidate.RepoId}; parameters={candidate.ParameterCount?.ToString() ?? "unknown"}; load={string.Join(',', candidate.LoadLevels)}; hardware={candidate.Hardware.Status}; q4_runtime_gb={candidate.Hardware.EstimatedQ4RuntimeGb?.ToString() ?? "unknown"}; source={candidate.Source}");
        Console.WriteLine($"  evidence: {string.Join("; ", candidate.MatchReasons)}");
    }
    return;
}

using var collector = new HuggingFaceCatalogCollector();
if (mode.Equals("sync", StringComparison.OrdinalIgnoreCase))
{
    var syncService = new HuggingFaceCatalogSyncService(collector);
    var result = await syncService.SynchronizeAsync(seed, output, includeRadar, CancellationToken.None);
    Console.WriteLine($"Catalog: {result.CatalogPath}");
    Console.WriteLine($"Changes: {result.ChangesPath}");
    Console.WriteLine($"Seed slots: {result.SeedSlotCount}");
    Console.WriteLine($"Tracked repositories: {result.TrackedRepositoryCount}");
    Console.WriteLine($"Added: {result.AddedCount}; updated: {result.UpdatedCount}; unchanged: {result.UnchangedCount}; unavailable: {result.UnavailableCount}");
    Console.WriteLine($"Radar candidates: {result.RadarCandidateCount}; radar added: {result.RadarAddedCount}; radar rejected: {result.RadarRejectedCount}; radar removed by policy: {result.RadarRemovedCount}");
    Console.WriteLine($"Warnings: {result.Warnings.Count}");
    return;
}

var snapshot = mode.ToLowerInvariant() switch
{
    "latest" => await collector.CollectLatestProbeAsync(limit, output, CancellationToken.None),
    "author" when !string.IsNullOrWhiteSpace(author) =>
        await collector.CollectAuthorProbeAsync(author, limit, output, CancellationToken.None),
    "author" => throw new ArgumentException("--author is required when --mode author is selected."),
    "search" => await collector.CollectProbeAsync(query, limit, output, CancellationToken.None),
    _ => throw new ArgumentException("--mode must be search, author, latest, local-search, or sync.")
};

Console.WriteLine($"Catalog: {Path.Combine(output, "catalog.json")}");
Console.WriteLine($"Query: {snapshot.Query}");
Console.WriteLine($"Parsed entries: {snapshot.Entries.Count}");
Console.WriteLine($"Warnings: {snapshot.Warnings.Count + snapshot.Entries.Sum(entry => entry.Warnings.Count)}");
foreach (var entry in snapshot.Entries)
{
    Console.WriteLine($"- {entry.RepoId}; parameters={entry.ParameterCount?.ToString() ?? "unknown"}; license={entry.License}; base={string.Join(",", entry.BaseModels)}");
}

static Dictionary<string, string> ParseArguments(string[] arguments)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < arguments.Length; index++)
    {
        var argument = arguments[index];
        if (!argument.StartsWith("--", StringComparison.Ordinal) || index + 1 >= arguments.Length)
        {
            continue;
        }

        result[argument[2..]] = arguments[++index];
    }
    return result;
}

static string ReadOption(IReadOnlyDictionary<string, string> options, string name, string fallback) =>
    options.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

static List<string> SplitList(string value) => value
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .ToList();

static string FindProjectRoot(string startDirectory)
{
    var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "VERSION"))
            && Directory.Exists(Path.Combine(directory.FullName, "Каталоги")))
        {
            return directory.FullName;
        }
        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("AI_HUB project root was not found.");
}
