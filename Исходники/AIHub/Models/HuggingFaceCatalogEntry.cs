namespace AIHub.Models;

public sealed class HuggingFaceCatalogEntry
{
    public string RepoId { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string RevisionSha { get; set; } = string.Empty;

    public DateTimeOffset? CreatedAtUtc { get; set; }

    public DateTimeOffset? LastModifiedUtc { get; set; }

    public long? Downloads { get; set; }

    public long? Likes { get; set; }

    public double? TrendingScore { get; set; }

    public string PipelineTag { get; set; } = string.Empty;

    public string LibraryName { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public string LicenseUrl { get; set; } = string.Empty;

    public bool IsGated { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsDisabled { get; set; }

    public List<string> BaseModels { get; set; } = [];

    public string BaseModelRelation { get; set; } = string.Empty;

    public List<string> Languages { get; set; } = [];

    public List<string> Datasets { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public List<string> Architectures { get; set; } = [];

    public string ModelType { get; set; } = string.Empty;

    public string GgufArchitecture { get; set; } = string.Empty;

    public long? ParameterCount { get; set; }

    public long? ContextLength { get; set; }

    public long? TotalFileSizeBytes { get; set; }

    public string AuthorDescription { get; set; } = string.Empty;

    public string MetadataEvidence { get; set; } = "huggingface_hub_api";

    public string DescriptionEvidence { get; set; } = "model_card_author_claim";

    public bool IsRevisionPinned { get; set; }

    public string ApiSourceUrl { get; set; } = string.Empty;

    public string ModelCardSourceUrl { get; set; } = string.Empty;

    public string RawApiRelativePath { get; set; } = string.Empty;

    public string RawApiSha256 { get; set; } = string.Empty;

    public string RawModelCardRelativePath { get; set; } = string.Empty;

    public string RawModelCardSha256 { get; set; } = string.Empty;

    public DateTimeOffset RetrievedAtUtc { get; set; }

    public List<string> Warnings { get; set; } = [];
}
