namespace AIHub.Models;

public sealed class TaskPlanResponse
{
    public string Task { get; set; } = string.Empty;

    public string TaskType { get; set; } = string.Empty;

    public List<string> RequiredRoles { get; set; } = [];

    public List<string> InstalledRoles { get; set; } = [];

    public List<string> MissingRoles { get; set; } = [];

    public bool CanContinueWithoutDownload { get; set; }

    public string NextAction { get; set; } = string.Empty;

    public List<string> Notes { get; set; } = [];
}
