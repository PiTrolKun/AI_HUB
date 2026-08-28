using System.Windows;
using AIHub.Controls;
using AIHub.Models;
using AIHub.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace AIHub;

public partial class MainWindow
{
    private readonly CoreSpeechPresentationCoordinator _imageAnalysisProgrammaticSpeechCoordinator =
        new(new CoreVoiceEngineRouter(new EspeakCoreVoiceEngine(), new RhVoiceCoreVoiceEngine()));
    private KokoroSpeechRuntimeService? _imageAnalysisKokoroSpeechService;
    private CancellationTokenSource? _imageAnalysisSpeechCts;
    private CancellationTokenSource? _imageAnalysisSpeechWarmupCts;
    private CancellationTokenSource? _imageAnalysisVoiceDownloadCts;
    private string _lastAutoSpokenImageAnalysisFingerprint = string.Empty;

    private KokoroSpeechRuntimeService GetImageAnalysisKokoroSpeechService() =>
        _imageAnalysisKokoroSpeechService ??= new KokoroSpeechRuntimeService(
            _imageAnalysisBundleInstallationService.LibraryStore);

    private void RefreshImageAnalysisSpeechUi(string? status = null, bool isBusy = false)
    {
        _appSettings.ImageAnalysisSpeech ??= new ImageAnalysisSpeechSettings();
        var settings = _appSettings.ImageAnalysisSpeech;
        var mode = settings.Mode;
        var kokoroService = GetImageAnalysisKokoroSpeechService();
        var installed = kokoroService.IsModelInstalled(_appSettings.LanguageCode);
        var canReplay = ImageAnalysisSpeechTextService
            .BuildSegments(_imageAnalysisLiterarySession?.ReviewSummary)
            .Count > 0;
        status ??= mode switch
        {
            ImageAnalysisSpeechModes.Kokoro when !installed =>
                L("ImageAnalysis.Workspace.Voice.ModelMissing"),
            ImageAnalysisSpeechModes.Kokoro when kokoroService.IsWarm(_appSettings.LanguageCode) =>
                L("ImageAnalysis.Workspace.Voice.KokoroReady"),
            ImageAnalysisSpeechModes.Programmatic =>
                L("ImageAnalysis.Workspace.Voice.ProgrammaticReady"),
            _ => string.Empty
        };
        ImageAnalysisWorkspacePage.SetSpeechState(settings, installed, canReplay, status, isBusy);
    }

    private async void ImageAnalysisWorkspacePage_SpeechModeRequested(
        object? sender,
        ImageAnalysisSpeechModeRequestedEventArgs e)
    {
        CancelImageAnalysisSpeech();
        _appSettings.ImageAnalysisSpeech ??= new ImageAnalysisSpeechSettings();
        _appSettings.ImageAnalysisSpeech.Mode = e.Mode;
        _appSettingsStore.Save(_appSettings);
        RefreshImageAnalysisSpeechUi();

        if (e.Mode == ImageAnalysisSpeechModes.Kokoro
            && GetImageAnalysisKokoroSpeechService().IsModelInstalled(_appSettings.LanguageCode))
        {
            await WarmImageAnalysisSpeechAsync(forceMemoryAttempt: true);
        }

        if (e.Mode != ImageAnalysisSpeechModes.Off
            && ImageAnalysisSpeechTextService.BuildSegments(_imageAnalysisLiterarySession?.ReviewSummary).Count > 0)
        {
            await SpeakCurrentImageAnalysisSummaryAsync(automatic: false);
        }
    }

    private async void ImageAnalysisWorkspacePage_KokoroDownloadRequested(object? sender, EventArgs e)
    {
        if (_managedModelAcquisition is null
            || _managedModelOperationActive
            || _imageAnalysisVoiceDownloadCts is not null)
        {
            return;
        }

        _imageAnalysisBundleInstallationService.Check(_storageSettings);
        var artifactId = ManagedModelCatalog.ResolveKokoroArtifactId(_appSettings.LanguageCode);
        var card = _imageAnalysisBundleInstallationService.LibraryStore.Load(artifactId);
        if (card is null)
        {
            RefreshImageAnalysisSpeechUi(L("ImageAnalysis.Workspace.Voice.DownloadFailed"));
            return;
        }

        var confirmation = WpfMessageBox.Show(
            this,
            LF(
                "ImageAnalysis.Workspace.Voice.DownloadConfirm",
                card.DisplayName,
                ComponentCardViewModel.FormatBytes(card.TotalBytes),
                card.InstallDirectory,
                card.RepositoryId,
                card.License),
            L("ImageAnalysis.Workspace.Voice.DownloadTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        _imageAnalysisVoiceDownloadCts = new CancellationTokenSource();
        var progress = new Progress<ManagedModelDownloadProgress>(value =>
        {
            var message = LF(
                "ImageAnalysis.Workspace.Voice.DownloadProgress",
                ComponentCardViewModel.FormatBytes(value.DownloadedBytes),
                ComponentCardViewModel.FormatBytes(value.TotalBytes));
            RefreshImageAnalysisSpeechUi(message, isBusy: true);
        });
        try
        {
            RefreshImageAnalysisSpeechUi(
                L("ImageAnalysis.Workspace.Voice.Downloading"),
                isBusy: true);
            await _managedModelAcquisition.DownloadAsync(
                artifactId,
                progress,
                _imageAnalysisVoiceDownloadCts.Token);
            RefreshManagedModels();
            RefreshImageAnalysisSpeechUi(L("ImageAnalysis.Workspace.Voice.DownloadComplete"));
            await WarmImageAnalysisSpeechAsync(forceMemoryAttempt: false);
        }
        catch (OperationCanceledException)
        {
            RefreshImageAnalysisSpeechUi(L("ImageAnalysis.Workspace.Voice.DownloadCancelled"));
        }
        catch (Exception ex)
        {
            LogImageAnalysisRuntime($"Kokoro download failed: {ex.Message}");
            RefreshImageAnalysisSpeechUi(L("ImageAnalysis.Workspace.Voice.DownloadFailed"));
        }
        finally
        {
            _imageAnalysisVoiceDownloadCts.Dispose();
            _imageAnalysisVoiceDownloadCts = null;
        }
    }

    private void ImageAnalysisWorkspacePage_SpeechSettingsChanged(
        object? sender,
        ImageAnalysisSpeechSettingsChangedEventArgs e)
    {
        _appSettings.ImageAnalysisSpeech ??= new ImageAnalysisSpeechSettings();
        if (e.Mode == ImageAnalysisSpeechModes.Kokoro)
        {
            _appSettings.ImageAnalysisSpeech.KokoroVolume = e.Volume;
            _appSettings.ImageAnalysisSpeech.KokoroRatePercent = e.RatePercent;
        }
        else if (e.Mode == ImageAnalysisSpeechModes.Programmatic)
        {
            _appSettings.ImageAnalysisSpeech.ProgrammaticVolume = e.Volume;
            _appSettings.ImageAnalysisSpeech.ProgrammaticRatePercent = e.RatePercent;
        }
        else
        {
            return;
        }

        _appSettingsStore.Save(_appSettings);
    }

    private async void ImageAnalysisWorkspacePage_ReplaySpeechRequested(object? sender, EventArgs e) =>
        await SpeakCurrentImageAnalysisSummaryAsync(automatic: false);

    private Task BeginImageAnalysisSpeechWarmup()
    {
        _imageAnalysisSpeechWarmupCts?.Cancel();
        _imageAnalysisSpeechWarmupCts?.Dispose();
        _imageAnalysisSpeechWarmupCts = new CancellationTokenSource();
        return WarmImageAnalysisSpeechAsync(
            forceMemoryAttempt: false,
            _imageAnalysisSpeechWarmupCts.Token);
    }

    private async Task WarmImageAnalysisSpeechAsync(
        bool forceMemoryAttempt,
        CancellationToken cancellationToken = default)
    {
        if (!GetImageAnalysisKokoroSpeechService().IsModelInstalled(_appSettings.LanguageCode))
        {
            RefreshImageAnalysisSpeechUi();
            return;
        }

        var currentMode = _appSettings.ImageAnalysisSpeech?.Mode ?? ImageAnalysisSpeechModes.Off;
        if (currentMode == ImageAnalysisSpeechModes.Kokoro)
        {
            RefreshImageAnalysisSpeechUi(
                L("ImageAnalysis.Workspace.Voice.Preparing"),
                isBusy: true);
        }

        var result = await GetImageAnalysisKokoroSpeechService().WarmAsync(
            _appSettings.LanguageCode,
            forceMemoryAttempt,
            pendingAllocationBytes: 0,
            cancellationToken);
        var memory = result.Memory;
        var status = result.Code switch
        {
            KokoroWarmupCodes.Ready or KokoroWarmupCodes.AlreadyReady =>
                L("ImageAnalysis.Workspace.Voice.KokoroReady"),
            KokoroWarmupCodes.InsufficientMemory =>
                L("ImageAnalysis.Workspace.Voice.LowMemory"),
            KokoroWarmupCodes.RuntimeMissing =>
                L("ImageAnalysis.Workspace.Voice.RuntimeMissing"),
            KokoroWarmupCodes.ModelMissing =>
                L("ImageAnalysis.Workspace.Voice.ModelMissing"),
            KokoroWarmupCodes.Cancelled => string.Empty,
            _ => L("ImageAnalysis.Workspace.Voice.Failed")
        };
        if ((_appSettings.ImageAnalysisSpeech?.Mode ?? ImageAnalysisSpeechModes.Off)
            == ImageAnalysisSpeechModes.Kokoro)
        {
            RefreshImageAnalysisSpeechUi(status);
            await System.Windows.Threading.Dispatcher.Yield(
                System.Windows.Threading.DispatcherPriority.Render);
        }
        else
        {
            RefreshImageAnalysisSpeechUi();
        }

        LogImageAnalysisRuntime(
            $"Kokoro warmup: {result.Code}; language={NormalizeSpeechLanguage(_appSettings.LanguageCode)}; " +
            $"placement=CPU/RAM; load={result.LoadMilliseconds} ms; " +
            $"peakRam={result.PeakWorkingSetBytes} bytes; cpuAvg={result.AverageCpuPercent:F1}%; " +
            $"cpuPeak={result.PeakCpuPercent:F1}%; " +
            $"memoryAvailable={memory?.AvailableBytes ?? 0} bytes; " +
            $"memoryExpected={memory?.ExpectedRuntimeBytes ?? 0} bytes; " +
            $"memoryPending={memory?.PendingAllocationBytes ?? 0} bytes; " +
            $"memorySafetyReserve={memory?.SafetyReserveBytes ?? 0} bytes; " +
            $"memoryRequired={memory?.RequiredBytes ?? 0} bytes; " +
            $"errorStage={DiagnosticValue(result.ErrorStage)}; " +
            $"errorType={DiagnosticValue(result.ErrorType)}; " +
            $"error={DiagnosticValue(result.Error)}; " +
            $"stderr={DiagnosticValue(result.StandardErrorTail)}.");
        if (result.IsReady)
        {
            LogImageAnalysisRuntime(GetImageAnalysisKokoroSpeechService().DescribeCurrentLaunch(
                _appSettings.LanguageCode));
            LogImageAnalysisRuntime(GetImageAnalysisKokoroSpeechService().DescribeCurrentRuntime(
                _appSettings.LanguageCode,
                "warm"));
        }
    }

    private async Task SpeakCurrentImageAnalysisSummaryAsync(
        bool automatic,
        Action? playbackStarted = null)
    {
        var playbackSignal = 0;
        void SignalPlaybackStarted()
        {
            if (Interlocked.Exchange(ref playbackSignal, 1) == 0)
            {
                playbackStarted?.Invoke();
            }
        }

        var session = _imageAnalysisLiterarySession;
        if (session is null)
        {
            SignalPlaybackStarted();
            return;
        }
        var segments = ImageAnalysisSpeechTextService.BuildSegments(session.ReviewSummary);
        if (segments.Count == 0)
        {
            SignalPlaybackStarted();
            return;
        }
        var mode = _appSettings.ImageAnalysisSpeech?.Mode ?? ImageAnalysisSpeechModes.Off;
        if (mode == ImageAnalysisSpeechModes.Off)
        {
            SignalPlaybackStarted();
            return;
        }
        var fingerprint = ImageAnalysisSpeechTextService.CreateFingerprint(session.ReviewSummary);
        if (automatic && string.Equals(
                fingerprint,
                _lastAutoSpokenImageAnalysisFingerprint,
                StringComparison.Ordinal))
        {
            SignalPlaybackStarted();
            return;
        }

        CancelImageAnalysisSpeech();
        var owner = new CancellationTokenSource();
        _imageAnalysisSpeechCts = owner;
        var cancellationToken = owner.Token;
        RefreshImageAnalysisSpeechUi(
            mode == ImageAnalysisSpeechModes.Kokoro
                ? GetImageAnalysisKokoroSpeechService().IsWarm(_appSettings.LanguageCode)
                    ? L("ImageAnalysis.Workspace.Voice.Synthesizing")
                    : L("ImageAnalysis.Workspace.Voice.Preparing")
                : L("ImageAnalysis.Workspace.Voice.Speaking"),
            isBusy: true);
        try
        {
            var completed = mode == ImageAnalysisSpeechModes.Kokoro
                ? await SpeakImageAnalysisWithKokoroAsync(
                    segments,
                    SignalPlaybackStarted,
                    cancellationToken)
                : await SpeakImageAnalysisProgrammaticallyAsync(
                    segments,
                    SignalPlaybackStarted,
                    cancellationToken,
                    fallbackFromKokoro: false);
            if (completed)
            {
                _lastAutoSpokenImageAnalysisFingerprint = fingerprint;
                RefreshImageAnalysisSpeechUi(L("ImageAnalysis.Workspace.Voice.Ready"));
            }
        }
        catch (OperationCanceledException)
        {
            RefreshImageAnalysisSpeechUi();
        }
        finally
        {
            SignalPlaybackStarted();
            if (ReferenceEquals(_imageAnalysisSpeechCts, owner))
            {
                _imageAnalysisSpeechCts = null;
            }
            owner.Dispose();
        }
    }

    private async Task<bool> SpeakImageAnalysisWithKokoroAsync(
        IReadOnlyList<CoreSpeechSegment> segments,
        Action playbackStarted,
        CancellationToken cancellationToken)
    {
        if (!GetImageAnalysisKokoroSpeechService().IsModelInstalled(_appSettings.LanguageCode))
        {
            RefreshImageAnalysisSpeechUi(L("ImageAnalysis.Workspace.Voice.ModelMissing"));
            return false;
        }

        var progress = new Progress<KokoroSpeechProgress>(value =>
        {
            if (value.Stage == KokoroSpeechStages.Playing)
            {
                playbackStarted();
            }
            var status = value.Stage switch
            {
                KokoroSpeechStages.Warming => L("ImageAnalysis.Workspace.Voice.Preparing"),
                KokoroSpeechStages.Synthesizing => L("ImageAnalysis.Workspace.Voice.Synthesizing"),
                KokoroSpeechStages.Playing => L("ImageAnalysis.Workspace.Voice.Speaking"),
                _ => L("ImageAnalysis.Workspace.Voice.Speaking")
            };
            RefreshImageAnalysisSpeechUi(status, isBusy: true);
        });
        var result = await GetImageAnalysisKokoroSpeechService().SpeakAsync(
            _appSettings.LanguageCode,
            string.Join(Environment.NewLine, segments.Select(segment => segment.Text)),
            _appSettings.ImageAnalysisSpeech?.KokoroVolume ?? 100,
            _appSettings.ImageAnalysisSpeech?.KokoroRatePercent ?? 100,
            progress,
            cancellationToken);
        LogImageAnalysisRuntime(
            $"Kokoro speech: {result.Code}; language={NormalizeSpeechLanguage(_appSettings.LanguageCode)}; " +
            $"requestedEngine=kokoro; generation={result.GenerationMilliseconds} ms; " +
            $"firstAudio={result.TimeToFirstAudioMilliseconds} ms; peak={result.PeakWorkingSetBytes} bytes; " +
            $"cpu={result.CpuMilliseconds:F0} ms; cpuAvg={result.AverageCpuPercent:F1}%; " +
            $"cpuPeak={result.PeakCpuPercent:F1}%; errorStage={DiagnosticValue(result.ErrorStage)}; " +
            $"errorType={DiagnosticValue(result.ErrorType)}; error={DiagnosticValue(result.Error)}; " +
            $"stderr={DiagnosticValue(result.StandardErrorTail)}.");
        LogImageAnalysisRuntime(GetImageAnalysisKokoroSpeechService().DescribeCurrentRuntime(
            _appSettings.LanguageCode,
            result.Completed ? "after_speech" : "speech_failed"));
        if (result.Completed)
        {
            var actualVoice = NormalizeSpeechLanguage(_appSettings.LanguageCode) == "en"
                ? "af_heart"
                : "sveta";
            LogImageAnalysisRuntime(
                $"Voice playback completed: requested=kokoro; actual=kokoro; " +
                $"voice={actualVoice}; device=CPU; " +
                $"language={NormalizeSpeechLanguage(_appSettings.LanguageCode)}.");
            return true;
        }
        if (result.Code == KokoroWarmupCodes.Cancelled)
        {
            return false;
        }

        RefreshImageAnalysisSpeechUi(
            L("ImageAnalysis.Workspace.Voice.Fallback"),
            isBusy: true);
        LogImageAnalysisRuntime(
            $"Voice fallback: requested=kokoro; actual=programmatic; " +
            $"language={NormalizeSpeechLanguage(_appSettings.LanguageCode)}; " +
            $"reason={DiagnosticValue(result.Error)}.");
        return await SpeakImageAnalysisProgrammaticallyAsync(
            segments,
            playbackStarted,
            cancellationToken,
            fallbackFromKokoro: true);
    }

    private async Task<bool> SpeakImageAnalysisProgrammaticallyAsync(
        IReadOnlyList<CoreSpeechSegment> segments,
        Action playbackStarted,
        CancellationToken cancellationToken,
        bool fallbackFromKokoro)
    {
        var configured = _appSettings.CoreVoice ?? new CoreVoiceSettings();
        var speechSettings = _appSettings.ImageAnalysisSpeech ?? new ImageAnalysisSpeechSettings();
        var settings = new CoreVoiceSettings
        {
            Enabled = true,
            Provider = configured.Provider,
            Volume = Math.Clamp(speechSettings.ProgrammaticVolume * 2, 0, 200),
            Rate = Math.Clamp(
                (int)Math.Round(120d * speechSettings.ProgrammaticRatePercent / 100d),
                80,
                240),
            RussianVoice = configured.RussianVoice,
            EnglishVoice = configured.EnglishVoice
        };
        var language = NormalizeSpeechLanguage(_appSettings.LanguageCode);
        var usesRhVoice = string.Equals(
                configured.Provider,
                CoreVoiceSettings.RhVoiceProvider,
                StringComparison.OrdinalIgnoreCase)
            && _imageAnalysisProgrammaticSpeechCoordinator.IsRhVoiceAvailable;
        var actualProvider = usesRhVoice ? "RHVoice" : "eSpeak NG";
        var actualVoice = usesRhVoice
            ? language == "en" ? "Bdl" : "Elena"
            : language == "en" ? settings.EnglishVoice : settings.RussianVoice;
        LogImageAnalysisRuntime(
            $"Voice playback started: requested={(fallbackFromKokoro ? "kokoro" : "programmatic")}; " +
            $"actual=programmatic; provider={actualProvider}; voice={actualVoice}; " +
            $"language={language}; fallbackFromKokoro={fallbackFromKokoro}.");
        playbackStarted();
        var result = await _imageAnalysisProgrammaticSpeechCoordinator.PresentAsync(
            new CoreSpeechRequest(
                segments,
                _appSettings.LanguageCode,
                settings,
                "image_analysis_review_summary",
                SpeechRoles.UncertaintyExecutor),
            new Progress<CoreSpeechProgress>(_ => { }),
            _coreSessionLog,
            cancellationToken);
        if (!result.Completed && !result.Skipped)
        {
            RefreshImageAnalysisSpeechUi(L("ImageAnalysis.Workspace.Voice.Failed"));
        }
        LogImageAnalysisRuntime(
            $"Voice playback finished: actual=programmatic; provider={actualProvider}; " +
            $"voice={actualVoice}; language={language}; completed={result.Completed}; " +
            $"skipped={result.Skipped}; errorCode={DiagnosticValue(result.ErrorCode)}.");
        return result.Completed;
    }

    private static string NormalizeSpeechLanguage(string? languageCode) =>
        languageCode?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "ru";

    private static string DiagnosticValue(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "none"
            : value.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= 800 ? normalized : normalized[..800] + "...";
    }

    private void CancelImageAnalysisSpeech()
    {
        _imageAnalysisSpeechCts?.Cancel();
        _imageAnalysisProgrammaticSpeechCoordinator.Cancel();
        _imageAnalysisKokoroSpeechService?.StopPlayback();
    }

    private void StopImageAnalysisSpeechSession()
    {
        CancelImageAnalysisSpeech();
        _imageAnalysisSpeechWarmupCts?.Cancel();
        _imageAnalysisSpeechWarmupCts?.Dispose();
        _imageAnalysisSpeechWarmupCts = null;
        _imageAnalysisVoiceDownloadCts?.Cancel();
        _imageAnalysisKokoroSpeechService?.Stop();
        _lastAutoSpokenImageAnalysisFingerprint = string.Empty;
    }

    private void DisposeImageAnalysisSpeech()
    {
        StopImageAnalysisSpeechSession();
        _imageAnalysisSpeechCts?.Dispose();
        _imageAnalysisSpeechCts = null;
        _imageAnalysisVoiceDownloadCts?.Dispose();
        _imageAnalysisVoiceDownloadCts = null;
        _imageAnalysisProgrammaticSpeechCoordinator.Dispose();
        _imageAnalysisKokoroSpeechService?.Dispose();
        _imageAnalysisKokoroSpeechService = null;
    }
}
