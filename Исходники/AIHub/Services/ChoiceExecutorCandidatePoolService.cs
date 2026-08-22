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
        CancellationToken cancellationToken,
        SessionFilePromptManifest? fileManifest = null,
        WorkPatternSelectionResult? workPatterns = null)
    {
        var inventory = _inventoryService.Create(storageSettings);
        var request = BuildCatalogRequest(capabilityProfile, workloadMode);
        var catalog = new LocalModelCatalogTool(computerPassport: computerPassport)
            .Search(JsonSerializer.Serialize(request, JsonOptions), currentCoreName);

        sessionLog.Write("scenario_candidate_inventory", inventory);
        sessionLog.Write("scenario_candidate_catalog", new { Request = request, Response = catalog });

        var pool = CreatePool(
            inventory,
            catalog,
            capabilityProfile,
            workloadMode,
            computerPassport,
            fileManifest: fileManifest,
            workPatterns: workPatterns);
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
            ExecutionCompatibilityService.CoordinatorFallbackMatch,
            fileManifest,
            workPatterns);
        pool.AlternativeCandidates.AddRange(coordinatorPool.AlternativeCandidates);
        pool.Warnings.AddRange(coordinatorCatalog.Warnings);
        pool.AlternativeCandidates = pool.AlternativeCandidates
            .GroupBy(candidate => $"{candidate.Family}|{candidate.Model}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(candidate => candidate.ConditionalMatchPercent)
            .ThenBy(candidate => candidate.Model, StringComparer.OrdinalIgnoreCase)
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
        AddLiveCandidates(
            pool,
            live,
            workloadMode,
            computerPassport,
            capabilityProfile,
            workPatterns);
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
            Directions = source.Directions
                .Append("text_knowledge")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            TaskType = string.IsNullOrWhiteSpace(source.TaskType)
                ? "general_reasoning"
                : source.TaskType,
            RequiredCapabilities = source.RequiredCapabilities
                .Where(capability =>
                    !capability.StartsWith("read.", StringComparison.OrdinalIgnoreCase)
                    && !capability.StartsWith("edit.", StringComparison.OrdinalIgnoreCase)
                    && !capability.StartsWith("generate.", StringComparison.OrdinalIgnoreCase)
                    && !capability.StartsWith("analyze.", StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList(),
            LoadLevel = source.LoadLevel,
            Limit = source.Limit
        };

    public static ChoiceExecutorCandidatePool CreatePool(
        CapabilityInventoryResponse inventory,
        ModelCatalogSearchResponse catalog,
        ChoiceCapabilityProfile capabilityProfile,
        string workloadMode,
        ComputerPassport computerPassport,
        string catalogMatchScope = ExecutionCompatibilityService.TaskProfileMatch,
        SessionFilePromptManifest? fileManifest = null,
        WorkPatternSelectionResult? workPatterns = null,
        ExecutionRoutePlannerService? routePlanner = null)
    {
        workPatterns ??= new WorkPatternSelectionResult
        {
            Selections =
            [
                new WorkPatternSelection
                {
                    PatternId = "other.custom",
                    MatchPercent = 100,
                    Reason = "Compatibility fallback for callers without Sandbox classification."
                }
            ],
            Source = "program_fallback",
            UsedFallback = true
        };
        var catalogService = new WorkPatternCatalogService();
        var selectedPatterns = catalogService.ResolveSelected(workPatterns);
        var artifactContract = new ArtifactContractBuilder().Build(
            selectedPatterns,
            fileManifest);
        var outcomeContract = new ExecutionOutcomeContractService().Build(
            BuildOutcomeGoal(selectedPatterns, capabilityProfile),
            capabilityProfile,
            fileManifest,
            selectedPatterns,
            artifactContract);
        var executionRoute = (routePlanner ?? new ExecutionRoutePlannerService()).Build(
            capabilityProfile,
            fileManifest,
            "Prepare the verified execution route before selecting a coordinator.",
            selectedPatterns,
            outcomeContract: outcomeContract);
        var executionBundle = new ExecutionBundlePlannerService().Build(
            workPatterns,
            artifactContract,
            executionRoute);
        var capabilityResolution = CreateCapabilityResolution(executionRoute);
        var pool = new ChoiceExecutorCandidatePool
        {
            WorkPatterns = workPatterns,
            ArtifactContract = artifactContract,
            OutcomeContract = outcomeContract,
            ExecutionBundle = executionBundle,
            ExecutionRoute = executionRoute,
            RequiredProtocols = BuildRequiredProtocols(
                capabilityProfile,
                workloadMode,
                capabilityResolution),
            RequiredCapabilities = capabilityResolution.Required.ToList(),
            AvailableCapabilities = capabilityResolution.Available.ToList(),
            MissingCapabilities = capabilityResolution.Missing.ToList(),
            UnresolvedCapabilities = capabilityResolution.Unresolved.ToList(),
            AvailableComponentIds = ComponentCatalog.Processing
                .Where(entry => entry.IsVisibleToAi)
                .Select(entry => entry.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Warnings = catalog.Warnings.ToList()
        };
        pool.Warnings.AddRange(executionRoute.Warnings);
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
                Evidence = "installed=true; runnable=true; runtime inventory",
                SemanticDescriptionRu = item.SemanticDescriptionRu,
                SemanticDescriptionEn = item.SemanticDescriptionEn
            };
            ExecutionCompatibilityService.ApplyExecutionPassport(
                installedCandidate,
                capabilityResolution,
                ExecutionCompatibilityService.TaskProfileMatch);
            if (installedCandidate.RuntimeCompatible)
            {
                ApplyConditionalMatch(
                    installedCandidate,
                    selectedPatterns,
                    capabilityProfile,
                    installed: true);
                installedCandidate.RouteCoveragePercent = executionRoute.OutcomeCoveragePercent;
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
                ApplyConditionalMatch(
                    alternativeCandidate,
                    selectedPatterns,
                    capabilityProfile,
                    installed: false);
                alternativeCandidate.RouteCoveragePercent = executionRoute.OutcomeCoveragePercent;
                pool.AlternativeCandidates.Add(alternativeCandidate);
            }
        }

        pool.InstalledCandidates = pool.InstalledCandidates
            .OrderByDescending(candidate => candidate.ConditionalMatchPercent)
            .ThenBy(candidate => candidate.Model, StringComparer.OrdinalIgnoreCase)
            .ToList();
        pool.AlternativeCandidates = pool.AlternativeCandidates
            .GroupBy(candidate => $"{candidate.Family}|{candidate.Model}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(candidate => candidate.ConditionalMatchPercent)
            .ThenBy(candidate => candidate.Model, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();
        AssignIds(pool);
        return pool;
    }

    public static bool TryApplySelection(
        ChoiceTaskCard card,
        ChoiceExecutorCandidatePool pool,
        out string error,
        ExecutionRoutePlannerService? routePlanner = null)
    {
        error = string.Empty;
        if (!TryValidateExecutionPlan(card.ExecutionPlan, pool, out error))
        {
            return false;
        }

        var selection = card.ExecutorSelection;
        var installed = pool.InstalledCandidates.FirstOrDefault(candidate => string.Equals(
            candidate.Id,
            selection.InstalledCandidateId,
            StringComparison.OrdinalIgnoreCase));
        var alternative = pool.AlternativeCandidates.FirstOrDefault(candidate => string.Equals(
            candidate.Id,
            selection.AlternativeCandidateId,
            StringComparison.OrdinalIgnoreCase));
        if (installed is null && alternative is null)
        {
            error = "Executor selection must contain at least one trusted coordinator candidate ID.";
            return false;
        }

        var selectedSources = new[] { installed, alternative }
            .Where(candidate => candidate is not null)
            .Cast<ChoiceExecutorPoolCandidate>()
            .DistinctBy(candidate => candidate.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!selectedSources.Any(candidate => string.Equals(
                selection.PreferredCandidateId,
                candidate.Id,
                StringComparison.OrdinalIgnoreCase)))
        {
            error = "preferredCandidateId must match one of the selected trusted coordinator IDs.";
            return false;
        }

        card.ExecutorCandidates = selectedSources
            .Select(candidate => CreateResolvedCandidate(
                candidate,
                string.Equals(
                    selection.PreferredCandidateId,
                    candidate.Id,
                    StringComparison.OrdinalIgnoreCase)))
            .ToList();
        card.WorkPatterns = pool.WorkPatterns;
        card.ArtifactContract = pool.ArtifactContract;
        card.OutcomeContract = CloneOutcomeContract(pool.OutcomeContract, card.Goal);
        card.ExecutionRoute = (routePlanner ?? new ExecutionRoutePlannerService()).ApplyExecutionPlan(
            pool.ExecutionRoute,
            card.ExecutionPlan,
            "Validate the complete execution bundle authored by the core.",
            card.OutcomeContract);
        card.ExecutionBundle = new ExecutionBundlePlannerService().Build(
            card.WorkPatterns,
            card.ArtifactContract,
            card.ExecutionRoute);
        if (alternative is not null
            && (card.ExecutionRoute.HasBlockedRequirements
                || !card.ExecutionRoute.HasCompleteOutcomeCoverage))
        {
            error = "A downloadable coordinator may be offered only with a complete execution bundle. "
                + "Remove alternativeCandidateId or choose trusted components with working adapters for every required capability.";
            return false;
        }

        foreach (var candidate in card.ExecutorCandidates)
        {
            ApplyResolvedRoute(candidate, card.ExecutionRoute);
        }

        var preferred = card.ExecutorCandidates.Single(candidate => candidate.IsRecommended);
        card.RecommendedExecutor = preferred.Model;
        card.ExecutorStatus = preferred.Status;
        card.ExecutorRole = string.IsNullOrWhiteSpace(card.ExecutionPlan.ExecutorRole)
            ? preferred.Role
            : card.ExecutionPlan.ExecutorRole.Trim();
        card.ExecutorCapabilityClass = preferred.CapabilityClass;
        card.ExecutorReason = string.IsNullOrWhiteSpace(card.ExecutionPlan.Rationale)
            ? preferred.Reason
            : card.ExecutionPlan.Rationale.Trim();
        ApplyToolProtocols(card);
        return true;
    }

    public static string BuildSelectionPrompt(ChoiceExecutorCandidatePool pool)
    {
        var builder = new StringBuilder();
        builder.AppendLine("TRUSTED_EXECUTOR_CANDIDATE_POOL");
        builder.AppendLine("Program verified factual inventory: identity, installation status, runtime, adapters, package state and PC fit.");
        builder.AppendLine("You own the semantic execution plan. Choose the coordinator and compose the complete bundle of required and optional capabilities.");
        builder.AppendLine("The program may reject impossible facts, but it must not replace your capability plan with a scripted task route.");
        builder.AppendLine("Choose candidate and component IDs only from this inventory. Do not rewrite their verified facts.");
        builder.AppendLine("A coordinator model does not directly provide specialist file/media operations. Add the exact component capabilities needed for decoding, semantic understanding, transformation and output.");
        builder.AppendLine("The installed coordinator may be selected for an incomplete but useful route. A downloadable recommendation is worthy only when its complete bundle covers every required action and has a small reserve.");
        builder.AppendLine("Before choosing, silently test: can this coordinator plus the selected capabilities complete the actual abstract task and produce the requested artifact without inventing access?");
        builder.AppendLine("A downloaded or installed package with adapterReady=false is not an executable model tool. Do not describe it as ready.");
        builder.AppendLine("Required capability protocols:");
        foreach (var protocol in pool.RequiredProtocols)
        {
            builder.AppendLine($"- {protocol}");
        }

        builder.AppendLine("Sandbox work-pattern matches:");
        foreach (var pattern in pool.WorkPatterns.Selections)
        {
            builder.AppendLine(
                $"- id={pattern.PatternId}; conditionalMatch={pattern.MatchPercent}%; "
                + $"reason={NormalizePromptValue(pattern.Reason)}");
        }

        builder.AppendLine(
            $"Artifact contract: kind={pool.ArtifactContract.ArtifactKind}; "
            + $"extension={pool.ArtifactContract.PreferredExtension}; "
            + $"mime={pool.ArtifactContract.MimeType}; "
            + $"emergencyResult={NormalizePromptValue(pool.ArtifactContract.EmergencyAcceptableResult)}");
        builder.AppendLine(
            $"Execution bundle: selectedRoute={pool.ExecutionBundle.SelectedRouteLevel}; "
            + $"preferredStartable={pool.ExecutionBundle.PreferredRoute.IsStartable}; "
            + $"degradedStartable={pool.ExecutionBundle.DegradedRoute.IsStartable}; "
            + $"emergencyStartable={pool.ExecutionBundle.EmergencyRoute.IsStartable}; "
            + $"downloads={pool.ExecutionBundle.AcquisitionPlan.Items.Count}");
        builder.AppendLine("Program-owned required outcome actions (you may add detail but must not remove or weaken them):");
        foreach (var action in pool.OutcomeContract.Actions)
        {
            builder.AppendLine(
                $"- id={action.Id}; kind={action.Kind}; required={action.Required.ToString().ToLowerInvariant()}; "
                + $"capabilities={string.Join(',', action.CapabilityIds)}; purpose={NormalizePromptValue(action.Purpose)}");
        }
        AppendExecutionRoute(builder, pool.ExecutionRoute);
        AppendComponentPassports(builder, pool.AvailableComponentIds);
        builder.AppendLine("Installed runnable candidates:");
        foreach (var candidate in pool.InstalledCandidates)
        {
            AppendCandidate(builder, candidate);
        }

        builder.AppendLine("Downloadable coordinator alternatives:");
        foreach (var candidate in pool.AlternativeCandidates)
        {
            AppendCandidate(builder, candidate);
        }

        builder.AppendLine("Return final_task_card with executorSelection and executionPlan:");
        builder.AppendLine("- installedCandidateId: one installed_* ID, or an empty string when no installed comparison is useful;");
        builder.AppendLine("- alternativeCandidateId: one alternative_* ID, or an empty string when no downloadable comparison is materially better;");
        builder.AppendLine("- preferredCandidateId: exactly one non-empty selected candidate ID;");
        builder.AppendLine("- executionPlan.requiredCapabilities: every capability needed to finish the expected task, including file decode, semantic specialist work, transformations and output;");
        builder.AppendLine("- executionPlan.optionalCapabilities: useful reserve capabilities that improve quality but are not required;");
        builder.AppendLine("- executionPlan.preferredComponentIds: component IDs from the verified passports that you intentionally want used;");
        builder.AppendLine("- executionPlan.executorRole: a short adaptive professional role for the coordinator;");
        builder.AppendLine("- executionPlan.rationale: why this complete combination is appropriate, including honest missing parts;");
        builder.AppendLine("- if any required capability has no trusted working adapter, leave alternativeCandidateId empty; do not advertise a downloadable coordinator as the solution;");
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
        ComputerPassport computerPassport,
        ChoiceCapabilityProfile capabilityProfile,
        WorkPatternSelectionResult? workPatterns)
    {
        var selectedPatterns = new WorkPatternCatalogService().ResolveSelected(
            workPatterns ?? new WorkPatternSelectionResult
            {
                Selections =
                [
                    new WorkPatternSelection
                    {
                        PatternId = "other.custom",
                        MatchPercent = 100,
                        Reason = "Live-search compatibility fallback."
                    }
                ]
            });
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
                ApplyConditionalMatch(
                    liveCandidate,
                    selectedPatterns,
                    capabilityProfile,
                    installed: false);
                liveCandidate.RouteCoveragePercent = pool.ExecutionRoute.OutcomeCoveragePercent;
                pool.AlternativeCandidates.Add(liveCandidate);
            }
        }

        pool.AlternativeCandidates = pool.AlternativeCandidates
            .GroupBy(candidate => $"{candidate.Family}|{candidate.Model}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(candidate => candidate.ConditionalMatchPercent)
            .ThenBy(candidate => candidate.Model, StringComparer.OrdinalIgnoreCase)
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

    private static ExecutionCapabilityResolution CreateCapabilityResolution(
        ExecutionRoutePlan executionRoute)
    {
        var bindings = executionRoute.Resolution.Bindings;
        return new ExecutionCapabilityResolution
        {
            Required = executionRoute.Requirements
                .Where(requirement => requirement.Request.Required)
                .Select(requirement => requirement.Request.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Available = bindings
                .Where(binding => binding.IsExecutable)
                .Select(binding => binding.RequestedCapabilityId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Missing = bindings
                .Where(binding =>
                    binding.Required
                    && binding.Status == CapabilityBindingStatuses.PackageMissing
                    && binding.AdapterAvailable)
                .Select(binding => binding.RequestedCapabilityId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Unresolved = bindings
                .Where(binding =>
                    binding.Required
                    && binding.Status is CapabilityBindingStatuses.AdapterMissing
                        or CapabilityBindingStatuses.UnknownCapability)
                .Select(binding => binding.RequestedCapabilityId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
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
            SemanticDescriptionRu = candidate.SemanticDescriptionRu,
            SemanticDescriptionEn = candidate.SemanticDescriptionEn,
            IsRecommended = preferred,
            RuntimeBackend = candidate.RuntimeBackend,
            ArtifactFormat = candidate.ArtifactFormat,
            CatalogMatchScope = candidate.CatalogMatchScope,
            RequiredCapabilities = candidate.RequiredCapabilities.ToList(),
            AvailableCapabilities = candidate.AvailableCapabilities.ToList(),
            MissingCapabilities = candidate.MissingCapabilities.ToList(),
            UnresolvedCapabilities = candidate.UnresolvedCapabilities.ToList(),
            ConditionalMatchPercent = candidate.ConditionalMatchPercent,
            CoordinatorMatchPercent = candidate.CoordinatorMatchPercent,
            RouteCoveragePercent = candidate.RouteCoveragePercent,
            MatchReason = candidate.MatchReason
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
            + $"coordinatorMatch={candidate.CoordinatorMatchPercent}%; "
            + $"routeCoverage={candidate.RouteCoveragePercent}%; "
            + $"matchReason={NormalizePromptValue(candidate.MatchReason)}; "
            + $"hardware={candidate.HardwareStatus}; evidence={candidate.Evidence}; "
            + $"semanticPassportRu={NormalizePromptValue(candidate.SemanticDescriptionRu)}");
    }

    private static void AppendExecutionRoute(
        StringBuilder builder,
        ExecutionRoutePlan route)
    {
        builder.AppendLine("Verified execution route:");
        builder.AppendLine(
            $"- sourceFormats={string.Join(',', route.SourceFormats)}; executable={route.IsExecutable.ToString().ToLowerInvariant()}; "
            + $"outcomeCoverage={route.OutcomeCoveragePercent}%; missingOutcomeActions={string.Join(',', route.MissingOutcomeActionIds)}");
        foreach (var requirement in route.Requirements)
        {
            var binding = route.Resolution.Bindings.FirstOrDefault(item =>
                string.Equals(
                    item.RequestedCapabilityId,
                    requirement.Request.Id,
                    StringComparison.OrdinalIgnoreCase));
            builder.AppendLine(
                $"- layer={requirement.Layer}; capability={requirement.Request.Id}; "
                + $"status={binding?.Status ?? CapabilityBindingStatuses.UnknownCapability}; "
                + $"component={binding?.ComponentId ?? string.Empty}; "
                + $"adapter={binding?.AdapterId ?? string.Empty}; "
                + $"purpose={NormalizePromptValue(requirement.Request.Purpose)}");
        }
    }

    private static void AppendComponentPassports(
        StringBuilder builder,
        IEnumerable<string> componentIds)
    {
        var providers = componentIds
            .Select(ComponentCatalog.Find)
            .Where(entry => entry is { IsVisibleToAi: true })
            .Cast<ComponentCatalogEntry>()
            .DistinctBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (providers.Count == 0)
        {
            return;
        }

        var statuses = new ComponentManager()
            .GetStatus(ComponentKinds.Processing)
            .ToDictionary(status => status.Entry.Id, StringComparer.OrdinalIgnoreCase);
        builder.AppendLine("Verified component passports:");
        foreach (var provider in providers)
        {
            statuses.TryGetValue(provider.Id, out var status);
            var adapters = provider.Capabilities
                .Select(ComponentAdapterRegistry.Find)
                .Where(adapter => adapter is not null)
                .DistinctBy(adapter => adapter!.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
            builder.AppendLine(
                $"- id={provider.Id}; capabilities={string.Join(',', provider.Capabilities)}; "
                + $"available={(status?.IsAvailable == true).ToString().ToLowerInvariant()}; "
                + $"planned={provider.IsPlanned.ToString().ToLowerInvariant()}; "
                + $"adapterReady={(adapters.Count > 0).ToString().ToLowerInvariant()}; "
                + $"tools={string.Join(',', adapters.SelectMany(adapter => adapter!.ToolNames).Distinct(StringComparer.Ordinal))}; "
                + $"usage={NormalizePromptValue(string.Join(' ', adapters.Select(adapter => adapter!.UsageSummary)))}; "
                + $"downloadBytes={provider.DownloadSizeBytes}; license={provider.License}; source={provider.Source}; "
                + $"description={NormalizePromptValue(ComponentSemanticPassportCatalog.Get(provider).Ru)}");
        }
    }

    private static bool TryValidateExecutionPlan(
        ChoiceExecutionPlan plan,
        ChoiceExecutorCandidatePool pool,
        out string error)
    {
        error = string.Empty;
        if (plan is null)
        {
            error = "The core must provide executionPlan.";
            return false;
        }

        plan.RequiredCapabilities = NormalizeCapabilities(plan.RequiredCapabilities);
        plan.OptionalCapabilities = NormalizeCapabilities(plan.OptionalCapabilities)
            .Except(plan.RequiredCapabilities, StringComparer.OrdinalIgnoreCase)
            .ToList();
        plan.PreferredComponentIds = plan.PreferredComponentIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .ToList();
        plan.ExecutorRole = NormalizePromptValue(plan.ExecutorRole);
        plan.Rationale = NormalizePromptValue(plan.Rationale);

        if (plan.RequiredCapabilities.Count == 0)
        {
            error = "executionPlan.requiredCapabilities must describe at least one capability needed for the task.";
            return false;
        }

        if (plan.ExecutorRole.Length == 0 || plan.Rationale.Length == 0)
        {
            error = "executionPlan must include a short executorRole and rationale.";
            return false;
        }

        var knownIds = pool.AvailableComponentIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownIds = plan.PreferredComponentIds
            .Where(id => !knownIds.Contains(id))
            .ToList();
        if (unknownIds.Count > 0)
        {
            error = $"executionPlan contains component IDs outside the trusted catalog: {string.Join(", ", unknownIds)}.";
            return false;
        }

        var requested = ComponentCapabilityAliasCatalog.Expand(
                plan.RequiredCapabilities.Concat(plan.OptionalCapabilities))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var componentId in plan.PreferredComponentIds)
        {
            var component = ComponentCatalog.Find(componentId);
            if (component is null
                || !component.Capabilities.Any(capability => requested.Contains(capability)))
            {
                error = $"Preferred component '{componentId}' does not provide a requested execution capability.";
                return false;
            }
        }

        return true;
    }

    private static List<string> NormalizeCapabilities(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(ComponentCapabilityAliasCatalog.Canonicalize)
            .Where(value => Regex.IsMatch(value, @"^[a-z0-9][a-z0-9._-]{1,79}$"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(24)
            .ToList();

    private static void ApplyResolvedRoute(
        ChoiceExecutorCandidate candidate,
        ExecutionRoutePlan route)
    {
        candidate.RequiredCapabilities = route.Requirements
            .Where(requirement => requirement.Request.Required)
            .Select(requirement => requirement.Request.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        candidate.AvailableCapabilities = route.Resolution.Bindings
            .Where(binding => binding.IsExecutable)
            .Select(binding => binding.RequestedCapabilityId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        candidate.MissingCapabilities = route.Resolution.Bindings
            .Where(binding =>
                binding.Required
                && binding.Status == CapabilityBindingStatuses.PackageMissing)
            .Select(binding => binding.RequestedCapabilityId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        candidate.UnresolvedCapabilities = route.Resolution.Bindings
            .Where(binding => binding.Required && !binding.IsExecutable)
            .Select(binding => binding.RequestedCapabilityId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        candidate.RouteCoveragePercent = route.OutcomeCoveragePercent;
        candidate.Limitation = candidate.UnresolvedCapabilities.Count == 0
            ? candidate.Limitation
            : $"The complete bundle still requires: {string.Join(", ", candidate.UnresolvedCapabilities)}.";
    }

    private static string NormalizePromptValue(string value) =>
        Regex.Replace(value?.Trim() ?? string.Empty, @"\s+", " ");

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

    private static void ApplyConditionalMatch(
        ChoiceExecutorPoolCandidate candidate,
        IReadOnlyList<SandboxWorkPattern> selectedPatterns,
        ChoiceCapabilityProfile capabilityProfile,
        bool installed)
    {
        var assessment = new CoordinatorMatchScoringService().Score(
            candidate,
            selectedPatterns,
            capabilityProfile,
            installed);
        candidate.CoordinatorMatchPercent = assessment.Percent;
        candidate.ConditionalMatchPercent = assessment.Percent;
        candidate.MatchReason = assessment.Reason;
    }

    private static string BuildOutcomeGoal(
        IReadOnlyList<SandboxWorkPattern> patterns,
        ChoiceCapabilityProfile profile)
    {
        var patternGoals = patterns
            .Select(pattern => pattern.DescriptionRu)
            .Where(value => !string.IsNullOrWhiteSpace(value));
        var profileValues = profile.Dimensions
            .Where(dimension => dimension.Status is ChoiceDimensionStatuses.Resolved
                or ChoiceDimensionStatuses.Provisional)
            .SelectMany(dimension => dimension.Values)
            .Where(value => !string.IsNullOrWhiteSpace(value));
        return string.Join(" ", patternGoals.Concat(profileValues).Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static ExecutionOutcomeContract CloneOutcomeContract(
        ExecutionOutcomeContract source,
        string goal)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        var clone = JsonSerializer.Deserialize<ExecutionOutcomeContract>(json, JsonOptions)
            ?? new ExecutionOutcomeContract();
        clone.Goal = string.IsNullOrWhiteSpace(goal) ? source.Goal : goal.Trim();
        return clone;
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
