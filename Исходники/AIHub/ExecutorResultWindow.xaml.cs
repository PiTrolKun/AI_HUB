using System.Windows;
using System.Windows.Controls;
using AIHub.Models;
using AIHub.Services;

namespace AIHub;

public partial class ExecutorResultWindow : Window
{
    private readonly List<ExecutorResultSnapshot> _snapshots = [];

    public ExecutorResultWindow(string title, string versionLabel, string hint)
    {
        InitializeComponent();
        Title = title;
        WindowHeadingText.Text = title;
        VersionLabelText.Text = versionLabel;
        WindowHintText.Text = hint;
        SnapshotSelector.ItemsSource = _snapshots;
    }

    public void AddSnapshot(ExecutorResultSnapshot snapshot)
    {
        _snapshots.Add(snapshot);
        SnapshotSelector.Items.Refresh();
        SnapshotSelector.SelectedItem = snapshot;
    }

    private void SnapshotSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SnapshotSelector.SelectedItem is ExecutorResultSnapshot snapshot)
        {
            DocumentViewer.Document = ExecutorMarkdownDocumentBuilder.Build(snapshot.Markdown);
        }
    }
}
