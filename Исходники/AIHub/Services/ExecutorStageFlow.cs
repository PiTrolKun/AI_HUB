namespace AIHub.Services;

public static class ExecutorStageIds
{
    public const string TaskDefinition = "task_definition";
    public const string SolutionMethod = "solution_method";
    public const string DataCollection = "data_collection";
    public const string ResultAssembly = "result_assembly";
}

public static class ExecutorStageFlow
{
    private static readonly string[] StageIds =
    [
        ExecutorStageIds.TaskDefinition,
        ExecutorStageIds.SolutionMethod,
        ExecutorStageIds.DataCollection,
        ExecutorStageIds.ResultAssembly
    ];

    public static IReadOnlyList<string> ActiveStageIds => StageIds;

    public static bool IsKnown(string stageId) =>
        StageIds.Contains(stageId, StringComparer.Ordinal);

    public static int GetIndex(string stageId) =>
        Array.FindIndex(StageIds, value => string.Equals(value, stageId, StringComparison.Ordinal));

    public static string? GetPrevious(string stageId)
    {
        var index = GetIndex(stageId);
        return index > 0 ? StageIds[index - 1] : null;
    }

    public static string? GetNext(string stageId)
    {
        var index = GetIndex(stageId);
        return index >= 0 && index < StageIds.Length - 1 ? StageIds[index + 1] : null;
    }

    public static bool AreAdjacent(string firstStageId, string secondStageId)
    {
        var firstIndex = GetIndex(firstStageId);
        var secondIndex = GetIndex(secondStageId);
        return firstIndex >= 0 && secondIndex >= 0 && Math.Abs(firstIndex - secondIndex) == 1;
    }
}
