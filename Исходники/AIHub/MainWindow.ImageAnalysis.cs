using System.Windows;
using AIHub.Controls;
using AIHub.Models;
using AIHub.Services;

namespace AIHub;

public partial class MainWindow
{
    private readonly ImageAnalysisRecommendationService _imageAnalysisRecommendationService = new();
    private readonly ImageAnalysisHardwareSnapshotService _imageAnalysisHardwareSnapshotService = new();
    private IReadOnlyList<ImageAnalysisBundleDefinition> _imageAnalysisBundles = [];
    private ImageAnalysisRecommendationResult? _imageAnalysisRecommendation;
    private ImageAnalysisBundleDefinition? _selectedImageAnalysisBundle;

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
    }

    private void ShowImageAnalysisBundleConfirmation(ImageAnalysisBundleDefinition bundle)
    {
        _selectedImageAnalysisBundle = bundle;
        ImageAnalysisBundleConfirmationPage.Configure(bundle, L, LF);
        HideStandardPages();
        ImageAnalysisBundleSelectorPage.Visibility = Visibility.Collapsed;
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
    }

    private bool TryHandleImageAnalysisEscape()
    {
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
}
