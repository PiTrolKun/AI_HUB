using System.Security.Cryptography;
using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public static class ImageAnalysisSpeechTextService
{
    public static bool ShouldDelaySummaryReveal(
        string? speechMode,
        ImageAnalysisReviewSummary? summary) =>
        ImageAnalysisSpeechModes.Normalize(speechMode) != ImageAnalysisSpeechModes.Off
        && BuildSegments(summary).Count > 0;

    public static IReadOnlyList<CoreSpeechSegment> BuildSegments(ImageAnalysisReviewSummary? summary)
    {
        if (summary is null)
        {
            return [];
        }

        var segments = new List<CoreSpeechSegment>();
        Add(segments, summary.Items, "finding");
        Add(segments, summary.Uncertainties, "uncertainty");
        return segments;
    }

    public static string BuildPlainText(ImageAnalysisReviewSummary? summary) => string.Join(
        Environment.NewLine,
        BuildSegments(summary).Select(segment => segment.Text));

    public static string CreateFingerprint(ImageAnalysisReviewSummary? summary)
    {
        var payload = string.Join(
            "\n\u241e\n",
            BuildSegments(summary).Select(segment => segment.Text));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))
            .ToLowerInvariant();
    }

    private static void Add(
        ICollection<CoreSpeechSegment> target,
        IEnumerable<string> source,
        string prefix)
    {
        var index = 0;
        foreach (var value in source)
        {
            var text = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            index++;
            target.Add(new CoreSpeechSegment($"{prefix}-{index}", text));
        }
    }
}
