using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public sealed class LimitedResultDocumentBuilder
{
    private readonly SpecialistToolResultNormalizer _normalizer = new();

    public string Build(
        ExecutorHandoffPackage handoff,
        string confirmedBrief,
        ExecutionActionGraph graph,
        IReadOnlyCollection<ExecutionEvidenceReceipt> receipts,
        EvidenceValidationResult validation,
        IReadOnlyCollection<string>? workingFragments = null,
        string? currentResultSummary = null)
    {
        ArgumentNullException.ThrowIfNull(handoff);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(receipts);
        ArgumentNullException.ThrowIfNull(validation);

        var russian = handoff.LanguageCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        var successful = receipts.Where(receipt => receipt.Success).ToList();
        var normalized = successful
            .Select(receipt => new
            {
                Receipt = receipt,
                Result = NormalizeReceipt(receipt)
            })
            .ToList();
        var fragments = (workingFragments ?? [])
            .Append(currentResultSummary ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .TakeLast(8)
            .ToList();
        var missing = graph.Nodes
            .Where(node => validation.MissingActionIds.Contains(
                node.Id,
                StringComparer.OrdinalIgnoreCase))
            .ToList();
        var builder = new StringBuilder();

        builder.AppendLine(russian ? "# Результат с ограничениями" : "# Result with limitations");
        builder.AppendLine();
        builder.AppendLine(russian ? "## Подтверждённая задача" : "## Confirmed task");
        builder.AppendLine(FirstNonEmpty(
            confirmedBrief,
            handoff.Goal,
            handoff.SuggestedDirection,
            russian ? "Постановка не сохранена." : "The task brief was not preserved."));

        var semanticDescriptions = normalized
            .Where(item => string.Equals(
                    item.Receipt.ToolName,
                    "session_image_describe",
                    StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(item.Result.Description))
            .Select(item => item.Result.Description)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (semanticDescriptions.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(russian ? "## Описание сцены" : "## Scene description");
            foreach (var description in semanticDescriptions)
            {
                builder.AppendLine(description);
                builder.AppendLine();
            }
        }

        if (fragments.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(russian
                ? "## Композиция, цвет, свет, атмосфера и стиль"
                : "## Composition, colour, light, atmosphere and style");
            foreach (var fragment in fragments)
            {
                builder.AppendLine(fragment);
                builder.AppendLine();
            }
        }

        var otherConfirmed = normalized
            .Where(item => !string.IsNullOrWhiteSpace(item.Result.UserText)
                && !semanticDescriptions.Contains(
                    item.Result.UserText,
                    StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (otherConfirmed.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(russian ? "## Другие подтверждённые данные" : "## Other confirmed data");
            foreach (var item in otherConfirmed)
            {
                builder.AppendLine($"- {item.Result.UserText.ReplaceLineEndings(" ").Trim()}");
            }
        }

        builder.AppendLine();
        builder.AppendLine(russian ? "## Ограничения наблюдения" : "## Observation limits");
        if (missing.Count == 0)
        {
            builder.AppendLine(russian
                ? "- Дополнительных неподтверждённых предметных действий не обнаружено."
                : "- No additional unverified domain actions were found.");
        }
        else
        {
            foreach (var node in missing)
            {
                builder.AppendLine($"- `{node.CapabilityId}`: {FirstNonEmpty(node.Purpose, node.Layer)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine(russian
            ? "Документ собран только из сохранённой постановки, принятых рабочих фрагментов и нормализованных результатов инструментов. Технические JSON-payload в текст не включены."
            : "This document was assembled only from the saved brief, accepted working fragments and normalized tool results. Technical JSON payloads were not included.");
        return builder.ToString().TrimEnd();
    }

    private NormalizedSpecialistToolResult NormalizeReceipt(
        ExecutionEvidenceReceipt receipt)
    {
        var result = _normalizer.Normalize(receipt.ToolName, receipt.ResultExcerpt);
        if (!string.IsNullOrWhiteSpace(receipt.NormalizedResultText))
        {
            result.Description = receipt.NormalizedResultText;
            result.UserText = receipt.NormalizedResultText;
        }

        return result;
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? string.Empty;
}
