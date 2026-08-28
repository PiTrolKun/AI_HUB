using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIHub.Models;

public static class ManagedModelStatuses
{
    public const string NotInstalled = "not_installed";
    public const string AwaitingConfirmation = "awaiting_confirmation";
    public const string Downloading = "downloading";
    public const string Paused = "paused";
    public const string Installed = "installed";
    public const string NeedsVerification = "needs_verification";
    public const string Corrupted = "corrupted";
    public const string RuntimeIncompatible = "runtime_incompatible";
    public const string FilesRemoved = "files_removed";
    public const string SourceUnavailable = "source_unavailable";
    public const string External = "external";
    public const string InUse = "in_use";
}

public static class ManagedModelRoles
{
    public const string Core = "core";
    public const string Executor = "executor";
    public const string Vision = "vision";
    public const string Localizer = "localizer";
    public const string Embedding = "embedding";
    public const string Reranker = "reranker";
    public const string Tool = "tool";
    public const string Speech = "speech";
    public const string External = "external";
}

public static class ManagedModelOrigins
{
    public const string PredefinedScenario = "predefined_scenario";
    public const string Sandbox = "sandbox";
    public const string ExistingManifest = "existing_manifest";
    public const string ExternalDiscovery = "external_discovery";
}

public sealed class ManagedModelArtifactCard
{
    public int SchemaVersion { get; set; } = 1;

    public string ModelArtifactId { get; set; } = string.Empty;

    public string Family { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = ManagedModelRoles.External;

    public string Provider { get; set; } = string.Empty;

    public string RepositoryId { get; set; } = string.Empty;

    public string Revision { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string Architecture { get; set; } = string.Empty;

    public string Quantization { get; set; } = string.Empty;

    public long ParameterCount { get; set; }

    public string License { get; set; } = string.Empty;

    public string SourcePage { get; set; } = string.Empty;

    public bool IsManaged { get; set; }

    public bool IsPinned { get; set; }

    public bool IsSystem { get; set; }

    public bool CanRemoveFiles { get; set; }

    public bool SupportsDirectDownload { get; set; } = true;

    public string InstallDirectory { get; set; } = string.Empty;

    public string ModelsRoot { get; set; } = string.Empty;

    public string Status { get; set; } = ManagedModelStatuses.NotInstalled;

    public long StoredBytes { get; set; }

    public DateTimeOffset FirstDiscoveredAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? FirstInstalledAt { get; set; }

    public DateTimeOffset? LastVerifiedAt { get; set; }

    public DateTimeOffset? RuntimeVerifiedAt { get; set; }

    public string RuntimeBackend { get; set; } = string.Empty;

    public string LastError { get; set; } = string.Empty;

    public string Origin { get; set; } = ManagedModelOrigins.ExistingManifest;

    public ManagedModelDiscoveryProvenance Discovery { get; set; } = new();

    public ModelSemanticPassport SemanticPassport { get; set; } = new();

    public List<ManagedModelConsumer> Consumers { get; set; } = [];

    public List<ManagedModelArtifactFile> Files { get; set; } = [];

    public long TotalBytes => Files.Sum(file => Math.Max(0, file.SizeBytes));
}

public sealed class ManagedModelArtifactFile
{
    public string RelativePath { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    public long VerifiedSizeBytes { get; set; }

    public DateTimeOffset? VerifiedLastWriteTimeUtc { get; set; }
}

public sealed class ManagedModelConsumer
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;
}

public sealed class ManagedModelDiscoveryProvenance
{
    public string ScenarioId { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string SearchSnapshot { get; set; } = string.Empty;

    public string SelectionReason { get; set; } = string.Empty;

    public string HardwareAssessment { get; set; } = string.Empty;

    public string AppVersion { get; set; } = string.Empty;
}

public sealed record ManagedModelDownloadProgress(
    string ModelArtifactId,
    string FileName,
    long DownloadedBytes,
    long TotalBytes,
    double BytesPerSecond,
    string Stage);

public sealed record ManagedModelRemovalResult(long RemovedBytes, IReadOnlyList<string> RemovedFiles);

public sealed class ManagedModelCardViewModel : INotifyPropertyChanged
{
    private string _statusText = string.Empty;
    private bool _isBusy;
    private double _progressPercent;
    private string _progressText = string.Empty;

    public required ManagedModelArtifactCard Card { get; init; }

    public string RoleText { get; set; } = string.Empty;

    public string StatusText { get => _statusText; set => SetField(ref _statusText, value); }

    public string SizeText { get; set; } = string.Empty;

    public string SourceText { get; set; } = string.Empty;

    public string ConsumersText { get; set; } = string.Empty;

    public string PathText { get; set; } = string.Empty;

    public string WarningText { get; set; } = string.Empty;

    public string DownloadActionText { get; set; } = string.Empty;

    public string VerifyActionText { get; set; } = string.Empty;

    public string RemoveActionText { get; set; } = string.Empty;

    public bool IsBusy { get => _isBusy; set => SetField(ref _isBusy, value); }

    public double ProgressPercent { get => _progressPercent; set => SetField(ref _progressPercent, value); }

    public string ProgressText { get => _progressText; set => SetField(ref _progressText, value); }

    public bool CanDownload => Card.IsManaged && Card.SupportsDirectDownload && Card.Status is not ManagedModelStatuses.Installed and not ManagedModelStatuses.InUse;

    public bool CanReinstall => Card.IsManaged && Card.SupportsDirectDownload && Card.Status == ManagedModelStatuses.Installed;

    public bool CanAcquire => CanDownload || CanReinstall;

    public bool CanVerify => Card.IsManaged && Card.StoredBytes > 0;

    public bool CanRemove => Card.IsManaged && Card.CanRemoveFiles && !Card.IsPinned && Card.Status is ManagedModelStatuses.Installed or ManagedModelStatuses.NeedsVerification or ManagedModelStatuses.Corrupted or ManagedModelStatuses.RuntimeIncompatible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyActions()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDownload)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanReinstall)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanAcquire)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanVerify)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRemove)));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
