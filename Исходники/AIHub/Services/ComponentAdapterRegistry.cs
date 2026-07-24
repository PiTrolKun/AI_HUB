using AIHub.Models;

namespace AIHub.Services;

public sealed record ComponentAdapterDescriptor(
    string Id,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> ToolNames,
    string UsageSummary);

public static class ComponentAdapterRegistry
{
    private static readonly IReadOnlyList<ComponentAdapterDescriptor> Adapters =
    [
        new(
            "adapter.session_files.read",
            [
                "read.text",
                "read.json",
                "read.xml",
                "read.office_openxml",
                "read.spreadsheet",
                "read.pdf_text",
                "read.archive",
                "read.csv",
                "read.html",
                "read.svg",
                "read.markdown",
                "read.yaml",
                "read.email",
                "read.database.sqlite"
            ],
            [
                "session_files_list",
                "session_file_inspect",
                "session_file_read"
            ],
            "Reads bounded, non-destructive representations of files explicitly attached to the current session.")
    ];

    public static ComponentAdapterDescriptor? Find(string capability) =>
        Adapters.FirstOrDefault(adapter =>
            adapter.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase));

    public static bool IsCallable(string capability) => Find(capability) is not null;

    public static IReadOnlyList<string> GetCallableCapabilities() => Adapters
        .SelectMany(adapter => adapter.Capabilities)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToList();
}
