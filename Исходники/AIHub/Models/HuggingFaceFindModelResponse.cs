namespace AIHub.Models;

public sealed class HuggingFaceFindModelResponse
{
    public string Role { get; set; } = string.Empty;

    public string Query { get; set; } = string.Empty;

    public string Format { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public long? MaxSizeBytes { get; set; }

    public string SavedPath { get; set; } = string.Empty;

    public List<HuggingFaceModelCandidate> Candidates { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}
