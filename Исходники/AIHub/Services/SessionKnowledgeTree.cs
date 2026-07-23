using System.Text.Json;
using System.Text.Encodings.Web;
using AIHub.Models;

namespace AIHub.Services;

public sealed class SessionKnowledgeTree
{
    private static readonly JsonSerializerOptions ContextJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly object _sync = new();
    private readonly List<SessionKnowledgeNode> _nodes = [];
    private string _languageCode = "ru";
    private string _rootId = string.Empty;
    private string _activeNodeId = string.Empty;
    private string _requirementsRootId = string.Empty;
    private string _decisionsRootId = string.Empty;
    private string _knowledgeRootId = string.Empty;
    private string _resultRootId = string.Empty;
    private string _questionsRootId = string.Empty;
    private string _assumptionsRootId = string.Empty;
    private string _sourcesRootId = string.Empty;
    private string _activeConversationParentId = string.Empty;
    private string? _pendingQuestionId;
    private List<string> _pendingOptions = [];
    private int _sequence;
    private int _version;

    public event EventHandler<SessionKnowledgeTreeChangedEventArgs>? Changed;

    public bool IsInitialized
    {
        get
        {
            lock (_sync)
            {
                return !string.IsNullOrWhiteSpace(_rootId);
            }
        }
    }

    public void Initialize(ExecutorHandoffPackage handoff)
    {
        SessionKnowledgeTreeSnapshot snapshot;
        lock (_sync)
        {
            _nodes.Clear();
            _sequence = 0;
            _version = 0;
            _languageCode = handoff.LanguageCode;
            _rootId = AddNodeCore(
                null,
                SessionKnowledgeNodeTypes.Task,
                Text("Задача сессии", "Session task"),
                BuildRootContent(handoff),
                ExecutorStageIds.TaskDefinition,
                isStructural: true).Id;
            _activeNodeId = _rootId;
            _requirementsRootId = AddCategory(
                SessionKnowledgeNodeTypes.Requirement,
                Text("Требования", "Requirements"));
            _decisionsRootId = AddCategory(
                SessionKnowledgeNodeTypes.Decision,
                Text("Решения", "Decisions"));
            _knowledgeRootId = AddCategory(
                SessionKnowledgeNodeTypes.Knowledge,
                Text("Знания", "Knowledge"));
            _resultRootId = AddCategory(
                SessionKnowledgeNodeTypes.ResultFragment,
                Text("Рабочий результат", "Working result"));
            _questionsRootId = AddCategory(
                SessionKnowledgeNodeTypes.OpenQuestion,
                Text("Ход уточнений", "Clarification path"));
            _assumptionsRootId = AddCategory(
                SessionKnowledgeNodeTypes.Assumption,
                Text("Предположения", "Assumptions"));
            _sourcesRootId = AddCategory(
                SessionKnowledgeNodeTypes.Source,
                Text("Источники", "Sources"));
            _activeConversationParentId = _questionsRootId;
            _pendingQuestionId = null;
            _pendingOptions = [];

            AddInitialHandoff(handoff);
            _version++;
            snapshot = CreateSnapshotCore();
        }

        RaiseChanged("initialized", _rootId, snapshot);
    }

    public void RecordAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return;
        }

        SessionKnowledgeTreeSnapshot? snapshot = null;
        string changedNodeId = string.Empty;
        lock (_sync)
        {
            if (_pendingQuestionId is null)
            {
                changedNodeId = AddNodeCore(
                    _activeConversationParentId,
                    SessionKnowledgeNodeTypes.Decision,
                    Shorten(answer, 90),
                    answer.Trim(),
                    string.Empty,
                    isActive: true,
                    isResolved: true).Id;
                _activeConversationParentId = changedNodeId;
            }
            else
            {
                var question = FindNode(_pendingQuestionId);
                if (question is not null)
                {
                    question.IsResolved = true;
                }

                SessionKnowledgeNode? selected = null;
                foreach (var option in _pendingOptions)
                {
                    var isSelected = string.Equals(option.Trim(), answer.Trim(), StringComparison.OrdinalIgnoreCase);
                    var branch = AddNodeCore(
                        _pendingQuestionId,
                        SessionKnowledgeNodeTypes.Decision,
                        Shorten(option, 90),
                        option,
                        question?.StageId ?? string.Empty,
                        isActive: isSelected,
                        isResolved: isSelected);
                    if (isSelected)
                    {
                        selected = branch;
                    }
                }

                selected ??= AddNodeCore(
                    _pendingQuestionId,
                    SessionKnowledgeNodeTypes.Decision,
                    Shorten(answer, 90),
                    answer.Trim(),
                    question?.StageId ?? string.Empty,
                    isActive: true,
                    isResolved: true);
                changedNodeId = selected.Id;
                _activeConversationParentId = selected.Id;
            }

            _activeNodeId = changedNodeId;
            _pendingQuestionId = null;
            _pendingOptions = [];
            _version++;
            snapshot = CreateSnapshotCore();
        }

        RaiseChanged("branch_selected", changedNodeId, snapshot);
    }

    public void RecordBriefConfirmation(string checkpoint)
    {
        AddSingleChange(
            "brief_confirmed",
            _decisionsRootId,
            SessionKnowledgeNodeTypes.Decision,
            Text("Постановка подтверждена", "Task brief confirmed"),
            checkpoint,
            ExecutorStageIds.TaskDefinition,
            isResolved: true);
    }

    public void RecordTurn(ExecutorTurnResult turn)
    {
        SessionKnowledgeTreeSnapshot snapshot;
        string changedNodeId;
        lock (_sync)
        {
            EnsureInitialized();
            changedNodeId = _activeNodeId;

            if (!string.IsNullOrWhiteSpace(turn.StageSummary))
            {
                changedNodeId = AddDistinctNode(
                    _requirementsRootId,
                    SessionKnowledgeNodeTypes.Requirement,
                    Text("Контрольная точка", "Stage checkpoint"),
                    turn.StageSummary,
                    turn.StageId).Id;
            }

            if (!string.IsNullOrWhiteSpace(turn.WorkingResultFragment))
            {
                changedNodeId = AddDistinctNode(
                    _resultRootId,
                    SessionKnowledgeNodeTypes.ResultFragment,
                    Text("Фрагмент ответа", "Answer fragment"),
                    turn.WorkingResultFragment,
                    turn.StageId).Id;
            }

            if (!string.IsNullOrWhiteSpace(turn.CurrentResultSummary))
            {
                var resultRoot = FindNode(_resultRootId);
                if (resultRoot is not null)
                {
                    resultRoot.Content = turn.CurrentResultSummary;
                }
            }

            foreach (var assumption in turn.Assumptions)
            {
                changedNodeId = AddDistinctNode(
                    _assumptionsRootId,
                    SessionKnowledgeNodeTypes.Assumption,
                    Text("Предположение", "Assumption"),
                    assumption,
                    turn.StageId).Id;
            }

            foreach (var source in turn.Sources)
            {
                changedNodeId = AddDistinctNode(
                    _sourcesRootId,
                    SessionKnowledgeNodeTypes.Source,
                    Text("Источник", "Source"),
                    source,
                    turn.StageId).Id;
            }

            if (turn.Action == ExecutorTurnActions.AskUser
                && !string.IsNullOrWhiteSpace(turn.Question))
            {
                var question = AddNodeCore(
                    _activeConversationParentId,
                    SessionKnowledgeNodeTypes.OpenQuestion,
                    Text("Вопрос", "Question"),
                    turn.Question,
                    turn.StageId,
                    isActive: true);
                _pendingQuestionId = question.Id;
                _pendingOptions = [.. turn.Options];
                _activeNodeId = question.Id;
                changedNodeId = question.Id;
            }

            _version++;
            snapshot = CreateSnapshotCore();
        }

        RaiseChanged("turn_recorded", changedNodeId, snapshot);
    }

    public void RecordSnapshot(ExecutorResultSnapshot result)
    {
        AddSingleChange(
            "result_snapshot",
            _resultRootId,
            SessionKnowledgeNodeTypes.ResultFragment,
            result.DisplayName,
            Shorten(result.Markdown, 1800),
            result.StageId,
            isResolved: true);
    }

    public SessionKnowledgeTreeSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return CreateSnapshotCore();
        }
    }

    public string BuildModelContext()
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(_rootId))
            {
                return "{}";
            }

            var context = new
            {
                task = FindNode(_rootId)?.Content ?? string.Empty,
                requirements = RecentContents(SessionKnowledgeNodeTypes.Requirement, 8),
                activeDecisions = _nodes
                    .Where(node => node.Type == SessionKnowledgeNodeTypes.Decision
                        && node.IsActive
                        && !node.IsStructural)
                    .OrderBy(node => node.Sequence)
                    .TakeLast(12)
                    .Select(node => node.Content)
                    .ToArray(),
                knowledge = RecentContents(SessionKnowledgeNodeTypes.Knowledge, 8),
                workingResultFragments = RecentContents(SessionKnowledgeNodeTypes.ResultFragment, 6),
                assumptions = RecentContents(SessionKnowledgeNodeTypes.Assumption, 8),
                sources = RecentContents(SessionKnowledgeNodeTypes.Source, 12),
                openQuestion = _pendingQuestionId is null
                    ? string.Empty
                    : FindNode(_pendingQuestionId)?.Content ?? string.Empty
            };
            return JsonSerializer.Serialize(context, ContextJsonOptions);
        }
    }

    private void AddInitialHandoff(ExecutorHandoffPackage handoff)
    {
        foreach (var criterion in handoff.Criteria)
        {
            AddDistinctNode(
                _requirementsRootId,
                SessionKnowledgeNodeTypes.Requirement,
                Text("Критерий", "Criterion"),
                criterion,
                ExecutorStageIds.TaskDefinition);
        }

        foreach (var constraint in handoff.Constraints)
        {
            AddDistinctNode(
                _requirementsRootId,
                SessionKnowledgeNodeTypes.Requirement,
                Text("Ограничение", "Constraint"),
                constraint,
                ExecutorStageIds.TaskDefinition);
        }

        foreach (var item in handoff.ProgramFacts)
        {
            AddDistinctNode(
                _knowledgeRootId,
                SessionKnowledgeNodeTypes.Knowledge,
                item.Name,
                item.Value,
                ExecutorStageIds.TaskDefinition);
        }

        foreach (var item in handoff.UserSignals)
        {
            AddDistinctNode(
                _requirementsRootId,
                SessionKnowledgeNodeTypes.Requirement,
                item.Name,
                item.Value,
                ExecutorStageIds.TaskDefinition);
        }

        foreach (var item in handoff.CoreHypotheses)
        {
            AddDistinctNode(
                _assumptionsRootId,
                SessionKnowledgeNodeTypes.Assumption,
                item.Name,
                item.Value,
                ExecutorStageIds.TaskDefinition);
        }
    }

    private string AddCategory(string type, string title) =>
        AddNodeCore(
            _rootId,
            type,
            title,
            string.Empty,
            string.Empty,
            isStructural: true).Id;

    private void AddSingleChange(
        string changeType,
        string parentId,
        string type,
        string title,
        string content,
        string stageId,
        bool isResolved)
    {
        SessionKnowledgeTreeSnapshot snapshot;
        string nodeId;
        lock (_sync)
        {
            EnsureInitialized();
            var node = AddDistinctNode(parentId, type, title, content, stageId, isResolved);
            nodeId = node.Id;
            _activeNodeId = nodeId;
            _version++;
            snapshot = CreateSnapshotCore();
        }

        RaiseChanged(changeType, nodeId, snapshot);
    }

    private SessionKnowledgeNode AddDistinctNode(
        string parentId,
        string type,
        string title,
        string content,
        string stageId,
        bool isResolved = false)
    {
        var normalized = content.Trim();
        var existing = _nodes.LastOrDefault(node =>
            node.ParentId == parentId
            && node.Type == type
            && string.Equals(node.Content, normalized, StringComparison.OrdinalIgnoreCase));
        return existing ?? AddNodeCore(
            parentId,
            type,
            title,
            normalized,
            stageId,
            isResolved: isResolved);
    }

    private SessionKnowledgeNode AddNodeCore(
        string? parentId,
        string type,
        string title,
        string content,
        string stageId,
        bool isActive = true,
        bool isResolved = false,
        bool isStructural = false)
    {
        var node = new SessionKnowledgeNode
        {
            Id = $"node_{++_sequence}_{Guid.NewGuid():N}",
            ParentId = parentId,
            Type = SessionKnowledgeNodeTypes.IsKnown(type)
                ? type
                : SessionKnowledgeNodeTypes.Knowledge,
            Title = Shorten(title.Trim(), 100),
            Content = content.Trim(),
            StageId = stageId,
            IsActive = isActive,
            IsResolved = isResolved,
            IsStructural = isStructural,
            Sequence = _sequence,
            CreatedAt = DateTimeOffset.Now
        };
        _nodes.Add(node);
        return node;
    }

    private SessionKnowledgeNode? FindNode(string id) =>
        _nodes.FirstOrDefault(node => string.Equals(node.Id, id, StringComparison.Ordinal));

    private string[] RecentContents(string type, int maximum) =>
        _nodes
            .Where(node => node.Type == type
                && !node.IsStructural
                && !string.IsNullOrWhiteSpace(node.Content))
            .OrderBy(node => node.Sequence)
            .TakeLast(maximum)
            .Select(node => Shorten(
                node.Content,
                type == SessionKnowledgeNodeTypes.ResultFragment ? 1400 : 800))
            .ToArray();

    private SessionKnowledgeTreeSnapshot CreateSnapshotCore() =>
        new()
        {
            Version = _version,
            RootId = _rootId,
            ActiveNodeId = _activeNodeId,
            Nodes = _nodes.Select(CloneNode).ToList()
        };

    private static SessionKnowledgeNode CloneNode(SessionKnowledgeNode node) =>
        new()
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Type = node.Type,
            Title = node.Title,
            Content = node.Content,
            StageId = node.StageId,
            IsActive = node.IsActive,
            IsResolved = node.IsResolved,
            IsStructural = node.IsStructural,
            Sequence = node.Sequence,
            CreatedAt = node.CreatedAt
        };

    private string BuildRootContent(ExecutorHandoffPackage handoff)
    {
        var parts = new[] { handoff.SuggestedDirection, handoff.Goal, handoff.Prompt }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return Shorten(string.Join(Environment.NewLine, parts), 1600);
    }

    private void EnsureInitialized()
    {
        if (string.IsNullOrWhiteSpace(_rootId))
        {
            throw new InvalidOperationException("Session knowledge tree has not been initialized.");
        }
    }

    private string Text(string russian, string english) =>
        _languageCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase)
            ? russian
            : english;

    private static string Shorten(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters
            ? value
            : value[..(maximumCharacters - 1)].TrimEnd() + "…";

    private void RaiseChanged(
        string changeType,
        string nodeId,
        SessionKnowledgeTreeSnapshot snapshot) =>
        Changed?.Invoke(
            this,
            new SessionKnowledgeTreeChangedEventArgs
            {
                ChangeType = changeType,
                NodeId = nodeId,
                Snapshot = snapshot
            });
}
