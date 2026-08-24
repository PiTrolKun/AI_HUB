using AIHub.Models;
using System.Net.Http;

namespace AIHub.Services;

public sealed class ImageAnalysisBundleInstallationService : IDisposable
{
    private static readonly string[] MediumArtifactIds =
    [
        ManagedModelCatalog.CoreArtifactId,
        ManagedModelCatalog.KimiMediumArtifactId,
        ManagedModelCatalog.FlorenceLargeArtifactId
    ];

    private readonly ManagedModelLibraryStore _store;
    private readonly ManagedModelInventoryService _inventory;
    private readonly ManagedModelAcquisitionService _acquisition;
    private readonly ManagedModelRemovalService _removal;
    private readonly ImageAnalysisRuntimeCompatibilityService _runtimeCompatibility;

    public ImageAnalysisBundleInstallationService(
        ManagedModelLibraryStore? store = null,
        HttpClient? httpClient = null,
        IModelUsageGuard? usageGuard = null)
    {
        _store = store ?? new ManagedModelLibraryStore();
        _inventory = new ManagedModelInventoryService(_store);
        _acquisition = new ManagedModelAcquisitionService(_store, httpClient);
        _removal = new ManagedModelRemovalService(_store, usageGuard);
        _runtimeCompatibility = new ImageAnalysisRuntimeCompatibilityService(_store);
    }

    public ManagedModelLibraryStore LibraryStore => _store;

    public int MaximumParallelConnections
    {
        get => _acquisition.MaximumParallelConnections;
        set => _acquisition.MaximumParallelConnections = value;
    }

    public ImageAnalysisBundleInstallationSnapshot Check(StorageSettings settings)
    {
        var cards = _inventory.Synchronize(settings)
            .Where(card => MediumArtifactIds.Contains(card.ModelArtifactId, StringComparer.Ordinal))
            .OrderBy(card => Array.IndexOf(MediumArtifactIds, card.ModelArtifactId))
            .ToList();
        var modelsRoot = settings.Models.Locations
            .Select(location => location.Path?.Trim())
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path))
            ?? string.Empty;
        if (string.IsNullOrWhiteSpace(modelsRoot))
        {
            return CreateSnapshot(ImageAnalysisBundleInstallStates.StorageNotConfigured, cards, modelsRoot);
        }
        if (cards.Any(card => card.Status == ManagedModelStatuses.Corrupted))
        {
            return CreateSnapshot(ImageAnalysisBundleInstallStates.Corrupted, cards, modelsRoot);
        }
        if (cards.Any(card => card.Status == ManagedModelStatuses.RuntimeIncompatible))
        {
            return CreateSnapshot(ImageAnalysisBundleInstallStates.RuntimeIncompatible, cards, modelsRoot);
        }
        if (cards.Any(card => card.Status == ManagedModelStatuses.Paused))
        {
            return CreateSnapshot(ImageAnalysisBundleInstallStates.ResumeAvailable, cards, modelsRoot);
        }
        if (cards.Any(card => card.Status is ManagedModelStatuses.NotInstalled
                or ManagedModelStatuses.FilesRemoved
                or ManagedModelStatuses.SourceUnavailable))
        {
            return CreateSnapshot(ImageAnalysisBundleInstallStates.DownloadRequired, cards, modelsRoot);
        }
        if (cards.Any(card => card.Status == ManagedModelStatuses.NeedsVerification))
        {
            return CreateSnapshot(ImageAnalysisBundleInstallStates.NeedsVerification, cards, modelsRoot);
        }
        return CreateSnapshot(
            cards.Count == MediumArtifactIds.Length
                && cards.All(card => card.Status == ManagedModelStatuses.Installed)
                ? ImageAnalysisBundleInstallStates.Ready
                : ImageAnalysisBundleInstallStates.Error,
            cards,
            modelsRoot);
    }

    public async Task<ImageAnalysisBundleInstallationSnapshot> DownloadMissingAsync(
        StorageSettings settings,
        IProgress<ManagedModelDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var snapshot = Check(settings);
        if (snapshot.State == ImageAnalysisBundleInstallStates.StorageNotConfigured)
        {
            return snapshot;
        }
        foreach (var component in snapshot.Components.Where(component =>
                     component.Status is ManagedModelStatuses.NotInstalled
                         or ManagedModelStatuses.FilesRemoved
                         or ManagedModelStatuses.SourceUnavailable
                         or ManagedModelStatuses.Paused
                         or ManagedModelStatuses.Corrupted))
        {
            await _acquisition.DownloadAsync(component.ModelArtifactId, progress, cancellationToken);
        }
        return Check(settings);
    }

    public async Task<ImageAnalysisBundleInstallationSnapshot> VerifyAsync(
        StorageSettings settings,
        IProgress<ManagedModelDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var snapshot = Check(settings);
        foreach (var component in snapshot.Components.Where(component => component.StoredBytes > 0))
        {
            var card = await _acquisition.VerifyAsync(component.ModelArtifactId, progress, cancellationToken);
            if (card.Status == ManagedModelStatuses.NeedsVerification
                && card.ModelArtifactId is ManagedModelCatalog.KimiMediumArtifactId
                    or ManagedModelCatalog.FlorenceLargeArtifactId)
            {
                await _runtimeCompatibility.VerifyAsync(
                    card.ModelArtifactId,
                    cancellationToken,
                    progress);
            }
        }
        return Check(settings);
    }

    public ManagedModelRemovalResult RemoveVisionFiles() =>
        _removal.RemoveFiles(ManagedModelCatalog.KimiMediumArtifactId, includePartialFiles: true);

    public void Dispose() => _acquisition.Dispose();

    private static ImageAnalysisBundleInstallationSnapshot CreateSnapshot(
        string state,
        IReadOnlyList<ManagedModelArtifactCard> cards,
        string modelsRoot) => new()
        {
            State = state,
            ModelsRoot = modelsRoot,
            MissingBytes = cards.Sum(card => Math.Max(0, card.TotalBytes - card.StoredBytes)),
            Components = cards.Select(card => new ImageAnalysisBundleComponentState
            {
                ModelArtifactId = card.ModelArtifactId,
                DisplayName = card.DisplayName,
                Role = card.Role,
                Status = card.Status,
                TotalBytes = card.TotalBytes,
                StoredBytes = card.StoredBytes,
                IsShared = card.Consumers.Count > 1,
                LastError = card.LastError
            }).ToList()
        };
}
