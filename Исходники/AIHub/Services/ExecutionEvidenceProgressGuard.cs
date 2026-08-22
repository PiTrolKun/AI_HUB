using AIHub.Models;

namespace AIHub.Services;

public sealed class ExecutionEvidenceProgressGuard
{
    private readonly HashSet<string> _completedRequiredActions =
        new(StringComparer.OrdinalIgnoreCase);
    private int _stagnantObservations;

    public void Reset(ExecutionActionGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        _completedRequiredActions.Clear();
        foreach (var node in GetCompletedRequiredActions(graph))
        {
            _completedRequiredActions.Add(node);
        }

        _stagnantObservations = 0;
    }

    public bool Observe(ExecutionActionGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        var current = GetCompletedRequiredActions(graph);
        var progressed = current.Any(id => _completedRequiredActions.Add(id));
        _stagnantObservations = progressed ? 0 : _stagnantObservations + 1;
        return _stagnantObservations < 2;
    }

    private static List<string> GetCompletedRequiredActions(ExecutionActionGraph graph) =>
        graph.Nodes
            .Where(ExecutionActionGraphService.IsOperational)
            .Where(node => node.Required)
            .Where(node => string.Equals(
                node.Status,
                ExecutionActionStatuses.Succeeded,
                StringComparison.OrdinalIgnoreCase))
            .Select(node => node.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
