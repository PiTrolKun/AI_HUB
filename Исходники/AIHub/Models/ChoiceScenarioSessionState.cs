namespace AIHub.Models;

public sealed class ChoiceScenarioSessionState
{
    private readonly List<ChoiceScenarioStep> _steps = [];
    private readonly List<bool> _stepConsumesAnswer = [];
    private readonly List<ChoiceScenarioAnswer> _answers = [];
    private readonly List<ChoiceCapabilityProfile> _profileSnapshots = [];
    private readonly Dictionary<string, int> _stepFingerprints = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _semanticPatterns = new(StringComparer.Ordinal);

    public IReadOnlyList<ChoiceScenarioStep> Steps => _steps;

    public IReadOnlyList<ChoiceScenarioAnswer> Answers => _answers;

    public ChoiceScenarioStep? CurrentStep => _steps.LastOrDefault();

    public ChoiceScenarioStepBudget? StepBudget { get; private set; }

    public ChoiceCapabilityProfile CapabilityProfile { get; } = new();

    public int SubstantiveStepsUsed => _answers.Count;

    public int StepsRemaining => StepBudget is null
        ? 0
        : Math.Max(0, StepBudget.MaximumSteps - SubstantiveStepsUsed);

    public bool IsStepBudgetExhausted => StepBudget is not null && StepsRemaining == 0;

    public ChoiceScenarioStateCheckpoint CreateCheckpoint() => new()
    {
        Steps = CloneList(_steps),
        StepConsumesAnswer = [.. _stepConsumesAnswer],
        Answers = CloneList(_answers),
        ProfileSnapshots = CloneList(_profileSnapshots),
        StepBudget = CloneObject(StepBudget),
        CapabilityProfile = CapabilityProfile.Clone()
    };

    public void Restore(ChoiceScenarioStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (checkpoint.Steps.Count == 0
            || checkpoint.StepConsumesAnswer.Count != checkpoint.Steps.Count
            || checkpoint.ProfileSnapshots.Count != checkpoint.Steps.Count)
        {
            throw new InvalidOperationException("The saved choice scenario checkpoint is incomplete.");
        }

        _steps.Clear();
        _steps.AddRange(CloneList(checkpoint.Steps));
        _stepConsumesAnswer.Clear();
        _stepConsumesAnswer.AddRange(checkpoint.StepConsumesAnswer);
        _answers.Clear();
        _answers.AddRange(CloneList(checkpoint.Answers));
        _profileSnapshots.Clear();
        _profileSnapshots.AddRange(CloneList(checkpoint.ProfileSnapshots));
        _stepFingerprints.Clear();
        _semanticPatterns.Clear();
        StepBudget = CloneObject(checkpoint.StepBudget);
        CapabilityProfile.ReplaceWith(checkpoint.CapabilityProfile);
        foreach (var step in _steps)
        {
            var fingerprint = CreateFingerprint(step);
            _stepFingerprints.TryGetValue(fingerprint, out var fingerprintCount);
            _stepFingerprints[fingerprint] = fingerprintCount + 1;
            var semanticPattern = CreateSemanticPattern(step.Question);
            _semanticPatterns.TryGetValue(semanticPattern, out var semanticCount);
            _semanticPatterns[semanticPattern] = semanticCount + 1;
        }
    }

    public void Reset(ChoiceScenarioStep startStep)
    {
        _steps.Clear();
        _stepConsumesAnswer.Clear();
        _answers.Clear();
        _profileSnapshots.Clear();
        _stepFingerprints.Clear();
        _semanticPatterns.Clear();
        StepBudget = null;
        CapabilityProfile.ReplaceWith(new ChoiceCapabilityProfile());
        AddStep(startStep, consumedAnswer: false);
    }

    public void ConfigureStepBudget(ChoiceScenarioStepBudget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        if (budget.MaximumSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(budget));
        }

        StepBudget = budget;
    }

    public void ClearStepBudget()
    {
        StepBudget = null;
    }

    public void ApplyTrustedProfileUpdate(IEnumerable<ChoiceCapabilityDimension> updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        CapabilityProfile.Apply(updates);
        if (_profileSnapshots.Count > 0)
        {
            _profileSnapshots[^1] = CapabilityProfile.Clone();
        }
    }

    public bool TryAddAnswer(ChoiceScenarioOption option)
    {
        if (CurrentStep is null)
        {
            return false;
        }

        var appliedEffects = option.ProfileEffects.Select(effect => new ChoiceCapabilityDimension
        {
            Dimension = effect.Dimension,
            Status = effect.Status,
            Values = effect.Values.ToList(),
            Evidence = effect.Evidence
        }).ToList();
        if (string.Equals(option.Id, "custom", StringComparison.OrdinalIgnoreCase)
            && appliedEffects.Count == 0
            && !string.IsNullOrWhiteSpace(CurrentStep.DecisionDimension)
            && ChoiceDecisionDimensions.All.Contains(
                CurrentStep.DecisionDimension,
                StringComparer.OrdinalIgnoreCase))
        {
            appliedEffects.Add(new ChoiceCapabilityDimension
            {
                Dimension = CurrentStep.DecisionDimension,
                Status = ChoiceDimensionStatuses.Resolved,
                Values = [option.Title.Trim()],
                Evidence = "user custom input"
            });
        }

        _answers.Add(new ChoiceScenarioAnswer
        {
            StepNumber = _answers.Count + 1,
            Question = CurrentStep.Question,
            OptionId = option.Id,
            OptionTitle = option.Title,
            IsCustom = string.Equals(option.Id, "custom", StringComparison.OrdinalIgnoreCase),
            DecisionDimension = CurrentStep.DecisionDimension,
            SelectionImpact = CurrentStep.SelectionImpact.ToList(),
            AppliedProfileEffects = appliedEffects
        });
        CapabilityProfile.Apply(appliedEffects);
        return true;
    }

    public int AddStep(ChoiceScenarioStep step, bool consumedAnswer = true)
    {
        CapabilityProfile.Apply(step.ProfileUpdate);
        _steps.Add(step);
        _stepConsumesAnswer.Add(consumedAnswer);
        _profileSnapshots.Add(CapabilityProfile.Clone());
        var fingerprint = CreateFingerprint(step);
        _stepFingerprints.TryGetValue(fingerprint, out var count);
        count++;
        _stepFingerprints[fingerprint] = count;
        var semanticPattern = CreateSemanticPattern(step.Question);
        _semanticPatterns.TryGetValue(semanticPattern, out var semanticCount);
        _semanticPatterns[semanticPattern] = semanticCount + 1;
        return count;
    }

    public int GetFingerprintCount(ChoiceScenarioStep step)
    {
        _stepFingerprints.TryGetValue(CreateFingerprint(step), out var count);
        return count;
    }

    public bool IsSemanticLoop(ChoiceScenarioStep step, int allowedOccurrences = 3)
    {
        _semanticPatterns.TryGetValue(CreateSemanticPattern(step.Question), out var count);
        return count >= allowedOccurrences;
    }

    public bool IsSubjectMatterOverreach(ChoiceScenarioStep step)
    {
        if (!string.Equals(step.DecisionDimension, ChoiceDecisionDimensions.DomainSpecialization, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return _steps
            .AsEnumerable()
            .Reverse()
            .TakeWhile(previous => string.Equals(previous.StepType, "question_step", StringComparison.Ordinal))
            .Any(previous => string.Equals(
                previous.DecisionDimension,
                ChoiceDecisionDimensions.DomainSpecialization,
                StringComparison.OrdinalIgnoreCase));
    }

    public bool TryGoBack(out ChoiceScenarioStep? step)
    {
        step = null;
        if (_steps.Count <= 1)
        {
            return false;
        }

        var removedStep = _steps[^1];
        var consumedAnswer = _stepConsumesAnswer[^1];
        _steps.RemoveAt(_steps.Count - 1);
        _stepConsumesAnswer.RemoveAt(_stepConsumesAnswer.Count - 1);
        _profileSnapshots.RemoveAt(_profileSnapshots.Count - 1);
        var fingerprint = CreateFingerprint(removedStep);
        if (_stepFingerprints.TryGetValue(fingerprint, out var count))
        {
            if (count <= 1)
            {
                _stepFingerprints.Remove(fingerprint);
            }
            else
            {
                _stepFingerprints[fingerprint] = count - 1;
            }
        }

        var semanticPattern = CreateSemanticPattern(removedStep.Question);
        if (_semanticPatterns.TryGetValue(semanticPattern, out var semanticCount))
        {
            if (semanticCount <= 1)
            {
                _semanticPatterns.Remove(semanticPattern);
            }
            else
            {
                _semanticPatterns[semanticPattern] = semanticCount - 1;
            }
        }

        if (consumedAnswer && _answers.Count > 0)
        {
            _answers.RemoveAt(_answers.Count - 1);
        }


        CapabilityProfile.ReplaceWith(_profileSnapshots[^1]);

        step = _steps[^1];
        return true;
    }

    public void RemoveLastAnswer()
    {
        if (_answers.Count > 0)
        {
            _answers.RemoveAt(_answers.Count - 1);
        }


        if (_profileSnapshots.Count > 0)
        {
            CapabilityProfile.ReplaceWith(_profileSnapshots[^1]);
        }
    }

    public static string CreateFingerprint(ChoiceScenarioStep step)
    {
        var question = Normalize(step.Question);
        var options = string.Join('|', step.Options.Select(option => Normalize(option.Title)).Order());
        return $"{question}::{options}";
    }

    private static string Normalize(string value) =>
        string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string CreateSemanticPattern(string question)
    {
        var normalizedWords = question
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray()))
            .Where(word => word.Length > 0)
            .Take(3);
        return string.Join(' ', normalizedWords);
    }

    private static List<T> CloneList<T>(IEnumerable<T> values)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(values);
        return System.Text.Json.JsonSerializer.Deserialize<List<T>>(json) ?? [];
    }

    private static T? CloneObject<T>(T? value)
        where T : class
    {
        if (value is null)
        {
            return default;
        }

        var json = System.Text.Json.JsonSerializer.Serialize(value);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json);
    }
}
