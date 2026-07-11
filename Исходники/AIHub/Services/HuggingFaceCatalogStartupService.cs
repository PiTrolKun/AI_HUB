using System.IO;
using System.Net.Http;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class HuggingFaceCatalogStartupService
{
    public static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromHours(24);

    private static readonly SemaphoreSlim ProcessSyncLock = new(1, 1);

    private readonly string _seedPath;
    private readonly string _catalogDirectory;
    private readonly TimeSpan _refreshInterval;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Func<string, string, bool, CancellationToken, Task<HuggingFaceCatalogSyncResult>> _synchronize;

    public HuggingFaceCatalogStartupService(
        string? seedPath = null,
        string? catalogDirectory = null,
        TimeSpan? refreshInterval = null,
        Func<DateTimeOffset>? utcNow = null,
        Func<string, string, bool, CancellationToken, Task<HuggingFaceCatalogSyncResult>>? synchronize = null)
    {
        _seedPath = seedPath ?? AppDataPaths.HuggingFaceCatalogSeedPath;
        _catalogDirectory = catalogDirectory ?? AppDataPaths.HuggingFaceCatalogDirectory;
        _refreshInterval = refreshInterval ?? DefaultRefreshInterval;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _synchronize = synchronize ?? SynchronizeAsync;
    }

    public async Task<CatalogStartupSyncResult> SynchronizeIfDueAsync(CancellationToken cancellationToken)
    {
        await ProcessSyncLock.WaitAsync(cancellationToken);
        try
        {
            var now = _utcNow();
            var catalogPath = Path.Combine(_catalogDirectory, "catalog.json");
            var previousSync = TryReadLastSync(catalogPath, now);
            if (previousSync is not null && now - previousSync.Value < _refreshInterval)
            {
                return new CatalogStartupSyncResult
                {
                    Status = "skipped_fresh",
                    Message = "The local Hugging Face catalog is still fresh.",
                    PreviousSyncUtc = previousSync,
                    CurrentSyncUtc = previousSync
                };
            }

            if (!File.Exists(_seedPath))
            {
                return new CatalogStartupSyncResult
                {
                    Status = "skipped_seed_missing",
                    Message = $"Catalog seed was not found: {_seedPath}",
                    PreviousSyncUtc = previousSync
                };
            }

            try
            {
                var sync = await _synchronize(
                    _seedPath,
                    _catalogDirectory,
                    true,
                    cancellationToken);
                return new CatalogStartupSyncResult
                {
                    Status = "updated",
                    Message = "The local Hugging Face catalog was refreshed in the background.",
                    PreviousSyncUtc = previousSync,
                    CurrentSyncUtc = _utcNow(),
                    TrackedRepositoryCount = sync.TrackedRepositoryCount,
                    UpdatedCount = sync.UpdatedCount,
                    AddedCount = sync.AddedCount
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException)
            {
                return new CatalogStartupSyncResult
                {
                    Status = "failed_preserved",
                    Message = ex.Message,
                    PreviousSyncUtc = previousSync,
                    CurrentSyncUtc = previousSync
                };
            }
        }
        finally
        {
            ProcessSyncLock.Release();
        }
    }

    private static async Task<HuggingFaceCatalogSyncResult> SynchronizeAsync(
        string seedPath,
        string catalogDirectory,
        bool includeRadar,
        CancellationToken cancellationToken)
    {
        using var collector = new HuggingFaceCatalogCollector();
        var service = new HuggingFaceCatalogSyncService(collector);
        return await service.SynchronizeAsync(seedPath, catalogDirectory, includeRadar, cancellationToken);
    }

    private static DateTimeOffset? TryReadLastSync(string catalogPath, DateTimeOffset now)
    {
        try
        {
            return new HuggingFaceCatalogStore().Load(catalogPath, now).LastSuccessfulSyncUtc;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException)
        {
            return null;
        }
    }
}
