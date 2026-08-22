using System.Windows;
using System.Windows.Controls;
using AIHub.Models;
using AIHub.Services;

namespace AIHub;

public partial class ExecutorResultWindow : Window
{
    private readonly List<ExecutorResultSnapshot> _snapshots = [];
    private readonly Func<string, string> _localize;

    public ExecutorResultWindow(
        string title,
        string versionLabel,
        string hint,
        Func<string, string> localize)
    {
        _localize = localize ?? throw new ArgumentNullException(nameof(localize));
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
            var statusLines = new[]
            {
                BuildStatusLine(
                    "Executor.ResultStatus.RouteQuality",
                    "Executor.ResultStatus.Quality.",
                    snapshot.ArtifactQualityLevel),
                BuildStatusLine(
                    "Executor.ResultStatus.Artifact",
                    "Executor.ResultStatus.ArtifactValue.",
                    snapshot.ArtifactValidationStatus),
                BuildStatusLine(
                    "Executor.ResultStatus.Evidence",
                    "Executor.ResultStatus.EvidenceValue.",
                    snapshot.EvidenceValidationStatus),
                BuildStatusLine(
                    "Executor.ResultStatus.Task",
                    "Executor.ResultStatus.TaskValue.",
                    snapshot.TaskFulfillmentStatus)
            };
            ArtifactDetailsText.Text = string.Join(Environment.NewLine, statusLines)
                + (string.IsNullOrWhiteSpace(snapshot.ArtifactPath)
                    ? string.Empty
                    : Environment.NewLine + snapshot.ArtifactPath);
        }
    }

    private string BuildStatusLine(
        string labelKey,
        string valueKeyPrefix,
        string value) =>
        $"{_localize(labelKey)}: {_localize(valueKeyPrefix + value)}";
}
