using System.Globalization;
using AIHub.Models;

namespace AIHub.Services;

public static class ChoiceModelCandidateSelector
{
    public static bool IsVerifiedChoice(string selectedExecutor, IEnumerable<string> toolEvidence)
    {
        if (string.IsNullOrWhiteSpace(selectedExecutor))
        {
            return false;
        }

        var candidates = toolEvidence
            .SelectMany(evidence => evidence.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            .Select(line => line.Trim())
            .SelectMany(ExtractCandidateNames)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return candidates.Contains(selectedExecutor.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryGetVerifiedParameterCount(
        string selectedExecutor,
        IEnumerable<string> toolEvidence,
        out long parameterCount)
    {
        parameterCount = 0;
        var selectedLine = "- " + selectedExecutor.Trim();
        foreach (var evidence in toolEvidence)
        {
            var lines = evidence.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < lines.Length; index++)
            {
                if (!string.Equals(lines[index].Trim(), selectedLine, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var detail in lines.Skip(index + 1).Take(4))
                {
                    const string marker = "parameters:";
                    var markerIndex = detail.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (markerIndex < 0)
                    {
                        continue;
                    }

                    var value = detail[(markerIndex + marker.Length)..].Trim();
                    var separator = value.IndexOf(';');
                    if (long.TryParse(separator >= 0 ? value[..separator] : value, out parameterCount))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static bool TryGetCatalogCandidate(
        string selectedExecutor,
        IEnumerable<string> toolEvidence,
        out ModelCatalogCandidate candidate)
    {
        candidate = new ModelCatalogCandidate();
        if (string.IsNullOrWhiteSpace(selectedExecutor))
        {
            return false;
        }

        var selectedLine = "- " + selectedExecutor.Trim();
        foreach (var evidence in toolEvidence.Where(value =>
                     value.Contains("model_catalog_search", StringComparison.OrdinalIgnoreCase)))
        {
            var lines = evidence.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < lines.Length; index++)
            {
                if (!string.Equals(lines[index].Trim(), selectedLine, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                candidate.RepoId = selectedExecutor.Trim();
                foreach (var detail in lines.Skip(index + 1).TakeWhile(line =>
                             !line.TrimStart().StartsWith("- ", StringComparison.Ordinal)).Take(8))
                {
                    var normalized = detail.Trim();
                    if (normalized.StartsWith("pipeline:", StringComparison.OrdinalIgnoreCase))
                    {
                        candidate.PipelineTag = ReadField(normalized, "pipeline");
                        candidate.License = ReadField(normalized, "license");
                        candidate.ParameterCount = ReadLongField(normalized, "parameters");
                        candidate.ContextLength = ReadLongField(normalized, "context");
                    }
                    else if (normalized.StartsWith("lineage:", StringComparison.OrdinalIgnoreCase))
                    {
                        var baseModels = ReadField(normalized, "base_models");
                        candidate.BaseModels = string.Equals(baseModels, "none", StringComparison.OrdinalIgnoreCase)
                            ? []
                            : baseModels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                        candidate.ModelType = ReadField(normalized, "model_type");
                    }
                    else if (normalized.StartsWith("hardware:", StringComparison.OrdinalIgnoreCase))
                    {
                        candidate.Hardware.Status = ReadField(normalized, "status");
                        var compatible = ReadField(normalized, "compatible");
                        candidate.Hardware.IsCompatible = bool.TryParse(compatible, out var parsedCompatible)
                            ? parsedCompatible
                            : null;
                        candidate.Hardware.EstimatedQ4RuntimeGb = ReadDoubleField(normalized, "estimated_q4_runtime_gb");
                        candidate.Hardware.AvailableRamGb = ReadDoubleField(normalized, "ram_gb") ?? 0;
                        candidate.Hardware.AvailableVramGb = ReadDoubleField(normalized, "vram_gb") ?? 0;
                    }
                }

                return true;
            }
        }

        return false;
    }

    private static string ReadField(string line, string field)
    {
        var marker = field + "=";
        var index = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            marker = field + ":";
            index = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        }
        if (index < 0)
        {
            return string.Empty;
        }

        var value = line[(index + marker.Length)..].Trim();
        var separator = value.IndexOf(';');
        return (separator >= 0 ? value[..separator] : value).Trim();
    }

    private static long? ReadLongField(string line, string field)
    {
        var value = ReadField(line, field);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static double? ReadDoubleField(string line, string field)
    {
        var value = ReadField(line, field);
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static IEnumerable<string> ExtractCandidateNames(string line)
    {
        if (line.StartsWith("- ", StringComparison.Ordinal) && line.Contains('/'))
        {
            yield return line[2..].Trim();
        }

        const string filePrefix = "file:";
        if (line.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var value = line[filePrefix.Length..].Trim();
            var separator = value.IndexOf(';');
            yield return (separator >= 0 ? value[..separator] : value).Trim();
        }
    }
}
