using AIHub.Models;

namespace AIHub.Services;

internal static class ExecutorTurnStagePolicy
{
    public static bool IsAllowed(
        ExecutorTurnResult turn,
        string currentStageId,
        bool briefConfirmed) =>
        GetRejectionReason(turn, currentStageId, briefConfirmed) is null;

    public static string? GetRejectionReason(
        ExecutorTurnResult turn,
        string currentStageId,
        bool briefConfirmed)
    {
        if (!ExecutorStageFlow.IsKnown(currentStageId))
        {
            return "unknown_stage";
        }

        if (turn.Action == ExecutorTurnActions.RequestCapability)
        {
            return turn.Status == ExecutorTurnStatuses.Working
                && turn.RequestedCapabilities.Count > 0
                && turn.RequestedCapabilities.All(request =>
                    !string.IsNullOrWhiteSpace(request.Id)
                    && !string.IsNullOrWhiteSpace(request.Purpose))
                ? null
                : "invalid_capability_request";
        }

        if (turn.Action == ExecutorTurnActions.ConfirmBrief)
        {
            return !briefConfirmed
                && currentStageId == ExecutorStageIds.TaskDefinition
                && turn.Status == ExecutorTurnStatuses.StageReady
                ? null
                : "brief_confirmation_not_allowed";
        }

        if (!briefConfirmed)
        {
            return currentStageId == ExecutorStageIds.TaskDefinition
                && turn.Status is ExecutorTurnStatuses.Working
                    or ExecutorTurnStatuses.Blocked
                && (turn.Action is ExecutorTurnActions.AskUser
                    or ExecutorTurnActions.Blocked)
                && !turn.CanFinalize
                ? null
                : "task_definition_policy_violation";
        }

        if (currentStageId != ExecutorStageIds.PracticalClarification)
        {
            return "practical_stage_required";
        }

        if (turn.Status == ExecutorTurnStatuses.StageReady)
        {
            return "stage_ready_after_brief_confirmation";
        }

        if (turn.Action == ExecutorTurnActions.RequestTool)
        {
            return turn.Status == ExecutorTurnStatuses.Working
                && turn.RequestedTools.Count > 0
                ? null
                : "invalid_tool_request";
        }

        if (turn.Action == ExecutorTurnActions.SuggestFinalization)
        {
            if (turn.Status != ExecutorTurnStatuses.Working)
            {
                return "suggest_finalization_requires_working_status";
            }
            if (!turn.CanFinalize || string.IsNullOrWhiteSpace(turn.CompletionReason))
            {
                return "finalization_readiness_missing";
            }
            if (string.IsNullOrWhiteSpace(turn.CurrentResultSummary))
            {
                return "current_result_summary_missing";
            }
            if (!ExecutorWorkingResultPolicy.IsSubstantive(turn.WorkingResultFragment))
            {
                return "working_result_fragment_missing";
            }
            return turn.MissingCriticalInputs.Count == 0
                ? null
                : "critical_inputs_still_missing";
        }

        if (turn.Action == ExecutorTurnActions.AskUser)
        {
            if (turn.Status != ExecutorTurnStatuses.Working)
            {
                return "ask_user_requires_working_status";
            }
            if (string.IsNullOrWhiteSpace(turn.CurrentResultSummary))
            {
                return "current_result_summary_missing";
            }
            return ExecutorWorkingResultPolicy.IsSubstantive(turn.WorkingResultFragment)
                ? null
                : "working_result_fragment_missing";
        }

        return turn.Status == ExecutorTurnStatuses.Blocked
            && turn.Action == ExecutorTurnActions.Blocked
            ? null
            : "action_not_allowed_in_current_stage";
    }
}
