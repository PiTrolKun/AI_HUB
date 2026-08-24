using System.IO;
using System.Windows;
using AIHub.Models;
using UserControl = System.Windows.Controls.UserControl;

namespace AIHub.Controls;

public partial class ImageAnalysisWorkspaceControl : UserControl
{
    private Func<string, string> _localize = key => key;
    private Func<string, object[], string> _format = (key, _) => key;
    private string _selectedPath = string.Empty;

    public ImageAnalysisWorkspaceControl()
    {
        InitializeComponent();
    }

    public event EventHandler? BackRequested;

    public event EventHandler? SelectImageRequested;

    public void Configure(Func<string, string> localize, Func<string, object[], string> format)
    {
        _localize = localize;
        _format = format;
        ApplyLocalization();
    }

    public void SetSelectedFile(string path)
    {
        _selectedPath = path;
        ApplyLocalization();
    }

    public void ApplyLocalization()
    {
        TitleText.Text = _localize("ImageAnalysis.Workspace.Title");
        DescriptionText.Text = _localize("ImageAnalysis.Workspace.Description");
        SelectImageButton.Content = _localize("ImageAnalysis.Workspace.SelectImage");
        BoundaryText.Text = _localize("ImageAnalysis.Workspace.Boundary");
        BackButton.Content = _localize("ImageAnalysis.Workspace.Back");
        if (string.IsNullOrWhiteSpace(_selectedPath))
        {
            SelectedFilePanel.Visibility = Visibility.Collapsed;
            return;
        }
        var info = new FileInfo(_selectedPath);
        SelectedFilePanel.Visibility = Visibility.Visible;
        SelectedFileTitleText.Text = info.Name;
        SelectedFileDetailsText.Text = _format(
            "ImageAnalysis.Workspace.FileDetails",
            [info.Extension.TrimStart('.').ToUpperInvariant(), ComponentCardViewModel.FormatBytes(info.Length), info.FullName]);
    }

    private void SelectImageButton_Click(object sender, RoutedEventArgs e) =>
        SelectImageRequested?.Invoke(this, EventArgs.Empty);

    private void BackButton_Click(object sender, RoutedEventArgs e) =>
        BackRequested?.Invoke(this, EventArgs.Empty);
}
