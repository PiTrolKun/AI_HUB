using System.Windows;
using System.Windows.Controls;
using AIHub.Models;
using UserControl = System.Windows.Controls.UserControl;

namespace AIHub.Controls;

public partial class ImageAnalysisBundleConfirmationControl : UserControl
{
    private ImageAnalysisBundleDefinition? _bundle;
    private Func<string, string> _localize = key => key;
    private Func<string, object[], string> _format = (key, _) => key;

    public ImageAnalysisBundleConfirmationControl()
    {
        InitializeComponent();
    }

    public event EventHandler? BackToBundlesRequested;

    public event EventHandler? BackToWorkStartRequested;

    public void Configure(
        ImageAnalysisBundleDefinition bundle,
        Func<string, string> localize,
        Func<string, object[], string> format)
    {
        _bundle = bundle;
        _localize = localize;
        _format = format;
        ApplyLocalization();
    }

    public void ApplyLocalization()
    {
        if (_bundle is null)
        {
            return;
        }

        var bundleTitle = _localize(_bundle.TitleKey);
        TitleText.Text = _format("ImageAnalysis.Confirmation.Title", [bundleTitle]);
        DescriptionText.Text = _localize("ImageAnalysis.Confirmation.Description");
        BundleTitleText.Text = bundleTitle;
        BundleSummaryText.Text = _format(
            "ImageAnalysis.Confirmation.Summary",
            [
                _bundle.Components[0].ModelName,
                _bundle.Components[1].ModelName,
                _bundle.Components[2].ModelName
            ]);
        NextStepText.Text = _localize("ImageAnalysis.Confirmation.NextStep");
        BackToBundlesButton.Content = _localize("ImageAnalysis.Confirmation.BackToBundles");
        BackToWorkStartButton.Content = _localize("ImageAnalysis.Confirmation.BackToStart");
    }

    private void BackToBundlesButton_Click(object sender, RoutedEventArgs e) =>
        BackToBundlesRequested?.Invoke(this, EventArgs.Empty);

    private void BackToWorkStartButton_Click(object sender, RoutedEventArgs e) =>
        BackToWorkStartRequested?.Invoke(this, EventArgs.Empty);
}
