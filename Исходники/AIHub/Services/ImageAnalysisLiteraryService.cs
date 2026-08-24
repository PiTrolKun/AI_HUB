using System.IO;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public sealed class ImageAnalysisLiteraryService : IDisposable
{
    private readonly ImageAnalysisKimiRuntimeService _kimiRuntime;
    private readonly LlamaServerRuntimeService _coreRuntime;
    private readonly CoreModelManager _coreModelManager = new();
    private bool _disposed;

    public ImageAnalysisLiteraryService(
        ImageAnalysisKimiRuntimeService kimiRuntime,
        LlamaServerRuntimeService coreRuntime)
    {
        _kimiRuntime = kimiRuntime;
        _coreRuntime = coreRuntime;
    }

    public async Task<ImageAnalysisLiteraryResult> CreateAsync(
        ImageAnalysisFilePassport passport,
        ImageAnalysisLiterarySettings settings,
        StorageSettings storageSettings,
        string? existingVisualReport,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        IProgress<ModelStreamChunk>? streamProgress,
        Action<string>? visualReportReady,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passport);
        ArgumentNullException.ThrowIfNull(settings);
        _coreRuntime.Stop();
        var visualReport = existingVisualReport?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(visualReport))
        {
            progress?.Report(new ImageAnalysisLiteraryProgress(
                ManagedModelRoles.Vision,
                "vision",
                "The visual analyst is studying the image."));
            visualReport = await _kimiRuntime.DescribeAsync(
                passport,
                ImageAnalysisLiteraryPromptBuilder.BuildVisionPrompt(settings),
                log,
                progress,
                cancellationToken);
        }
        visualReportReady?.Invoke(visualReport);

        progress?.Report(new ImageAnalysisLiteraryProgress(
            ManagedModelRoles.Core,
            "writing",
            "The AI HUB core is creating the literary description."));
        var coreModel = ResolveCoreModel(storageSettings);
        var coreResultText = await _coreRuntime.GenerateTextAsync(
            coreModel,
            ImageAnalysisLiteraryPromptBuilder.BuildInitialSystemPrompt(settings),
            ImageAnalysisLiteraryPromptBuilder.BuildInitialUserPrompt(settings, visualReport),
            ResolveMaxTokens(settings.Length),
            ResolveTemperature(settings.Accuracy),
            log,
            cancellationToken,
            streamProgress);
        var parsed = ImageAnalysisCoreResultParser.Parse(coreResultText);
        var description = parsed.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidDataException("The AI HUB core returned an empty literary description.");
        }
        return new ImageAnalysisLiteraryResult(visualReport.Trim(), description, parsed.Summary);
    }

    public async Task<string> ReviseAsync(
        ImageAnalysisLiterarySession session,
        string changeRequest,
        StorageSettings storageSettings,
        Action<string> log,
        IProgress<ImageAnalysisLiteraryProgress>? progress,
        IProgress<ModelStreamChunk>? streamProgress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        var current = session.GetSelectedVersion()
            ?? throw new InvalidOperationException("There is no description version to revise.");
        if (string.IsNullOrWhiteSpace(session.VisualReport))
        {
            throw new InvalidOperationException("The visual report is missing from this session.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(changeRequest);

        progress?.Report(new ImageAnalysisLiteraryProgress(
            ManagedModelRoles.Core,
            "revising",
            "The AI HUB core is preparing a new version."));
        var coreModel = ResolveCoreModel(storageSettings);
        var revised = await _coreRuntime.GenerateTextAsync(
            coreModel,
            ImageAnalysisLiteraryPromptBuilder.BuildRevisionSystemPrompt(),
            ImageAnalysisLiteraryPromptBuilder.BuildRevisionUserPrompt(
                session.Settings,
                session.VisualReport,
                current.Text,
                changeRequest),
            ResolveMaxTokens(session.Settings.Length),
            ResolveTemperature(session.Settings.Accuracy),
            log,
            cancellationToken,
            streamProgress);
        revised = NormalizeModelText(revised);
        if (string.IsNullOrWhiteSpace(revised))
        {
            throw new InvalidDataException("The AI HUB core returned an empty revised description.");
        }
        return revised;
    }

    public void Stop() => _coreRuntime.Stop();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _coreRuntime.Dispose();
        _kimiRuntime.Dispose();
        _disposed = true;
    }

    internal static string NormalizeModelText(string text)
    {
        var value = (text ?? string.Empty).Trim();
        if (value.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLine = value.IndexOf('\n');
            var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLine >= 0 && lastFence > firstLine)
            {
                value = value[(firstLine + 1)..lastFence].Trim();
            }
        }

        if (value.StartsWith('{') && value.EndsWith('}'))
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                foreach (var name in new[] { "description", "text", "result", "content" })
                {
                    if (document.RootElement.TryGetProperty(name, out var property)
                        && property.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(property.GetString()))
                    {
                        return property.GetString()!.Trim();
                    }
                }
            }
            catch (JsonException)
            {
                // Keep a non-JSON model response as plain text.
            }
        }
        return value;
    }

    private DebugModelInfo ResolveCoreModel(StorageSettings storageSettings)
    {
        var check = _coreModelManager.Check(storageSettings);
        if (check.Availability != CoreModelAvailability.Installed
            || string.IsNullOrWhiteSpace(check.ModelPath)
            || !File.Exists(check.ModelPath))
        {
            throw new InvalidOperationException("The AI HUB core model is not installed and verified.");
        }
        var info = new FileInfo(check.ModelPath);
        return new DebugModelInfo
        {
            Name = CoreModelManager.CoreModelDisplayName,
            Path = info.FullName,
            SizeBytes = info.Length,
            Role = "core",
            Status = "installed",
            Format = "GGUF",
            IsCoreModel = true,
            IsRunnable = true
        };
    }

    private static int ResolveMaxTokens(string length) => length switch
    {
        ImageAnalysisTextLengths.Brief => 700,
        ImageAnalysisTextLengths.Detailed => 2800,
        _ => 1600
    };

    private static double ResolveTemperature(string accuracy) => accuracy switch
    {
        ImageAnalysisAccuracyModes.Strict => 0.25,
        ImageAnalysisAccuracyModes.Free => 0.75,
        _ => 0.5
    };
}
