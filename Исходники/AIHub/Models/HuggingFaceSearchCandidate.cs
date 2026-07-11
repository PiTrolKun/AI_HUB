namespace AIHub.Models;

public sealed class HuggingFaceSearchCandidate
{
    public string RepoId { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public DateTimeOffset? CreatedAtUtc { get; set; }

    public long? Downloads { get; set; }

    public long? Likes { get; set; }

    public double? TrendingScore { get; set; }

    public string PipelineTag { get; set; } = string.Empty;

    public long? ParameterCount { get; set; }
}
