using System.Windows;
using AIHub.Services;

namespace AIHub;

public partial class ImageAnalysisPreviewWindow : Window
{
    public ImageAnalysisPreviewWindow(string title, string closeText, string markdown)
    {
        InitializeComponent();
        Title = title;
        HeadingText.Text = title;
        CloseButton.Content = closeText;
        DocumentViewer.Document = ExecutorMarkdownDocumentBuilder.Build(markdown);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
