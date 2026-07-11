namespace AIHub.Models;

public sealed class ModelCatalogSearchRequest
{
    public List<string> Directions { get; set; } = [];

    public string TaskType { get; set; } = string.Empty;

    public List<string> RequiredCapabilities { get; set; } = [];

    public string LoadLevel { get; set; } = "any";

    public int Limit { get; set; } = 5;
}

public sealed class ModelCatalogSearchResponse
{
    public string Status { get; set; } = string.Empty;

    public string CatalogPath { get; set; } = string.Empty;

    public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }

    public int RecordsConsidered { get; set; }

    public int HardwareRejectedCount { get; set; }

    public int LineageRejectedCount { get; set; }

    public bool RequiresLiveSearch { get; set; }

    public bool LiveVerificationSuggested { get; set; }

    public List<ModelCatalogCandidate> Candidates { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}

public sealed class ModelCatalogCandidate
{
    public string RepoId { get; set; } = string.Empty;

    public string PipelineTag { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public long? ParameterCount { get; set; }

    public long? ContextLength { get; set; }

    public List<string> BaseModels { get; set; } = [];

    public string ModelType { get; set; } = string.Empty;

    public List<string> Directions { get; set; } = [];

    public List<string> Roles { get; set; } = [];

    public List<string> LoadLevels { get; set; } = [];

    public bool LoadLevelWasInferred { get; set; }

    public string Source { get; set; } = string.Empty;

    public DateTimeOffset? LastCheckedUtc { get; set; }

    public long? Downloads { get; set; }

    public long? Likes { get; set; }

    public ModelHardwareCompatibility Hardware { get; set; } = new();

    public List<string> MatchReasons { get; set; } = [];
}

public sealed class ModelHardwareCompatibility
{
    public string Status { get; set; } = "unknown";

    public bool? IsCompatible { get; set; }

    public double? EstimatedQ4RuntimeGb { get; set; }

    public double AvailableRamGb { get; set; }

    public double AvailableVramGb { get; set; }

    public double LargestFreeDriveGb { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string EstimateBasis { get; set; } = "estimated_q4_weights_plus_runtime_overhead";
}
