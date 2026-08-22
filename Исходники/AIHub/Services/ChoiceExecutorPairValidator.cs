using AIHub.Models;

namespace AIHub.Services;

public static class ChoiceExecutorPairValidator
{
    public static bool Validate(
        ChoiceTaskCard card,
        ChoiceExecutorCandidatePool pool,
        string workloadMode,
        string currentCoreName,
        ComputerPassport computerPassport,
        out string error)
    {
        error = string.Empty;
        if (card.ExecutorCandidates.Count is < 1 or > 2)
        {
            error = "The resolved executor choice must contain one or two trusted coordinator candidates.";
            return false;
        }

        if (card.ExecutorCandidates.Count(candidate => candidate.IsRecommended) != 1
            || !card.ExecutorCandidates.Any(candidate => candidate.IsRecommended
                && string.Equals(candidate.Model, card.RecommendedExecutor, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Exactly one trusted candidate must be preferred and match recommendedExecutor.";
            return false;
        }

        foreach (var candidate in card.ExecutorCandidates)
        {
            var source = candidate.Status switch
            {
                ChoiceExecutorCandidateStatuses.Installed => pool.InstalledCandidates.FirstOrDefault(item =>
                    string.Equals(item.Model, candidate.Model, StringComparison.OrdinalIgnoreCase)),
                ChoiceExecutorCandidateStatuses.NotInstalled => pool.AlternativeCandidates.FirstOrDefault(item =>
                    string.Equals(item.Model, candidate.Model, StringComparison.OrdinalIgnoreCase)),
                _ => null
            };
            if (source is null)
            {
                error = $"Executor candidate '{candidate.Model}' is outside the trusted candidate pool.";
                return false;
            }
            if (!source.RuntimeCompatible)
            {
                error = $"Executor candidate '{candidate.Model}' has no verified coordinator runtime.";
                return false;
            }

            var policyMetadata = new ModelCatalogCandidate
            {
                RepoId = source.Model,
                ParameterCount = source.ParameterCount,
                ModelType = source.ModelType,
                Directions = source.Directions.ToList(),
                Roles = source.Roles.ToList()
            };
            if (!ChoiceExecutorPolicy.Validate(
                    CreateCandidatePolicyCard(card, candidate),
                    workloadMode,
                    modelSearchUnavailable: false,
                    currentCoreName,
                    policyMetadata,
                    out error))
            {
                error = $"Executor candidate '{candidate.Model}' violates policy: {error}";
                return false;
            }

            if (source.ParameterCount is not null && candidate.Status == ChoiceExecutorCandidateStatuses.NotInstalled)
            {
                var hardware = ModelHardwareCompatibilityService.Assess(
                    source.ParameterCount,
                    computerPassport,
                    workloadMode);
                if (hardware.IsCompatible == false)
                {
                    error = $"Executor candidate '{candidate.Model}' does not fit the current PC: {hardware.Reason}";
                    return false;
                }
            }
        }

        return true;
    }

    public static bool Validate(
        ChoiceTaskCard card,
        IReadOnlyList<string> toolEvidence,
        bool modelSearchUnavailable,
        string workloadMode,
        string currentCoreName,
        ComputerPassport computerPassport,
        out string error)
    {
        error = string.Empty;
        foreach (var candidate in card.ExecutorCandidates)
        {
            var isInstalledEvidence = ChoiceModelCandidateSelector.IsRunnableInstalledInventoryChoice(
                candidate.Model,
                toolEvidence);
            if (candidate.Status == ChoiceExecutorCandidateStatuses.Installed && !isInstalledEvidence)
            {
                error = $"Installed executor candidate '{candidate.Model}' is not a runnable installed inventory model.";
                return false;
            }

            if (candidate.Status == ChoiceExecutorCandidateStatuses.NotInstalled
                && (isInstalledEvidence || !ChoiceModelCandidateSelector.IsVerifiedChoice(candidate.Model, toolEvidence)))
            {
                error = $"Download executor candidate '{candidate.Model}' must be confirmed by catalog/HF evidence and absent from runnable inventory.";
                return false;
            }

            ChoiceModelCandidateSelector.TryGetCatalogCandidate(candidate.Model, toolEvidence, out var catalogCandidate);
            var hasCatalogCandidate = !string.IsNullOrWhiteSpace(catalogCandidate.RepoId);
            if (!ChoiceExecutorPolicy.Validate(
                    CreateCandidatePolicyCard(card, candidate),
                    workloadMode,
                    modelSearchUnavailable,
                    currentCoreName,
                    hasCatalogCandidate ? catalogCandidate : null,
                    out error))
            {
                error = $"Executor candidate '{candidate.Model}' violates policy: {error}";
                return false;
            }

            if (isInstalledEvidence)
            {
                continue;
            }

            long? parameterCount = hasCatalogCandidate
                ? catalogCandidate.ParameterCount
                : ChoiceModelCandidateSelector.TryGetVerifiedParameterCount(
                    candidate.Model,
                    toolEvidence,
                    out var verifiedParameterCount)
                    ? verifiedParameterCount
                    : ModelHardwareCompatibilityService.TryReadParameterCountFromName(candidate.Model);
            var hardware = ModelHardwareCompatibilityService.Assess(parameterCount, computerPassport, workloadMode);
            if (hardware.IsCompatible == false)
            {
                error = $"Executor candidate '{candidate.Model}' does not fit the current PC: {hardware.Reason}";
                return false;
            }

            if (hardware.IsCompatible is null
                && !string.Equals(workloadMode, UserWorkloadModes.Light, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Executor candidate '{candidate.Model}' has insufficient metadata for mandatory PC compatibility verification.";
                return false;
            }
        }

        return true;
    }

    private static ChoiceTaskCard CreateCandidatePolicyCard(
        ChoiceTaskCard source,
        ChoiceExecutorCandidate candidate) => new()
        {
            RecommendedExecutor = candidate.Model,
            ExecutorRole = candidate.Role,
            ExecutorCapabilityClass = candidate.CapabilityClass,
            ExecutorStatus = candidate.Status,
            CapabilityProfile = source.CapabilityProfile
        };
}
