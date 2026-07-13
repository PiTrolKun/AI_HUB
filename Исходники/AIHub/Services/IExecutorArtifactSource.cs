using AIHub.Models;

namespace AIHub.Services;

public interface IExecutorArtifactSource
{
    Task<HuggingFaceModelCandidate> GetFilesAsync(
        string repoId,
        StorageSettings storageSettings,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<HuggingFaceModelCandidate>> SearchGgufAsync(
        string requestedModel,
        StorageSettings storageSettings,
        CancellationToken cancellationToken);
}

public sealed class HuggingFaceExecutorArtifactSource : IExecutorArtifactSource
{
    private readonly HuggingFaceProviderTool _provider = new();

    public Task<HuggingFaceModelCandidate> GetFilesAsync(
        string repoId,
        StorageSettings storageSettings,
        CancellationToken cancellationToken) =>
        _provider.GetModelFilesAsync(repoId, storageSettings, cancellationToken);

    public async Task<IReadOnlyList<HuggingFaceModelCandidate>> SearchGgufAsync(
        string requestedModel,
        StorageSettings storageSettings,
        CancellationToken cancellationToken)
    {
        var response = await _provider.FindModelAsync(
            $"role=executor query={requestedModel} GGUF format=gguf",
            storageSettings,
            cancellationToken);
        return response.Candidates;
    }
}
