using System.IO;
using System.Net.Http;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class HuggingFaceCatalogSyncService
{
    private const int MaximumParallelRepositoryRequests = 4;

    private readonly HuggingFaceCatalogCollector _collector;
    private readonly HuggingFaceCatalogStore _store;
    private readonly Func<DateTimeOffset> _utcNow;

    public HuggingFaceCatalogSyncService(
        HuggingFaceCatalogCollector collector,
        HuggingFaceCatalogStore? store = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        _store = store ?? new HuggingFaceCatalogStore();
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<HuggingFaceCatalogSyncResult> SynchronizeAsync(
        string seedPath,
        string outputDirectory,
        bool includeRadar,
        CancellationToken cancellationToken)
    {
        var seed = HuggingFaceCatalogSeedStore.Load(seedPath);
        var outputRoot = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputRoot);
        var catalogPath = Path.Combine(outputRoot, "catalog.json");
        var changesPath = Path.Combine(outputRoot, "changes.jsonl");
        var now = _utcNow();
        var database = _store.Load(catalogPath, now);
        var result = new HuggingFaceCatalogSyncResult
        {
            CatalogPath = catalogPath,
            ChangesPath = changesPath,
            SeedSlotCount = seed.Slots.Count
        };
        var changes = new List<HuggingFaceCatalogChange>();
        var records = database.Records
            .Where(record => !string.IsNullOrWhiteSpace(record.Entry.RepoId))
            .GroupBy(record => record.Entry.RepoId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var slotsByRepository = seed.Slots
            .GroupBy(slot => slot.RepoId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var trackedRepositoryIds = slotsByRepository.Keys
            .Concat(records.Values.Where(record => record.IsRadarDiscovery).Select(record => record.Entry.RepoId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        result.TrackedRepositoryCount = trackedRepositoryIds.Length;

        var trackedFetch = await FetchRepositoriesAsync(trackedRepositoryIds, outputRoot, cancellationToken);
        foreach (var repoId in trackedRepositoryIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            slotsByRepository.TryGetValue(repoId, out var seedSlots);
            if (trackedFetch.Entries.TryGetValue(repoId, out var entry))
            {
                MergeSuccessfulEntry(records, entry, seedSlots ?? [], false, false, [], 0, now, result, changes);
                AddSeedSizeWarnings(entry, seedSlots ?? [], seed.Radar.MinimumParameterCountExclusive, result.Warnings);
            }
            else
            {
                var error = trackedFetch.Errors.GetValueOrDefault(repoId, "Repository could not be checked.");
                MergeUnavailableEntry(records, repoId, seedSlots ?? [], error, now, result, changes);
            }
        }

        RemoveRadarRecordsThatFailPolicy(database, records, seed, now, result, changes);

        if (includeRadar)
        {
            await DiscoverPopularModelsAsync(seed, outputRoot, records, now, result, changes, cancellationToken);
        }

        if (result.TrackedRepositoryCount > 0
            && result.UnavailableCount == result.TrackedRepositoryCount
            && result.AddedCount == 0
            && result.UpdatedCount == 0
            && result.UnchangedCount == 0)
        {
            throw new HttpRequestException("All tracked Hugging Face repositories were unavailable; the previous catalog was preserved.");
        }

        database.Records = records.Values
            .OrderBy(record => record.Entry.RepoId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        database.LastSuccessfulSyncUtc = now;
        database.Warnings = result.Warnings.Distinct(StringComparer.Ordinal).Take(200).ToList();
        await _store.SaveAsync(database, catalogPath, cancellationToken);
        await _store.AppendChangesAsync(changes, changesPath, cancellationToken);
        return result;
    }

    private async Task DiscoverPopularModelsAsync(
        HuggingFaceCatalogSeed seed,
        string outputRoot,
        Dictionary<string, HuggingFaceCatalogRecord> records,
        DateTimeOffset now,
        HuggingFaceCatalogSyncResult result,
        List<HuggingFaceCatalogChange> changes,
        CancellationToken cancellationToken)
    {
        var radar = seed.Radar;
        var sourceDefinitions = new List<RadarSource>
        {
            new("trending", "trendingScore", string.Empty),
            new("likes", "likes", string.Empty),
            new("downloads", "downloads", string.Empty)
        };
        sourceDefinitions.AddRange(radar.SupportedPipelineTags.Select(
            pipeline => new RadarSource($"pipeline:{pipeline}", "trendingScore", pipeline)));
        var sourceResults = await Task.WhenAll(sourceDefinitions.Select(async source =>
        {
            var sourceLimit = string.IsNullOrWhiteSpace(source.PipelineTag)
                ? radar.QueryLimit
                : Math.Min(20, radar.QueryLimit);
            var url = BuildRadarUrl(source.SortField, sourceLimit, source.PipelineTag);
            var candidates = await _collector.SearchCandidatesAsync(
                $"radar_{source.Name}", url, outputRoot, cancellationToken);
            return (source, candidates);
        }));

        var cutoff = now.AddDays(-radar.LookbackDays);
        var supportedPipelines = radar.SupportedPipelineTags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hits = new Dictionary<string, RadarHit>(StringComparer.OrdinalIgnoreCase);
        foreach (var (source, candidates) in sourceResults)
        {
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                var rank = index + 1;
                if (records.ContainsKey(candidate.RepoId)
                    || candidate.CreatedAtUtc is null
                    || candidate.CreatedAtUtc < cutoff
                    || candidate.ParameterCount is null
                    || candidate.ParameterCount <= radar.MinimumParameterCountExclusive
                    || string.IsNullOrWhiteSpace(candidate.PipelineTag)
                    || !supportedPipelines.Contains(candidate.PipelineTag))
                {
                    continue;
                }

                var isPopular = candidate.Likes >= radar.MinimumLikes
                    || candidate.Downloads >= radar.MinimumDownloads
                    || source.SortField == "trendingScore" && rank <= radar.AutomaticTrendingRankLimit;
                if (!isPopular)
                {
                    continue;
                }

                if (!hits.TryGetValue(candidate.RepoId, out var hit))
                {
                    hit = new RadarHit(candidate);
                    hits.Add(candidate.RepoId, hit);
                }
                hit.Sources.Add(source.Name);
                hit.Score += CalculateSourceScore(source.Name, rank, candidate);
            }
        }

        result.RadarCandidateCount = hits.Count;
        var selectedHits = SelectBalancedRadarHits(hits.Values, radar.MaximumNewEntriesPerSync);
        var radarFetch = await FetchRepositoriesAsync(
            selectedHits.Select(hit => hit.Candidate.RepoId), outputRoot, cancellationToken);
        var seedAuthors = seed.Slots
            .Select(slot => slot.RepoId.Split('/', 2)[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var hit in selectedHits)
        {
            var repoId = hit.Candidate.RepoId;
            if (!radarFetch.Entries.TryGetValue(repoId, out var entry))
            {
                result.Warnings.Add($"Radar candidate {repoId}: {radarFetch.Errors.GetValueOrDefault(repoId, "detail request failed")}");
                continue;
            }
            if (!IsAutomaticallyAdmissibleRadarEntry(entry, radar))
            {
                result.RadarRejectedCount++;
                continue;
            }

            var author = string.IsNullOrWhiteSpace(entry.Author)
                ? repoId.Split('/', 2)[0]
                : entry.Author;
            MergeSuccessfulEntry(
                records,
                entry,
                [],
                true,
                !seedAuthors.Contains(author),
                hit.Sources.Order(StringComparer.OrdinalIgnoreCase).ToList(),
                hit.Score,
                now,
                result,
                changes);
            result.RadarAddedCount++;
        }
    }

    private async Task<RepositoryFetchResult> FetchRepositoriesAsync(
        IEnumerable<string> repositoryIds,
        string outputRoot,
        CancellationToken cancellationToken)
    {
        var result = new RepositoryFetchResult();
        using var semaphore = new SemaphoreSlim(MaximumParallelRepositoryRequests);
        var tasks = repositoryIds.Distinct(StringComparer.OrdinalIgnoreCase).Select(async repoId =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var entry = await _collector.CollectRepositoryEntryAsync(repoId, outputRoot, cancellationToken);
                lock (result)
                {
                    result.Entries[repoId] = entry;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or IOException or InvalidDataException)
            {
                lock (result)
                {
                    result.Errors[repoId] = ex.Message;
                }
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
        return result;
    }

    private static void MergeSuccessfulEntry(
        Dictionary<string, HuggingFaceCatalogRecord> records,
        HuggingFaceCatalogEntry entry,
        List<HuggingFaceCatalogSeedSlot> seedSlots,
        bool isRadarDiscovery,
        bool isNewAuthor,
        List<string> discoverySources,
        double discoveryScore,
        DateTimeOffset now,
        HuggingFaceCatalogSyncResult result,
        List<HuggingFaceCatalogChange> changes)
    {
        if (!records.TryGetValue(entry.RepoId, out var record))
        {
            record = new HuggingFaceCatalogRecord
            {
                Entry = entry,
                SeedSlots = seedSlots,
                CatalogDirections = ResolveDirections(seedSlots, entry.PipelineTag),
                IsRadarDiscovery = isRadarDiscovery,
                IsNewAuthor = isNewAuthor,
                DiscoverySources = discoverySources,
                DiscoveryScore = discoveryScore,
                FirstSeenUtc = now,
                LastSeenUtc = now,
                LastSuccessfulCheckUtc = now,
                IsAvailable = true
            };
            records.Add(entry.RepoId, record);
            result.AddedCount++;
            changes.Add(CreateChange(now, entry.RepoId, isRadarDiscovery ? "radar_added" : "seed_added", string.Empty, entry.RevisionSha, "First catalog observation."));
            return;
        }

        var previousEntry = record.Entry;
        var revisionChanged = !string.Equals(previousEntry.RevisionSha, entry.RevisionSha, StringComparison.OrdinalIgnoreCase);
        var metadataChanged = HasMaterialMetadataChange(previousEntry, entry);
        if (revisionChanged)
        {
            record.PreviousRevisionSha = previousEntry.RevisionSha;
            record.RevisionUpdateCount++;
            result.UpdatedCount++;
            changes.Add(CreateChange(now, entry.RepoId, "revision_changed", previousEntry.RevisionSha, entry.RevisionSha, "Hub revision SHA changed."));
        }
        else if (metadataChanged)
        {
            result.UpdatedCount++;
            changes.Add(CreateChange(now, entry.RepoId, "metadata_changed", previousEntry.RevisionSha, entry.RevisionSha, "License, pipeline, parameter count, model type or access flags changed."));
        }
        else
        {
            result.UnchangedCount++;
        }

        record.Entry = entry;
        record.SeedSlots = seedSlots.Count > 0 ? seedSlots : record.SeedSlots;
        record.CatalogDirections = ResolveDirections(record.SeedSlots, entry.PipelineTag);
        record.IsRadarDiscovery |= isRadarDiscovery;
        record.IsNewAuthor |= isNewAuthor;
        record.DiscoverySources = discoverySources.Count > 0 ? discoverySources : record.DiscoverySources;
        record.DiscoveryScore = Math.Max(record.DiscoveryScore, discoveryScore);
        record.LastSeenUtc = now;
        record.LastSuccessfulCheckUtc = now;
        record.IsAvailable = true;
        record.LastError = string.Empty;
    }

    private static void MergeUnavailableEntry(
        Dictionary<string, HuggingFaceCatalogRecord> records,
        string repoId,
        List<HuggingFaceCatalogSeedSlot> seedSlots,
        string error,
        DateTimeOffset now,
        HuggingFaceCatalogSyncResult result,
        List<HuggingFaceCatalogChange> changes)
    {
        result.UnavailableCount++;
        result.Warnings.Add($"{repoId}: {error}");
        if (!records.TryGetValue(repoId, out var record))
        {
            record = new HuggingFaceCatalogRecord
            {
                Entry = new HuggingFaceCatalogEntry { RepoId = repoId },
                SeedSlots = seedSlots,
                CatalogDirections = seedSlots.Select(slot => slot.Direction).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                FirstSeenUtc = now,
                LastSeenUtc = now,
                IsAvailable = false,
                LastError = error
            };
            records.Add(repoId, record);
        }
        else
        {
            record.SeedSlots = seedSlots.Count > 0 ? seedSlots : record.SeedSlots;
            record.LastSeenUtc = now;
            record.IsAvailable = false;
            record.LastError = error;
        }

        changes.Add(CreateChange(now, repoId, "check_failed", record.Entry.RevisionSha, record.Entry.RevisionSha, error));
    }

    private static void AddSeedSizeWarnings(
        HuggingFaceCatalogEntry entry,
        IReadOnlyList<HuggingFaceCatalogSeedSlot> slots,
        long minimumExclusive,
        List<string> warnings)
    {
        if (slots.Count == 0 || slots.All(slot => slot.IsManualException))
        {
            return;
        }
        if (entry.ParameterCount is null)
        {
            warnings.Add($"Seed model {entry.RepoId} has unknown parameter count; it remains curated but cannot pass automatic >8B admission.");
        }
        else if (entry.ParameterCount <= minimumExclusive)
        {
            warnings.Add($"Seed model {entry.RepoId} is not above 8B and must be marked as a manual exception.");
        }
    }

    private static bool HasMaterialMetadataChange(HuggingFaceCatalogEntry previous, HuggingFaceCatalogEntry current) =>
        !string.Equals(previous.License, current.License, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(previous.PipelineTag, current.PipelineTag, StringComparison.OrdinalIgnoreCase)
        || previous.ParameterCount != current.ParameterCount
        || !string.Equals(previous.ModelType, current.ModelType, StringComparison.OrdinalIgnoreCase)
        || previous.IsGated != current.IsGated
        || previous.IsPrivate != current.IsPrivate
        || previous.IsDisabled != current.IsDisabled;

    private static HuggingFaceCatalogChange CreateChange(
        DateTimeOffset now,
        string repoId,
        string kind,
        string previousRevision,
        string currentRevision,
        string details) => new()
        {
            OccurredAtUtc = now,
            RepoId = repoId,
            Kind = kind,
            PreviousRevisionSha = previousRevision,
            CurrentRevisionSha = currentRevision,
            Details = details
        };

    private static string BuildRadarUrl(string sortField, int limit, string pipelineTag) =>
        $"https://huggingface.co/api/models?sort={Uri.EscapeDataString(sortField)}&direction=-1&limit={limit}"
        + (string.IsNullOrWhiteSpace(pipelineTag) ? string.Empty : $"&pipeline_tag={Uri.EscapeDataString(pipelineTag)}")
        + "&expand[]=author&expand[]=createdAt&expand[]=downloads&expand[]=likes"
        + "&expand[]=pipeline_tag&expand[]=safetensors&expand[]=trendingScore";

    private static double CalculateSourceScore(
        string source,
        int rank,
        HuggingFaceSearchCandidate candidate)
    {
        var sourceWeight = source switch
        {
            "trending" => 4.0,
            "likes" => 2.0,
            "downloads" => 1.0,
            _ when source.StartsWith("pipeline:", StringComparison.Ordinal) => 3.0,
            _ => 0.5
        };
        var rankScore = sourceWeight * (1.0 / Math.Max(1, rank));
        var popularityScore = Math.Log10(1 + Math.Max(0, candidate.Likes ?? 0))
            + Math.Log10(1 + Math.Max(0, candidate.Downloads ?? 0)) / 2.0;
        return rankScore + popularityScore;
    }

    private static RadarHit[] SelectBalancedRadarHits(IEnumerable<RadarHit> hits, int limit)
    {
        var ranked = hits
            .OrderByDescending(hit => hit.Score)
            .ThenByDescending(hit => hit.Candidate.Likes ?? 0)
            .ThenByDescending(hit => hit.Candidate.Downloads ?? 0)
            .ToList();
        var selected = ranked
            .GroupBy(hit => hit.Candidate.PipelineTag, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(hit => hit.Score)
            .Take(limit)
            .ToList();
        selected.AddRange(ranked.Where(hit => !selected.Contains(hit)).Take(Math.Max(0, limit - selected.Count)));
        return selected.ToArray();
    }

    private static void RemoveRadarRecordsThatFailPolicy(
        HuggingFaceCatalogDatabase database,
        Dictionary<string, HuggingFaceCatalogRecord> records,
        HuggingFaceCatalogSeed seed,
        DateTimeOffset now,
        HuggingFaceCatalogSyncResult result,
        List<HuggingFaceCatalogChange> changes)
    {
        foreach (var record in records.Values
            .Where(record => record.IsRadarDiscovery && record.SeedSlots.Count == 0)
            .Where(record => !IsAutomaticallyAdmissibleRadarEntry(record.Entry, seed.Radar))
            .ToList())
        {
            records.Remove(record.Entry.RepoId);
            result.RadarRemovedCount++;
            changes.Add(CreateChange(
                now,
                record.Entry.RepoId,
                "radar_removed_policy",
                record.Entry.RevisionSha,
                record.Entry.RevisionSha,
                "The entry no longer passes automatic radar admission policy."));
        }
        database.Warnings.RemoveAll(warning => warning.StartsWith("Radar candidate ", StringComparison.Ordinal));
    }

    private static bool IsAutomaticallyAdmissibleRadarEntry(
        HuggingFaceCatalogEntry entry,
        HuggingFaceRadarSettings radar)
    {
        if (entry.ParameterCount is null
            || entry.ParameterCount <= radar.MinimumParameterCountExclusive
            || entry.IsPrivate
            || entry.IsDisabled
            || entry.IsGated
            || !radar.SupportedPipelineTags.Contains(entry.PipelineTag, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var packageMarkers = new[] { "quantized", "gguf", "gptq", "awq", "exl2", "mlx", "nvfp4", "fp4", "fp8", "4bit", "4-bit", "8bit", "8-bit", "bitsandbytes" };
        if (string.Equals(entry.BaseModelRelation, "quantized", StringComparison.OrdinalIgnoreCase)
            || entry.Tags.Any(tag => packageMarkers.Contains(tag, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        var normalizedName = entry.RepoId.Replace("_", "-", StringComparison.Ordinal).ToLowerInvariant();
        return !packageMarkers.Any(marker => normalizedName.Contains($"-{marker}", StringComparison.Ordinal));
    }

    private static List<string> ResolveDirections(
        IReadOnlyList<HuggingFaceCatalogSeedSlot> seedSlots,
        string pipelineTag)
    {
        var directions = seedSlots.Select(slot => slot.Direction).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (directions.Count > 0)
        {
            return directions;
        }

        var direction = pipelineTag switch
        {
            "text-generation" => "text_knowledge",
            "image-text-to-text" => "vision_documents",
            "feature-extraction" or "sentence-similarity" => "search_memory",
            "text-to-image" => "image_generation",
            "text-to-video" => "video",
            "automatic-speech-recognition" or "text-to-speech" or "audio-text-to-text" => "audio_speech",
            "tabular-classification" or "time-series-forecasting" => "data_forecasting",
            "robotics" or "image-to-3d" or "text-to-3d" => "spatial_robotics",
            "text-classification" => "safety_control",
            _ => string.Empty
        };
        return string.IsNullOrWhiteSpace(direction) ? [] : [direction];
    }

    private sealed record RadarSource(string Name, string SortField, string PipelineTag);

    private sealed class RadarHit(HuggingFaceSearchCandidate candidate)
    {
        public HuggingFaceSearchCandidate Candidate { get; } = candidate;

        public HashSet<string> Sources { get; } = new(StringComparer.OrdinalIgnoreCase);

        public double Score { get; set; }
    }

    private sealed class RepositoryFetchResult
    {
        public Dictionary<string, HuggingFaceCatalogEntry> Entries { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Errors { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
