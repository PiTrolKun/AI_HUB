using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AIHub.Models;

namespace AIHub.Services;

public sealed class CoreContextMemoryService
{
    private const int KeepRecentMessages = 6;
    private const int MaxSummaryLines = 18;
    private const double CompressionThreshold = 0.86;
    private const int ModelTranscriptBudget = 70000;
    private const int ModelChoiceBudget = 12000;
    private const int ModelReferenceLimit = 24;

    private static readonly Regex UrlRegex = new(@"https?://[^\s`""'<>]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex WindowsPathRegex = new(@"[A-Za-z]:\\[^\r\n<>|""*?]+", RegexOptions.Compiled);

    private string _memorySummary = string.Empty;
    private string _lastSummaryPath = string.Empty;

    public CoreMemoryStatus CreateStatus(
        IReadOnlyList<DebugChatMessage> history,
        string pendingPrompt,
        bool isActive,
        bool isCompressing = false)
    {
        return new CoreMemoryStatus
        {
            IsActive = isActive,
            IsCompressing = isCompressing,
            HasCompressedSummary = !string.IsNullOrWhiteSpace(_memorySummary),
            UsedUnits = EstimateConversationUnits(history, pendingPrompt),
            LimitUnits = CoreContextRuntimeLimits.Qwen3EightBNativeContextLimit
        };
    }

    public CoreMemoryCompressionResult CompressIfNeeded(
        List<DebugChatMessage> history,
        string pendingPrompt,
        string sessionLogPath)
    {
        var status = CreateStatus(history, pendingPrompt, isActive: true);
        if (status.FillPercent / 100d < CompressionThreshold || history.Count <= KeepRecentMessages)
        {
            return CoreMemoryCompressionResult.NotCompressed;
        }

        var oldMessages = history.Take(history.Count - KeepRecentMessages).ToList();
        var recentMessages = history.Skip(history.Count - KeepRecentMessages).ToList();
        _memorySummary = BuildSummary(oldMessages);
        _lastSummaryPath = SaveSummary(sessionLogPath, _memorySummary);

        history.Clear();
        history.Add(new DebugChatMessage
        {
            Role = "AI HUB memory",
            Text = _memorySummary
        });
        history.AddRange(recentMessages);

        return new CoreMemoryCompressionResult(true, _lastSummaryPath);
    }

    public CoreMemoryCompressionPlan? CreateModelCompressionPlan(
        IReadOnlyList<DebugChatMessage> history,
        string pendingPrompt)
    {
        var status = CreateStatus(history, pendingPrompt, isActive: true);
        if (status.FillPercent / 100d < CompressionThreshold || history.Count <= KeepRecentMessages)
        {
            return null;
        }

        var compressedMessageCount = history.Count - KeepRecentMessages;
        var oldMessages = history.Take(compressedMessageCount).ToList();
        var prompt = BuildModelCompressionPrompt(oldMessages);
        return new CoreMemoryCompressionPlan(history.Count, compressedMessageCount, prompt);
    }

    public CoreMemoryCompressionResult ApplyModelCompression(
        List<DebugChatMessage> history,
        CoreMemoryCompressionPlan plan,
        string modelSummary,
        string sessionLogPath)
    {
        if (history.Count < plan.OriginalMessageCount
            || string.IsNullOrWhiteSpace(modelSummary))
        {
            return CoreMemoryCompressionResult.NotCompressed;
        }

        var recentMessages = history.Skip(plan.CompressedMessageCount).ToList();
        _memorySummary = BuildModelMemory(modelSummary, plan.CompressedMessageCount);
        _lastSummaryPath = SaveSummary(sessionLogPath, _memorySummary);

        history.Clear();
        history.Add(new DebugChatMessage
        {
            Role = "AI HUB memory",
            Text = _memorySummary
        });
        history.AddRange(recentMessages);

        return new CoreMemoryCompressionResult(true, _lastSummaryPath, "model");
    }

    public void Reset()
    {
        _memorySummary = string.Empty;
        _lastSummaryPath = string.Empty;
    }

    private int EstimateConversationUnits(IReadOnlyList<DebugChatMessage> history, string pendingPrompt)
    {
        var used = EstimateUnits(_memorySummary) + EstimateUnits(pendingPrompt);
        foreach (var message in history)
        {
            used += EstimateUnits(message.Role) + EstimateUnits(message.Text) + 4;
        }

        return Math.Max(0, used);
    }

    private static int EstimateUnits(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return Math.Max(1, (int)Math.Ceiling(text.Length / 4d));
    }

    private string BuildSummary(IReadOnlyList<DebugChatMessage> messages)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Служебная память AI HUB. Это не сообщение пользователя.");
        builder.AppendLine($"Сжато: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"Сообщений сжато: {messages.Count}.");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(_memorySummary))
        {
            builder.AppendLine("Предыдущая память:");
            builder.AppendLine(Clip(_memorySummary, 1200));
            builder.AppendLine();
        }

        builder.AppendLine("Важные фрагменты прежнего диалога:");
        foreach (var message in messages.TakeLast(MaxSummaryLines))
        {
            builder.AppendLine($"- {message.Role}: {Clip(message.Text, 260)}");
        }

        var references = ExtractReferences(messages);
        if (references.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Ссылки и пути, найденные в прежнем диалоге:");
            foreach (var reference in references.Take(12))
            {
                builder.AppendLine($"- {reference}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("При продолжении диалога используй эту память как краткий конспект, но не выдавай её за новое сообщение пользователя.");
        return builder.ToString().Trim();
    }

    private string BuildModelCompressionPrompt(IReadOnlyList<DebugChatMessage> messages)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Служебная задача AI HUB: обнови память текущей сессии.");
        builder.AppendLine("Это не пользовательский запрос и не ответ пользователю.");
        builder.AppendLine();
        builder.AppendLine("Нужно сохранить смысл, а не просто сократить текст.");
        builder.AppendLine("Обязательно сохрани решения и выборы пользователя, цель текущей задачи, что уже сделано, что запланировано, открытые вопросы, важные пути, ссылки, модели, инструменты, запреты и предпочтения.");
        builder.AppendLine("Не добавляй догадок. Если данных по разделу нет, напиши `нет данных`.");
        builder.AppendLine("Не пересказывай художественные или длинные материалы полностью: сохрани их смысловые опоры и укажи, что полный источник остаётся в JSONL-логе сессии.");
        builder.AppendLine();
        builder.AppendLine("Верни только служебную память в таком формате:");
        builder.AppendLine("# AI HUB session memory");
        builder.AppendLine("## User choices");
        builder.AppendLine("## Current task");
        builder.AppendLine("## Done");
        builder.AppendLine("## Planned next");
        builder.AppendLine("## Open questions");
        builder.AppendLine("## Important references");
        builder.AppendLine("## Constraints and preferences");
        builder.AppendLine("## Recovery notes");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(_memorySummary))
        {
            builder.AppendLine("Предыдущая служебная память:");
            builder.AppendLine(Clip(_memorySummary, 5000));
            builder.AppendLine();
        }

        var choices = ExtractUserChoiceCandidates(messages);
        if (choices.Count > 0)
        {
            builder.AppendLine("Кандидаты на важные решения пользователя:");
            builder.AppendLine(Clip(string.Join(Environment.NewLine, choices), ModelChoiceBudget));
            builder.AppendLine();
        }

        var references = ExtractReferences(messages);
        if (references.Count > 0)
        {
            builder.AppendLine("Ссылки и пути из старой истории:");
            foreach (var reference in references.Take(ModelReferenceLimit))
            {
                builder.AppendLine("- " + reference);
            }

            builder.AppendLine();
        }

        builder.AppendLine("Старая часть диалога для сжатия:");
        builder.AppendLine(BuildTranscriptForModel(messages));
        return builder.ToString().Trim();
    }

    private static string BuildTranscriptForModel(IReadOnlyList<DebugChatMessage> messages)
    {
        var selected = new List<DebugChatMessage>();
        selected.AddRange(messages.Take(8));
        selected.AddRange(messages.Skip(Math.Max(0, messages.Count - 60)));

        var unique = selected
            .Select((message, index) => new { message, index })
            .GroupBy(item => ReferenceEquals(item.message, null) ? item.index.ToString(System.Globalization.CultureInfo.InvariantCulture) : item.message.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture))
            .Select(group => group.First().message)
            .ToList();

        var builder = new StringBuilder();
        foreach (var message in unique)
        {
            if (builder.Length >= ModelTranscriptBudget)
            {
                builder.AppendLine("[transcript clipped]");
                break;
            }

            builder.AppendLine($"{message.Role}: {Clip(message.Text, 1200)}");
        }

        return builder.ToString();
    }

    private string BuildModelMemory(string modelSummary, int compressedMessageCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Служебная память AI HUB. Это не сообщение пользователя.");
        builder.AppendLine("Сжато моделью ядра.");
        builder.AppendLine($"Сжато: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"Сообщений сжато: {compressedMessageCount}.");
        builder.AppendLine();
        builder.AppendLine(modelSummary.Trim());
        builder.AppendLine();
        builder.AppendLine("Полный JSONL-лог сессии остаётся источником истины. Если нужно восстановить забытый фрагмент, используй инструмент session_log.");
        return builder.ToString().Trim();
    }

    private static List<string> ExtractUserChoiceCandidates(IEnumerable<DebugChatMessage> messages)
    {
        string[] markers =
        [
            "согласен", "выбира", "нужно", "нельзя", "обязательно", "запомни",
            "пока", "не надо", "не нужно", "без публикации", "не публикуй",
            "давай", "оставь", "убери", "добавь", "правило", "тз"
        ];

        return messages
            .Where(message => message.Role.Contains("user", StringComparison.OrdinalIgnoreCase)
                || message.Role.Contains("польз", StringComparison.OrdinalIgnoreCase))
            .Where(message => markers.Any(marker => message.Text.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            .Select(message => "- " + Clip(message.Text, 500))
            .TakeLast(28)
            .ToList();
    }

    private static List<string> ExtractReferences(IEnumerable<DebugChatMessage> messages)
    {
        var references = new List<string>();
        foreach (var message in messages)
        {
            foreach (Match match in UrlRegex.Matches(message.Text))
            {
                AddUnique(references, match.Value.TrimEnd('.', ',', ';', ')', ']'));
            }

            foreach (Match match in WindowsPathRegex.Matches(message.Text))
            {
                AddUnique(references, match.Value.Trim());
            }
        }

        return references;
    }

    private static void AddUnique(List<string> values, string value)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !values.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
        {
            values.Add(value);
        }
    }

    private static string SaveSummary(string sessionLogPath, string summary)
    {
        try
        {
            var sessionDirectory = Path.GetDirectoryName(sessionLogPath);
            if (string.IsNullOrWhiteSpace(sessionDirectory))
            {
                return string.Empty;
            }

            var memoryDirectory = Path.Combine(sessionDirectory, "Memory");
            Directory.CreateDirectory(memoryDirectory);
            var fileName = Path.GetFileNameWithoutExtension(sessionLogPath) + "_core-memory.md";
            var path = Path.Combine(memoryDirectory, fileName);
            File.WriteAllText(path, summary, Encoding.UTF8);
            return path;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Clip(string text, int maxLength)
    {
        var normalized = text.Replace("\r\n", " ").Replace('\n', ' ').Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength].TrimEnd() + "...";
    }
}

public sealed record CoreMemoryCompressionResult(bool WasCompressed, string SummaryPath)
{
    public static CoreMemoryCompressionResult NotCompressed { get; } = new(false, string.Empty);

    public CoreMemoryCompressionResult(bool wasCompressed, string summaryPath, string mode)
        : this(wasCompressed, summaryPath)
    {
        Mode = mode;
    }

    public string Mode { get; init; } = "mechanical";
}

public sealed record CoreMemoryCompressionPlan(
    int OriginalMessageCount,
    int CompressedMessageCount,
    string ModelPrompt);
