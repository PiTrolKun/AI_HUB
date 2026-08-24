using System.Windows;
using System.IO;
using AIHub.Controls;
using AIHub.Models;
using AIHub.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace AIHub;

public partial class MainWindow
{
    private readonly ImageAnalysisRecommendationService _imageAnalysisRecommendationService = new();
    private readonly ImageAnalysisHardwareSnapshotService _imageAnalysisHardwareSnapshotService = new();
    private readonly ImageAnalysisBundleInstallationService _imageAnalysisBundleInstallationService = new();
    private IReadOnlyList<ImageAnalysisBundleDefinition> _imageAnalysisBundles = [];
    private ImageAnalysisRecommendationResult? _imageAnalysisRecommendation;
    private ImageAnalysisBundleDefinition? _selectedImageAnalysisBundle;
    private CancellationTokenSource? _imageAnalysisBundleOperationCts;
    private readonly ImageAnalysisFileValidationService _imageAnalysisFileValidationService = new();
    private readonly ImageAnalysisSessionStore _imageAnalysisSessionStore = new();
    private ImageAnalysisLiteraryService? _imageAnalysisLiteraryService;
    private ImageAnalysisLiterarySession? _imageAnalysisLiterarySession;
    private CancellationTokenSource? _imageAnalysisLiteraryCts;

    private void SelectImageAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        ShowImageAnalysisBundleSelector(refreshHardware: true);
        StatusText.Text = L("Status.ImageAnalysisOpened");
    }

    private void ShowImageAnalysisBundleSelector(bool refreshHardware)
    {
        CancelCoreSpeech(revealFullText: false, "open_image_analysis_selector");
        if (refreshHardware || _imageAnalysisRecommendation is null)
        {
            _lastPassport = _computerPassportService.EnsurePassport();
            _imageAnalysisBundles = ImageAnalysisBundleCatalog.Create();
            var hardware = _imageAnalysisHardwareSnapshotService.Create(
                _lastPassport,
                _storageSettings);
            _imageAnalysisRecommendation = _imageAnalysisRecommendationService.Evaluate(
                _imageAnalysisBundles,
                hardware);
            ImageAnalysisBundleSelectorPage.Configure(
                _imageAnalysisBundles,
                _imageAnalysisRecommendation,
                L,
                LF);
        }

        HideStandardPages();
        ImageAnalysisBundleConfirmationPage.Visibility = Visibility.Collapsed;
        ImageAnalysisBundleSelectorPage.Visibility = Visibility.Visible;
        ImageAnalysisBundleSelectorPage.UpdateResponsiveLayout(ActualWidth);
    }

    private void ShowImageAnalysisBundleConfirmation(ImageAnalysisBundleDefinition bundle)
    {
        _selectedImageAnalysisBundle = bundle;
        var snapshot = _imageAnalysisBundleInstallationService.Check(_storageSettings);
        ImageAnalysisBundleConfirmationPage.Configure(bundle, snapshot, L, LF);
        HideStandardPages();
        ImageAnalysisBundleSelectorPage.Visibility = Visibility.Collapsed;
        ImageAnalysisWorkspacePage.Visibility = Visibility.Collapsed;
        ImageAnalysisBundleConfirmationPage.Visibility = Visibility.Visible;
    }

    private void HideStandardPages()
    {
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Collapsed;
    }

    private void HideImageAnalysisPages()
    {
        ImageAnalysisBundleSelectorPage.Visibility = Visibility.Collapsed;
        ImageAnalysisBundleConfirmationPage.Visibility = Visibility.Collapsed;
        ImageAnalysisWorkspacePage.Visibility = Visibility.Collapsed;
    }

    private void RefreshImageAnalysisLocalization()
    {
        ImageAnalysisScenarioTitleText.Text = L("ImageAnalysis.Scenario.Title");
        ImageAnalysisScenarioDescriptionText.Text = L("ImageAnalysis.Scenario.Description");
        SelectImageAnalysisButton.Content = L("ImageAnalysis.Scenario.Select");

        if (_imageAnalysisRecommendation is not null)
        {
            ImageAnalysisBundleSelectorPage.ApplyLocalization();
        }

        if (_selectedImageAnalysisBundle is not null)
        {
            ImageAnalysisBundleConfirmationPage.ApplyLocalization();
        }

        ImageAnalysisWorkspacePage.ApplyLocalization();
    }

    private bool TryHandleImageAnalysisEscape()
    {
        if (ImageAnalysisWorkspacePage.Visibility == Visibility.Visible)
        {
            if (_imageAnalysisLiteraryCts is not null)
            {
                _imageAnalysisLiteraryCts.Cancel();
                return true;
            }
            SaveCurrentImageAnalysisSession();
            if (_selectedImageAnalysisBundle is not null)
            {
                ShowImageAnalysisBundleConfirmation(_selectedImageAnalysisBundle);
            }
            return true;
        }

        if (ImageAnalysisBundleConfirmationPage.Visibility == Visibility.Visible)
        {
            ShowImageAnalysisBundleSelector(refreshHardware: false);
            StatusText.Text = L("Status.ImageAnalysisOpened");
            return true;
        }

        if (ImageAnalysisBundleSelectorPage.Visibility == Visibility.Visible)
        {
            ShowWorkStartPage();
            StatusText.Text = L("Status.WorkStartOpened");
            return true;
        }

        return false;
    }

    private void ImageAnalysisBundleSelectorPage_BackRequested(object? sender, EventArgs e)
    {
        ShowWorkStartPage();
        StatusText.Text = L("Status.WorkStartOpened");
    }

    private void ImageAnalysisBundleSelectorPage_BundleSelected(
        object? sender,
        ImageAnalysisBundleSelectedEventArgs e)
    {
        ShowImageAnalysisBundleConfirmation(e.Bundle);
        StatusText.Text = LF("Status.ImageAnalysisBundleSelected", L(e.Bundle.TitleKey));
    }

    private void ImageAnalysisBundleSelectorPage_UnavailableBundleRequested(
        object? sender,
        ImageAnalysisBundleSelectedEventArgs e)
    {
        StatusText.Text = LF("Status.ImageAnalysisBundleUnavailable", L(e.Bundle.TitleKey));
    }

    private void ImageAnalysisBundleConfirmationPage_BackToBundlesRequested(
        object? sender,
        EventArgs e)
    {
        ShowImageAnalysisBundleSelector(refreshHardware: false);
        StatusText.Text = L("Status.ImageAnalysisOpened");
    }

    private void ImageAnalysisBundleConfirmationPage_BackToWorkStartRequested(
        object? sender,
        EventArgs e)
    {
        ShowWorkStartPage();
        StatusText.Text = L("Status.WorkStartOpened");
    }

    private async void ImageAnalysisBundleConfirmationPage_ActionRequested(
        object? sender,
        ImageAnalysisBundleActionEventArgs e)
    {
        if (e.Action == ImageAnalysisBundleActions.Start)
        {
            ShowImageAnalysisWorkspace();
            return;
        }

        var snapshot = _imageAnalysisBundleInstallationService.Check(_storageSettings);
        if (e.Action == ImageAnalysisBundleActions.Download)
        {
            var names = string.Join(
                Environment.NewLine,
                snapshot.Components
                    .Where(component => component.Status != ManagedModelStatuses.Installed
                        && component.Status != ManagedModelStatuses.NeedsVerification)
                    .Select(component => $"• {component.DisplayName}"));
            var confirmation = WpfMessageBox.Show(
                this,
                LF(
                    "ImageAnalysis.Install.DownloadConfirm",
                    names,
                    ComponentCardViewModel.FormatBytes(snapshot.MissingBytes),
                    snapshot.ModelsRoot),
                L("ImageAnalysis.Install.DownloadTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }
        }
        else if (e.Action == ImageAnalysisBundleActions.Verify)
        {
            var confirmation = WpfMessageBox.Show(
                this,
                L("ImageAnalysis.Install.RuntimeConfirm"),
                L("ImageAnalysis.Install.VerifyTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _imageAnalysisBundleOperationCts?.Cancel();
        _imageAnalysisBundleOperationCts?.Dispose();
        _imageAnalysisBundleOperationCts = new CancellationTokenSource();
        ImageAnalysisBundleConfirmationPage.SetBusy(true);
        var progress = new Progress<ManagedModelDownloadProgress>(value =>
        {
            ImageAnalysisBundleConfirmationPage.UpdateProgress(value);
            StatusText.Text = LF("Status.ImageAnalysisModelOperation", value.FileName);
        });
        try
        {
            var updated = e.Action == ImageAnalysisBundleActions.Download
                ? await _imageAnalysisBundleInstallationService.DownloadMissingAsync(
                    _storageSettings,
                    progress,
                    _imageAnalysisBundleOperationCts.Token)
                : await _imageAnalysisBundleInstallationService.VerifyAsync(
                    _storageSettings,
                    progress,
                    _imageAnalysisBundleOperationCts.Token);
            ImageAnalysisBundleConfirmationPage.UpdateSnapshot(updated);
            StatusText.Text = updated.CanStart
                ? L("Status.ImageAnalysisBundleReady")
                : L("Status.ImageAnalysisBundleUpdated");
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = L("Status.ImageAnalysisOperationCancelled");
        }
        catch (Exception)
        {
            var updated = _imageAnalysisBundleInstallationService.Check(_storageSettings);
            ImageAnalysisBundleConfirmationPage.UpdateSnapshot(updated);
            StatusText.Text = L("Status.ImageAnalysisOperationFailed");
        }
        finally
        {
            ImageAnalysisBundleConfirmationPage.SetBusy(false);
        }
    }

    private void ImageAnalysisBundleConfirmationPage_RemoveVisionRequested(object? sender, EventArgs e)
    {
        var card = _imageAnalysisBundleInstallationService.LibraryStore.Load(
            ManagedModelCatalog.KimiMediumArtifactId);
        if (card is null)
        {
            return;
        }
        var files = string.Join(Environment.NewLine, card.Files.Select(file => $"• {file.RelativePath}"));
        var confirmation = WpfMessageBox.Show(
            this,
            LF(
                "ImageAnalysis.Install.RemoveVisionConfirm",
                card.DisplayName,
                files,
                ComponentCardViewModel.FormatBytes(card.StoredBytes)),
            L("ImageAnalysis.Install.RemoveVisionTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }
        try
        {
            var result = _imageAnalysisBundleInstallationService.RemoveVisionFiles();
            ImageAnalysisBundleConfirmationPage.UpdateSnapshot(
                _imageAnalysisBundleInstallationService.Check(_storageSettings));
            StatusText.Text = LF(
                "Status.ImageAnalysisVisionRemoved",
                ComponentCardViewModel.FormatBytes(result.RemovedBytes));
        }
        catch (Exception)
        {
            StatusText.Text = L("Status.ImageAnalysisVisionRemoveFailed");
        }
    }

    private void ImageAnalysisBundleConfirmationPage_CancelRequested(object? sender, EventArgs e) =>
        _imageAnalysisBundleOperationCts?.Cancel();

    private void ShowImageAnalysisWorkspace()
    {
        var snapshot = _imageAnalysisBundleInstallationService.Check(_storageSettings);
        if (!snapshot.CanStart)
        {
            ImageAnalysisBundleConfirmationPage.UpdateSnapshot(snapshot);
            return;
        }
        ImageAnalysisWorkspacePage.Configure(L, LF);
        HideStandardPages();
        ImageAnalysisBundleSelectorPage.Visibility = Visibility.Collapsed;
        ImageAnalysisBundleConfirmationPage.Visibility = Visibility.Collapsed;
        ImageAnalysisWorkspacePage.Visibility = Visibility.Visible;
        ShowImageAnalysisSubscenarioSelection();
        StatusText.Text = L("Status.ImageAnalysisWorkspaceOpened");
    }

    private void ImageAnalysisWorkspacePage_BackRequested(object? sender, EventArgs e)
    {
        if (_selectedImageAnalysisBundle is not null)
        {
            ShowImageAnalysisBundleConfirmation(_selectedImageAnalysisBundle);
        }
    }

}
