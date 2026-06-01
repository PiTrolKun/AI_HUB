namespace AIHub.Models;

public sealed class HuggingFaceModelCandidate
{
    public string RepoId { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string PipelineTag { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public DateTimeOffset? LastModified { get; set; }

    public int? Downloads { get; set; }

    public int? Likes { get; set; }

    public List<string> Tags { get; set; } = [];

    public List<HuggingFaceModelFile> Files { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}
