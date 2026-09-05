using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Panel = System.Windows.Controls.Panel;
using TextBox = System.Windows.Controls.TextBox;
using Binding = System.Windows.Data.Binding;
using MenuItem = System.Windows.Controls.MenuItem;
using ContextMenu = System.Windows.Controls.ContextMenu;

namespace AIHub.Controls;

public partial class ImageAnalysisWorkspaceControl
{
    private Window? _savedWorksWindow;
    private string? _selectedSavedWork;
    public event EventHandler? ExportAudioRequested;

    private Window CreateWorkspacePopup(string title, object content)
    {
        var owner = Window.GetWindow(this);
        var window = new Window { Owner = owner, Title = title, Content = content,
            Width = Math.Min(900, owner?.ActualWidth ?? 900), Height = 550,
            MinWidth = 400, MinHeight = 300, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner };
        if (owner is not null) window.Resources.MergedDictionaries.Add(owner.Resources);
        window.SetResourceReference(BackgroundProperty, "PanelBrush");
        window.SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
        window.PreviewKeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) window.Close(); };
        return window;
    }

    private void ShowSavedWorks()
    {
        var parent = (Panel)HistoryContentBorder.Parent;
        parent.Children.Remove(HistoryContentBorder);
        HistoryContentBorder.BeginAnimation(OpacityProperty, null);
        HistoryContentBorder.Opacity = 1;
        HistoryContentBorder.MaxHeight = double.PositiveInfinity;
        HistoryContentBorder.Visibility = Visibility.Visible;
        HistoryScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _selectedSavedWork = null;
        var window = CreateWorkspacePopup(HistoryTitleText.Text, HistoryContentBorder);
        _savedWorksWindow = window;
        try { window.ShowDialog(); }
        finally
        {
            window.Content = null;
            _savedWorksWindow = null;
            HistoryContentBorder.Visibility = Visibility.Collapsed;
            parent.Children.Add(HistoryContentBorder);
        }
        if (_selectedSavedWork is { } id)
            ResumeRequested?.Invoke(this, new ImageAnalysisSessionRequestedEventArgs(id));
    }

    private void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        var text = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(14) };
        text.SetBinding(TextBox.TextProperty, new Binding("Text") { Source = HeavyDiagnosticsTextBox });
        CreateWorkspacePopup(_localize("ImageAnalysis.Workspace.HeavyDiagnostics.Title"), text).ShowDialog();
    }

    private bool _audioAvailable;
    private bool _audioPlaying;

    public void SetAudioPlaybackState(bool available, bool playing)
    {
        _audioAvailable = available;
        _audioPlaying = playing;
        RefreshAudioPlaybackPresentation();
    }

    private void RefreshAudioPlaybackPresentation()
    {
        if (_speechMode != AIHub.Models.ImageAnalysisSpeechModes.Kokoro) return;
        var available = _audioAvailable;
        var playing = _audioPlaying;
        ReplaySpeechButton.Visibility = Visibility.Visible;
        ReplaySpeechButton.IsEnabled = available;
        ReplaySpeechButton.Content = playing ? "⏸" : "▶";
        var label = _localize(playing ? "ImageAnalysis.Workspace.Audio.Pause" : "ImageAnalysis.Workspace.Audio.Play");
        ReplaySpeechButton.ToolTip = label;
        System.Windows.Automation.AutomationProperties.SetName(ReplaySpeechButton, label);
        var export = new MenuItem { Header = _localize("ImageAnalysis.Workspace.Audio.Export"), IsEnabled = available };
        export.Click += (_, _) => ExportAudioRequested?.Invoke(this, EventArgs.Empty);
        var menu = new ContextMenu(); menu.Items.Add(export);
        ReplaySpeechButton.ContextMenu = menu;
    }
}
