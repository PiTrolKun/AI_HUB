using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ChoiceExecutorCandidatePoolService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CapabilityInventoryService _inventoryService = new();
    private readonly HuggingFaceProviderTool _huggingFaceProvider = new();

    public async Task<ChoiceExecutorCandidatePool> BuildAsync(
        StorageSettings storageSettings,
        ChoiceCapabilityProfile capabilityProfile,
        string workloadMode,
        string currentCoreName,
        ComputerPassport computerPassport,
        ISessionEventLog sessionLog,
        CancellationToken cancellationToken)
    {
        var inventory = _inventoryService.Create(storageSettings);
        var request = BuildCatalogRequest(capabilityProfile, workloadMode);
        var catalog = new LocalModelCatalogTool(computerPassport: computerPassport)
            .Search(JsonSerializer.Serialize(request, JsonOptions), currentCoreName);

        sessionLog.Write("scenario_candidate_inventory", inventory);
        sessionLog.Write("scenario_candidate_catalog", new { Request = request, Response = catalog });

        var pool = CreatePool(inventory, catalog, capabilityProfile, workloadMode, computerPassport);
        if (pool.HasCandidatePair)
        {
            return pool;
        }

        var coordinatorRequest = BuildCoordinatorCatalogRequest(request);
        var coordinatorCatalog = new LocalModelCatalogTool(computerPassport: computerPassport)
            .Search(JsonSerializer.Serialize(coordinatorRequest, JsonOptions), currentCoreName);
        sessionLog.Write(
            "scenario_candidate_coordinator_catalog",
            new { Request = coordinatorRequest, Response = coordinatorCatalog });
        var coordinatorPool = CreatePool(
            inventory,
            coordinatorCatalog,
            capabilityProfile,
            workloadMode,
            computerPassport,
            ExecutionCompatibilityService.CoordinatorFallbackMatch);
        pool.AlternativeCandidates.AddRange(coordinatorPool.AlternativeCandidates);
        pool.Warnings.AddRange(coordinatorCatalog.Warnings);
        pool.AlternativeCandidates = pool.AlternativeCandidates
            .GroupBy(candidate => $"{candidate.Family}|{candidate.Model}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6)
            .ToList();
        AssignIds(pool);
        if (pool.HasCandidatePair)
        {
            return pool;
        }

        var taskType = request.TaskType.Replace(' ', '_');
        var live = await _huggingFaceProvider.FindModelAsync(
            $"role=executor query={taskType} pipeline=text-generation format=gguf",
            storageSettings,
            cancellationToken);
        pool.UsedLiveSearch = true;
        sessionLog.Write("scenario_candidate_live_search", live);
        AddLiveCandidates(pool, live, workloadMode, computerPassport);
        AssignIds(pool);
        return pool;
    }

    public static ModelCatalogSearchRequest BuildCatalogRequest(
        ChoiceCapabilityProfile profile,
        string workloadMode)
    {
        var values = profile.Dimensions
            .SelectMany(dimension => dimension.Values)
            .Select(value => value.Trim().ToLowerInvariant())
            .Where(value => value.Length > 0)
            .ToList();
        var directions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddDirection(values, directions, "agents_code", "code", "program", "software", "developer");
        AddDirection(values, directions, "science_professional", "science", "medical", "legal", "professional");
        AddDirection(values, directions, "vision_documents", "image", "vision", "document_scan", "multimodal");
        AddDirection(values, directions, "audio_speech", "audio", "speech", "voice");
        AddDirection(values, directions, "video", "video");
        AddDirection(values, directions, "image_generation", "image_generation", "generate_image");
        AddDirection(values, directions, "data_forecasting", "forecast", "statistics", "time_series");
        AddDirection(values, directions, "search_memory", "search", "memory", "rag", "retrieval");
        AddDirection(values, directions, "safety_control", "safety", "moderation", "risk");
        AddDirection(values, directions, "spatial_robotics", "robot", "spatial", "control");
        if (directions.Count == 0)
        {
            directions.Add("text_knowledge");
        }

        var taskType = profile.Dimensions
            .FirstOrDefault(dimension => string.Equals(
                dimension.Dimension,
                ChoiceDecisionDimensions.TaskType,
                StringComparison.OrdinalIgnoreCase))?
            .Values.FirstOrDefault() ?? "general_reasoning";

        return new ModelCatalogSearchRequest
        {
            Directions = directions.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            TaskType = taskType,
            RequiredCapabilities = values.Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList(),
            LoadLevel = workloadMode.ToLowerInvariant() switch
            {
                UserWorkloadModes.Light => "light",
                UserWorkloadModes.Extreme => "extreme",
                _ => "optimal"
            },
            Limit = 6
        };
    }

    public static ModelCatalogSearchRequest BuildCoordinatorCatalogRequest(
        ModelCatalogSearchRequest source) => new()
        {
            Directions = ["text_knowledge"],
            TaskType = "general_reasoning",
            RequiredCapabilities = [],
            LoadLevel = source.LoadLevel,
            Limit = source.Limit
        };

    public static ChoiceExecutorCandidatePool CreatePool(
        CapabilityInventoryResponse inventory,
        ModelCatalogSearchResponse catalog,
        ChoiceCapabilityProfile capabilityProfile,
        string workloadMode,
        ComputerPassport computerPassport,
        string catalogMatchScope = ExecutionCompatibilityService.TaskProfileMatch)
    {
        var capabilityResolution = ExecutionCompatibilityService.ResolveCapabilities(
            capabilityProfile,
            inventory);
        var pool = new ChoiceExecutorCandidatePool
        {
            RequiredProtocols = BuildRequiredProtocols(
                capabilityProfile,
                workloadMode,
                capabilityResolution),
            RequiredCapabilities = capabilityResolution.Required.ToList(),
            AvailableCapabilities = capabilityResolution.Available.ToList(),
            MissingCapabilities = capabilityResolution.Missing.ToList(),
            UnresolvedCapabilities = capabilityResolution.Unresolved.ToList(),
            Warnings = catalog.Warnings.ToList()
        };
        if (pool.UnresolvedCapabilities.Count > 0)
        {
            pool.Warnings.Add(
                "The capability profile contains requirements without an approved executable provider.");
        }

        foreach (var item in inventory.Items.Where(item =>
                     string.Equals(item.Role, "executor", StringComparison.OrdinalIgnoreCase)
                     && item.IsInstalled
                     && item.IsRunnable
                     && string.Equals(item.Format, "gguf", StringComparison.OrdinalIgnoreCase)))
        {
            var parameterCount = ModelHardwareCompatibilityService.TryReadParameterCountFromName(item.Name);
            if (!IsAllowedByWorkload(parameterCount, workloadMode))
            {
                continue;
            }

            var installedCandidate = new ChoiceExecutorPoolCandidate
            {
                Model = item.Name,
                Family = GetFamilyKey(item.Name),
                Status = ChoiceExecutorCandidateStatuses.Installed,
                Role = ExecutionCompatibilityService.CoordinatorRole,
                CapabilityClass = GetCapabilityClass(parameterCount),
                ParameterCount = parameterCount,
                PipelineTag = "text-generation",
                HardwareStatus = "runtime_verified",
                Evidence = "installed=true; runnable=true; runtime inventory"
            };
            ExecutionCompatibilityService.ApplyExecutionPassport(
                installedCandidate,
                capabilityResolution,
                ExecutionCompatibilityService.TaskProfileMatch);
            if (installedCandidate.RuntimeCompatible)
            {
                pool.InstalledCandidates.Add(installedCandidate);
            }
        }

        foreach (var candidate in catalog.Candidates)
        {
            if (candidate.Hardware.IsCompatible == false
                || !IsAllowedByWorkload(candidate.ParameterCount, workloadMode)
                || !ExecutionCompatibilityService.IsLlamaCoordinatorCandidate(candidate))
            {
                continue;
            }

            var family = GetFamilyKey(candidate.RepoId, candidate.ModelType, candidate.BaseModels);
            if (pool.InstalledCandidates.Count == 1
                && string.Equals(pool.InstalledCandidates[0].Family, family, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var alternativeCandidate = new ChoiceExecutorPoolCandidate
            {
                Model = candidate.RepoId,
                Family = family,
                Status = ChoiceExecutorCandidateStatuses.NotInstalled,
                Role = ExecutionCompatibilityService.CoordinatorRole,
                CapabilityClass = GetCapabilityClass(candidate.ParameterCount),
                ParameterCount = candidate.ParameterCount,
                PipelineTag = candidate.PipelineTag,
                ModelType = candidate.ModelType,
                Directions = candidate.Directions.ToList(),
                Roles = candidate.Roles.ToList(),
                HardwareStatus = candidate.Hardware.Status,
                Evidence = string.Join("; ", candidate.MatchReasons)
            };
            ExecutionCompatibilityService.ApplyExecutionPassport(
                alternativeCandidate,
                capabilityResolution,
                catalogMatchScope);
            if (alternativeCandidate.RuntimeCompatible)
            {
                pool.AlternativeCandidates.Add(alternativeCandidate);
            }
        }

        pool.AlternativeCandidates = pool.AlternativeCandidates
            .GroupBy(candidate => $"{candidate.Family}|{candidate.Model}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6)
            .ToList();
        AssignIds(pool);
        return pool;
    }

    public static bool TryApplySelection(
        ChoiceTaskCard card,
        ChoiceExecutorCandidatePool pool,
        out string error)
    {
        error = string.Empty;
        var selection = card.ExecutorSelection;
        var installed = pool.InstalledCandidates.FirstOrDefault(candidate => string.Equals(
            candidate.Id,
            selection.InstalledCandidateId,
            StringComparison.OrdinalIgnoreCase));
        var alternative = pool.AlternativeCandidates.FirstOrDefault(candidate => string.Equals(
            candidate.Id,
            selection.AlternativeCandidateId,
            StringComparison.OrdinalIgnoreCase));
        if (installed is null || alternative is null)
        {
            error = "Executor selection must use one installed ID and one alternative ID from the trusted candidate pool.";
            return false;
        }

        if (string.Equals(installed.Family, alternative.Family, StringComparison.OrdinalIgnoreCase))
        {
            error = "The downloadable alternative must belong to a different model family than the installed choice.";
            return false;
        }

        if (!string.Equals(selection.PreferredCandidateId, installed.Id, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(selection.PreferredCandidateId, alternative.Id, StringComparison.OrdinalIgnoreCase))
        {
            error = "preferredCandidateId must match one of the two selected trusted candidate IDs.";
            return false;
        }

        var installedChoice = CreateResolvedCandidate(
            installed,
            string.Equals(selection.PreferredCandidateId, installed.Id, StringComparison.OrdinalIgnoreCase));
        var alternativeChoice = CreateResolvedCandidate(
            alternative,
            string.Equals(selection.PreferredCandidateId, alternative.Id, StringComparison.OrdinalIgnoreCase));
        card.ExecutorCandidates = [installedChoice, alternativeChoice];

        var preferred = installedChoice.IsRecommended ? installedChoice : alternativeChoice;
        card.RecommendedExecutor = preferred.Model;
        card.ExecutorStatus = preferred.Status;
        card.ExecutorRole = preferred.Role;
        card.ExecutorCapabilityClass = preferred.CapabilityClass;
        card.ExecutorReason = preferred.Reason;
        ApplyToolProtocols(card);
        return true;
    }

    public static string BuildSelectionPrompt(ChoiceExecutorCandidatePool pool)
    {
        var builder = new StringBuilder();
        builder.AppendLine("TRUSTED_EXECUTOR_CANDIDATE_POOL");
        builder.AppendLine("Program already verified identity, installation status, family, runtime, artifact format, component plan and PC fit.");
        builder.AppendLine("Choose IDs only. Do not rewrite model names, status, family, role or capability class.");
        builder.AppendLine("A coordinator model does not directly provide specialist file/media operations. Those are separate component capabilities.");
        builder.AppendLine("Required capability protocols:");
        foreach (var protocol in pool.RequiredProtocols)
        {
            builder.AppendLine($"- {protocol}");
        }

        builder.AppendLine("Installed runnable candidates:");
        foreach (var candidate in pool.InstalledCandidates)
        {
            AppendCandidate(builder, candidate);
        }

        builder.AppendLine("Download alternatives from other families:");
        foreach (var candidate in pool.AlternativeCandidates)
        {
            AppendCandidate(builder, candidate);
        }

        builder.AppendLine("Return final_task_card with executorSelection only:");
        builder.AppendLine("- installedCandidateId: exactly one installed_* ID;");
        builder.AppendLine("- alternativeCandidateId: exactly one alternative_* ID from a different family;");
        builder.AppendLine("- preferredCandidateId: either selected ID. Prefer installed when it is sufficient; downloading is not automatically better;");
        builder.AppendLine("Program generates all technical advantages, limitations and reasons from verified candidate facts.");
        return builder.ToString().Trim();
    }

    public static string GetFamilyKey(
        string model,
        string modelType = "",
        IReadOnlyList<string>? baseModels = null)
    {
        var source = string.Join(' ', new[]
        {
            model,
            modelType,
            baseModels?.FirstOrDefault() ?? string.Empty
        }).ToLowerInvariant();
        string[] knownFamilies =
        [
            "gpt-oss", "qwen", "gemma", "llama", "mistral", "mixtral", "deepseek", "phi",
            "command-r", "command", "yi", "falcon", "rwkv", "internlm", "glm", "nemotron",
            "granite", "olmo", "exaone", "minicpm", "baichuan", "bloom", "mpt"
        ];
        var family = knownFamilies.FirstOrDefault(value => source.Contains(value, StringComparison.OrdinalIgnoreCase));
        if (family is not null)
        {
            return family;
        }

        var name = model.Split('/').LastOrDefault() ?? model;
        name = Regex.Replace(name, @"(?i)(?:[-_.](?:i?q)\d.*|[-_.]gguf.*)$", string.Empty);
        name = Regex.Replace(name, @"(?i)[-_.](?:\d+(?:\.\d+)?b|instruct|chat|it)$", string.Empty);
        var token = Regex.Split(name.ToLowerInvariant(), "[-_. ]+")
            .FirstOrDefault(value => value.Length > 1 && !char.IsDigit(value[0]));
        return token ?? name.ToLowerInvariant();
    }

    private static void AddLiveCandidates(
        ChoiceExecutorCandidatePool pool,
        HuggingFaceFindModelResponse live,
        string workloadMode,
        ComputerPassport computerPassport)
    {
        foreach (var candidate in live.Candidates)
        {
            var parameterCount = ModelHardwareCompatibilityService.TryReadParameterCountFromName(candidate.RepoId);
            var hardware = ModelHardwareCompatibilityService.Assess(parameterCount, computerPassport, workloadMode);
            if (hardware.IsCompatible == false
                || !IsAllowedByWorkload(parameterCount, workloadMode)
                || !ExecutionCompatibilityService.IsLlamaCoordinatorPipeline(candidate.PipelineTag))
            {
                continue;
            }

            var family = GetFamilyKey(candidate.RepoId);
            if (pool.InstalledCandidates.Count == 1
                && string.Equals(pool.InstalledCandidates[0].Family, family, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var liveCandidate = new ChoiceExecutorPoolCandidate
            {
                Model = candidate.RepoId,
                Family = family,
                Status = ChoiceExecutorCandidateStatuses.NotInstalled,
                Role = ExecutionCompatibilityService.CoordinatorRole,
                CapabilityClass = GetCapabilityClass(parameterCount),
                ParameterCount = parameterCount,
                PipelineTag = candidate.PipelineTag,
                HardwareStatus = hardware.Status,
                Evidence = "live Hugging Face search"
            };
            ExecutionCompatibilityService.ApplyExecutionPassport(
                liveCandidate,
                new ExecutionCapabilityResolution
                {
                    Required = pool.RequiredCapabilities.ToList(),
                    Available = pool.AvailableCapabilities.ToList(),
                    Missing = pool.MissingCapabilities.ToList(),
                    Unresolved = pool.UnresolvedCapabilities.ToList()
                },
                ExecutionCompatibilityService.CoordinatorFallbackMatch);
            if (liveCandidate.RuntimeCompatible)
            {
                pool.AlternativeCandidates.Add(liveCandidate);
            }
        }

        pool.AlternativeCandidates = pool.AlternativeCandidates
            .GroupBy(candidate => $"{candidate.Family}|{candidate.Model}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(6)
            .ToList();
    }

    private static List<string> BuildRequiredProtocols(
        ChoiceCapabilityProfile profile,
        string workloadMode,
        ExecutionCapabilityResolution capabilityResolution)
    {
        var protocols = profile.Dimensions
            .Where(dimension => dimension.Status is ChoiceDimensionStatuses.Resolved or ChoiceDimensionStatuses.Provisional)
            .Select(dimension => $"{dimension.Dimension}={string.Join(',', dimension.Values)}")
            .ToList();
        protocols.Add($"workload_mode={workloadMode}");
        protocols.Add($"coordinator_runtime={ExecutionCompatibilityService.LlamaRuntime}");
        protocols.Add($"coordinator_artifact={ExecutionCompatibilityService.GgufArtifact}");
        protocols.AddRange(capabilityResolution.Required
            .Select(capability => $"required_component_capability={capability}"));
        protocols.AddRange(capabilityResolution.Missing
            .Select(capability => $"component_download_required={capability}"));
        protocols.AddRange(capabilityResolution.Unresolved
            .Select(capability => $"unresolved_capability={capability}"));
        return protocols;
    }

    private static void ApplyToolProtocols(ChoiceTaskCard card)
    {
        var values = card.CapabilityProfile.Dimensions
            .SelectMany(dimension => dimension.Values)
            .Select(value => value.ToLowerInvariant())
            .ToList();
        var offline = values.Any(value => value is "no_external_data" or "offline_only" or "local_only");
        var requiresFreshData = values.Any(value =>
            value.Contains("web", StringComparison.Ordinal)
            || value.Contains("current", StringComparison.Ordinal)
            || value.Contains("fresh", StringComparison.Ordinal)
            || value.Contains("live", StringComparison.Ordinal));
        var taskType = card.CapabilityProfile.Dimensions
            .FirstOrDefault(dimension => string.Equals(
                dimension.Dimension,
                ChoiceDecisionDimensions.TaskType,
                StringComparison.OrdinalIgnoreCase))?
            .Values.FirstOrDefault() ?? string.Empty;

        card.NeedsWeb = !offline && requiresFreshData;
        card.RequiredTools = card.NeedsWeb
            ? [taskType.Contains("research", StringComparison.OrdinalIgnoreCase) ? "web_research" : "web_search"]
            : [];
    }

    private static ChoiceExecutorCandidate CreateResolvedCandidate(
        ChoiceExecutorPoolCandidate candidate,
        bool preferred) => new()
        {
            Model = candidate.Model,
            Family = candidate.Family,
            Status = candidate.Status,
            Role = candidate.Role,
            CapabilityClass = candidate.CapabilityClass,
            Advantage = candidate.Status == ChoiceExecutorCandidateStatuses.Installed
                ? "verified_installed_coordinator"
                : candidate.CatalogMatchScope,
            Limitation = candidate.UnresolvedCapabilities.Count > 0
                ? "required_capability_has_no_approved_provider"
                : candidate.MissingCapabilities.Count > 0
                    ? "approved_components_require_acquisition"
                    : "specialist_operations_are_separate_capabilities",
            Reason = candidate.Status == ChoiceExecutorCandidateStatuses.Installed
                ? "Program verified this installed model as a runnable coordinator."
                : "Program verified this different-family catalog model as a downloadable coordinator candidate.",
            IsRecommended = preferred,
            RuntimeBackend = candidate.RuntimeBackend,
            ArtifactFormat = candidate.ArtifactFormat,
            CatalogMatchScope = candidate.CatalogMatchScope,
            RequiredCapabilities = candidate.RequiredCapabilities.ToList(),
            AvailableCapabilities = candidate.AvailableCapabilities.ToList(),
            MissingCapabilities = candidate.MissingCapabilities.ToList(),
            UnresolvedCapabilities = candidate.UnresolvedCapabilities.ToList()
        };

    private static void AppendCandidate(StringBuilder builder, ChoiceExecutorPoolCandidate candidate)
    {
        builder.AppendLine(
            $"- id={candidate.Id}; model={candidate.Model}; family={candidate.Family}; "
            + $"parameters={candidate.ParameterCount?.ToString() ?? "unknown"}; pipeline={candidate.PipelineTag}; "
            + $"directions={string.Join(',', candidate.Directions)}; roles={string.Join(',', candidate.Roles)}; "
            + $"runtime={candidate.RuntimeBackend}; artifact={candidate.ArtifactFormat}; "
            + $"matchScope={candidate.CatalogMatchScope}; missingComponents={string.Join(',', candidate.MissingCapabilities)}; "
            + $"unresolvedCapabilities={string.Join(',', candidate.UnresolvedCapabilities)}; "
            + $"hardware={candidate.HardwareStatus}; evidence={candidate.Evidence}");
    }

    private static void AssignIds(ChoiceExecutorCandidatePool pool)
    {
        for (var index = 0; index < pool.InstalledCandidates.Count; index++)
        {
            pool.InstalledCandidates[index].Id = $"installed_{index + 1}";
        }
        for (var index = 0; index < pool.AlternativeCandidates.Count; index++)
        {
            pool.AlternativeCandidates[index].Id = $"alternative_{index + 1}";
        }
    }

    private static bool IsAllowedByWorkload(long? parameterCount, string workloadMode) =>
        string.Equals(workloadMode, UserWorkloadModes.Light, StringComparison.OrdinalIgnoreCase)
        || parameterCount is > 8_000_000_000;

    private static string GetCapabilityClass(long? parameterCount) =>
        parameterCount is > 8_000_000_000
            ? ChoiceExecutorPolicy.Above8B
            : ChoiceExecutorPolicy.EightBOrLess;

    private static void AddDirection(
        IEnumerable<string> values,
        ISet<string> directions,
        string direction,
        params string[] markers)
    {
        if (values.Any(value => markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))))
        {
            directions.Add(direction);
        }
    }
}
