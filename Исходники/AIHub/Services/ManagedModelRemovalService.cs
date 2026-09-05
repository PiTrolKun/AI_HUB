using AIHub.Models;
using System.IO;

namespace AIHub.Services;

public interface IModelUsageGuard
{
    bool IsActive(string modelArtifactId);
}

public sealed class NullModelUsageGuard : IModelUsageGuard
{
    public bool IsActive(string modelArtifactId) => false;
}

public sealed class DelegateModelUsageGuard(Func<string, bool> predicate) : IModelUsageGuard
{
    public bool IsActive(string modelArtifactId) => predicate(modelArtifactId);
}

public sealed class ManagedModelRemovalService
{
    private readonly ManagedModelLibraryStore _store;
    private readonly IModelUsageGuard _usageGuard;

    public ManagedModelRemovalService(
        ManagedModelLibraryStore store,
        IModelUsageGuard? usageGuard = null)
    {
        _store = store;
        _usageGuard = usageGuard ?? new NullModelUsageGuard();
    }

    public ManagedModelRemovalResult RemoveFiles(string modelArtifactId, bool includePartialFiles)
    {
        var card = _store.Load(modelArtifactId)
            ?? throw new InvalidOperationException("The model card is not registered in the LOPATA library.");
        EnsureRemovalAllowed(card);
        var root = Path.GetFullPath(card.ModelsRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var installRoot = Path.GetFullPath(card.InstallDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!installRoot.StartsWith(root, StringComparison.OrdinalIgnoreCase) || installRoot == root)
        {
            throw new InvalidOperationException("Refusing to remove files outside the configured model storage.");
        }

        EnsureNoReparsePoint(root, installRoot);
        var removed = new List<string>();
        long removedBytes = 0;
        foreach (var file in card.Files)
        {
            var path = GetContainedPath(installRoot, file.RelativePath);
            removedBytes += DeleteExactFile(path, removed);
            if (includePartialFiles)
            {
                foreach (var partialPath in SegmentedModelFileDownloader.GetPartialArtifactPaths(path))
                {
                    removedBytes += DeleteExactFile(partialPath, removed);
                }
            }
        }

        card.StoredBytes = 0;
        card.Status = ManagedModelStatuses.FilesRemoved;
        card.LastVerifiedAt = null;
        card.RuntimeVerifiedAt = null;
        card.LastError = string.Empty;
        _store.Upsert(card);
        return new ManagedModelRemovalResult(removedBytes, removed);
    }

    private void EnsureRemovalAllowed(ManagedModelArtifactCard card)
    {
        if (!card.IsManaged || !card.CanRemoveFiles)
        {
            throw new InvalidOperationException("LOPATA does not own these model files and cannot remove them.");
        }
        if (card.IsPinned)
        {
            throw new InvalidOperationException("The model is pinned. Unpin it before removing files.");
        }
        if (_usageGuard.IsActive(card.ModelArtifactId) || card.Status == ManagedModelStatuses.InUse)
        {
            throw new InvalidOperationException("The model is used by an active session and cannot be removed.");
        }
        if (string.IsNullOrWhiteSpace(card.ModelsRoot) || string.IsNullOrWhiteSpace(card.InstallDirectory))
        {
            throw new InvalidOperationException("The managed model path is not available.");
        }
    }

    private static long DeleteExactFile(string path, ICollection<string> removed)
    {
        if (!File.Exists(path))
        {
            return 0;
        }
        var info = new FileInfo(path);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException("Refusing to remove a model file through a reparse point.");
        }
        var bytes = info.Length;
        File.Delete(path);
        removed.Add(path);
        return bytes;
    }

    private static string GetContainedPath(string installRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidDataException("The model manifest contains an unsafe file path.");
        }
        var path = Path.GetFullPath(Path.Combine(installRoot, relativePath));
        if (!path.StartsWith(installRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The model file path escapes the managed model directory.");
        }
        return path;
    }

    private static void EnsureNoReparsePoint(string modelsRoot, string installRoot)
    {
        var current = new DirectoryInfo(installRoot.TrimEnd(Path.DirectorySeparatorChar));
        var boundary = modelsRoot.TrimEnd(Path.DirectorySeparatorChar);
        while (current is not null
               && current.FullName.StartsWith(modelsRoot, StringComparison.OrdinalIgnoreCase)
               && !string.Equals(current.FullName, boundary, StringComparison.OrdinalIgnoreCase))
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("Refusing to remove model files through a junction or symbolic link.");
            }
            current = current.Parent;
        }
    }
}
