using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using AIHub.Models;
using AIHub.Services;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace AIHub;

public sealed class SessionTreeWindowStrings
{
    public string Title { get; init; } = string.Empty;
    public string Hint { get; init; } = string.Empty;
    public string ZoomIn { get; init; } = string.Empty;
    public string ZoomOut { get; init; } = string.Empty;
    public string Fit { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string SelectNode { get; init; } = string.Empty;
    public string Collapse { get; init; } = string.Empty;
    public string Expand { get; init; } = string.Empty;
    public string Task { get; init; } = string.Empty;
    public string Requirement { get; init; } = string.Empty;
    public string Decision { get; init; } = string.Empty;
    public string Knowledge { get; init; } = string.Empty;
    public string ResultFragment { get; init; } = string.Empty;
    public string OpenQuestion { get; init; } = string.Empty;
    public string Assumption { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
}

public partial class SessionTreeWindow : Window
{
    private const double MinimumZoom = 0.28;
    private const double MaximumZoom = 1.65;
    private readonly SessionTreeWindowStrings _strings;
    private readonly HashSet<string> _knownNodeIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _collapsedNodeIds = new(StringComparer.Ordinal);
    private SessionKnowledgeTreeSnapshot _snapshot = new();
    private SessionTreeLayoutResult _layout = new();
    private SessionKnowledgeNode? _selectedNode;
    private ScaleTransform _scaleTransform = new(1, 1);
    private Point _panStart;
    private double _panHorizontalOffset;
    private double _panVerticalOffset;
    private bool _isPanning;
    private double _zoom = 1;

    public SessionTreeWindow(
        SessionTreeWindowStrings strings,
        SessionKnowledgeTreeSnapshot snapshot,
        bool isDarkTheme)
    {
        InitializeComponent();
        _strings = strings;
        ApplyStrings();
        ApplyTheme(isDarkTheme);
        UpdateSnapshot(snapshot, animate: true);
        Loaded += (_, _) => Dispatcher.BeginInvoke(FitTree, DispatcherPriority.Loaded);
    }

    public void UpdateSnapshot(SessionKnowledgeTreeSnapshot snapshot, bool animate = true)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => UpdateSnapshot(snapshot, animate));
            return;
        }

        var activeNodeIsNew = snapshot.Nodes.Any(node =>
            node.Id == snapshot.ActiveNodeId && !_knownNodeIds.Contains(node.Id));
        _snapshot = snapshot;
        RenderTree(animate);
        if (activeNodeIsNew)
        {
            Dispatcher.BeginInvoke(
                () => FocusNode(snapshot.ActiveNodeId),
                DispatcherPriority.Background);
        }
    }

    private void ApplyStrings()
    {
        Title = _strings.Title;
        HeadingText.Text = _strings.Title;
        HintText.Text = _strings.Hint;
        ZoomInButton.ToolTip = _strings.ZoomIn;
        ZoomOutButton.ToolTip = _strings.ZoomOut;
        FitButton.ToolTip = _strings.Fit;
        System.Windows.Automation.AutomationProperties.SetName(ZoomInButton, _strings.ZoomIn);
        System.Windows.Automation.AutomationProperties.SetName(ZoomOutButton, _strings.ZoomOut);
        System.Windows.Automation.AutomationProperties.SetName(FitButton, _strings.Fit);
        DetailsHeadingText.Text = _strings.Details;
        DetailsContentText.Text = _strings.SelectNode;
        CollapseBranchButton.Content = _strings.Collapse;
        CollapseBranchButton.IsEnabled = false;
    }

    public void ApplyTheme(bool isDarkTheme)
    {
        SetBrush("WindowBackgroundBrush", isDarkTheme ? "#111827" : "#F4F7FB");
        SetBrush("HeaderBackgroundBrush", isDarkTheme ? "#0B1220" : "#FFFFFF");
        SetBrush("PanelBrush", isDarkTheme ? "#172033" : "#FFFFFF");
        SetBrush("LineBrush", isDarkTheme ? "#2D374B" : "#CBD5E1");
        SetBrush("TextPrimaryBrush", isDarkTheme ? "#F8FAFC" : "#172033");
        SetBrush("TextSecondaryBrush", isDarkTheme ? "#AAB4C4" : "#526174");
        SetBrush("AccentBrush", "#2F6FED");
        if (_snapshot.HasNodes)
        {
            RenderTree(animate: false);
        }
    }

    private void SetBrush(string key, string color) =>
        Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

    private void RenderTree(bool animate)
    {
        var newNodeIds = _snapshot.Nodes
            .Where(node => !_knownNodeIds.Contains(node.Id))
            .Select(node => node.Id)
            .ToHashSet(StringComparer.Ordinal);
        _layout = SessionTreeLayoutEngine.Calculate(_snapshot, _collapsedNodeIds);
        TreeCanvas.Children.Clear();
        TreeCanvas.Width = _layout.Width;
        TreeCanvas.Height = _layout.Height;
        _scaleTransform = new ScaleTransform(_zoom, _zoom);
        TreeCanvas.LayoutTransform = _scaleTransform;

        var byId = _snapshot.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in _layout.Edges)
        {
            if (!byId.TryGetValue(edge.ChildId, out var child))
            {
                continue;
            }

            var path = CreateEdge(edge, child.IsActive);
            TreeCanvas.Children.Add(path);
            if (animate && newNodeIds.Contains(edge.ChildId))
            {
                AnimateEdge(path, byId[edge.ChildId].Sequence);
            }
        }

        foreach (var placement in _layout.Nodes)
        {
            if (!byId.TryGetValue(placement.NodeId, out var node))
            {
                continue;
            }

            var card = CreateNodeCard(node, placement);
            TreeCanvas.Children.Add(card);
            Canvas.SetLeft(card, placement.X);
            Canvas.SetTop(card, placement.Y);
            if (animate && newNodeIds.Contains(node.Id))
            {
                AnimateNode(card, placement.Depth);
            }
        }

        foreach (var nodeId in newNodeIds)
        {
            _knownNodeIds.Add(nodeId);
        }

        if (_selectedNode is not null)
        {
            _selectedNode = _snapshot.Nodes.FirstOrDefault(node => node.Id == _selectedNode.Id);
            UpdateDetails();
        }
    }

    private Path CreateEdge(SessionTreeEdgePlacement edge, bool isActive)
    {
        var middleY = edge.StartY + ((edge.EndY - edge.StartY) * 0.52);
        var figure = new PathFigure
        {
            StartPoint = new Point(edge.StartX, edge.StartY),
            IsClosed = false
        };
        figure.Segments.Add(new BezierSegment(
            new Point(edge.StartX, middleY),
            new Point(edge.EndX, middleY),
            new Point(edge.EndX, edge.EndY),
            true));
        return new Path
        {
            Data = new PathGeometry([figure]),
            Stroke = BrushFrom(isActive ? "#4F83F1" : "#536174"),
            StrokeThickness = isActive ? 2.2 : 1.3,
            Opacity = isActive ? 0.92 : 0.38,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(0.5, 0)
        };
    }

    private Border CreateNodeCard(
        SessionKnowledgeNode node,
        SessionTreeNodePlacement placement)
    {
        var accent = GetNodeColor(node.Type);
        var border = new Border
        {
            Width = placement.Width,
            Height = placement.Height,
            Background = BrushFrom(GetCardBackground()),
            BorderBrush = BrushFrom(
                node.Id == _snapshot.ActiveNodeId
                    ? "#6B96F4"
                    : node.IsActive
                        ? "#3C4B63"
                        : "#303949"),
            BorderThickness = new Thickness(node.Id == _snapshot.ActiveNodeId ? 2 : 1),
            CornerRadius = new CornerRadius(7),
            Opacity = node.IsActive ? 1 : 0.48,
            Cursor = Cursors.Hand,
            Tag = node,
            ToolTip = node.Content
        };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new Border
        {
            Background = accent,
            CornerRadius = new CornerRadius(6, 0, 0, 6)
        });
        var content = new StackPanel
        {
            Margin = new Thickness(12, 9, 10, 8)
        };
        Grid.SetColumn(content, 1);
        content.Children.Add(new TextBlock
        {
            Text = GetTypeLabel(node.Type),
            Foreground = accent,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold
        });
        content.Children.Add(new TextBlock
        {
            Text = node.Title,
            Foreground = (Brush)Resources["TextPrimaryBrush"],
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = 36,
            Margin = new Thickness(0, 3, 0, 0)
        });
        content.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(node.Content) ? " " : node.Content,
            Foreground = (Brush)Resources["TextSecondaryBrush"],
            FontSize = 10.5,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = 31,
            ClipToBounds = true,
            Margin = new Thickness(0, 4, 0, 0)
        });
        grid.Children.Add(content);
        border.Child = grid;
        border.MouseLeftButtonDown += NodeCard_MouseLeftButtonDown;
        return border;
    }

    private void AnimateNode(Border card, int depth)
    {
        card.Opacity = 0;
        var scale = new ScaleTransform(0.88, 0.88);
        card.RenderTransform = scale;
        card.RenderTransformOrigin = new Point(0.5, 0);
        var delay = TimeSpan.FromMilliseconds(Math.Min(depth * 55, 440));
        card.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0, card.Tag is SessionKnowledgeNode { IsActive: false } ? 0.48 : 1,
                new Duration(TimeSpan.FromMilliseconds(240)))
            {
                BeginTime = delay,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.88, 1, TimeSpan.FromMilliseconds(260))
            {
                BeginTime = delay,
                EasingFunction = new BackEase { Amplitude = 0.18, EasingMode = EasingMode.EaseOut }
            });
        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.88, 1, TimeSpan.FromMilliseconds(260))
            {
                BeginTime = delay,
                EasingFunction = new BackEase { Amplitude = 0.18, EasingMode = EasingMode.EaseOut }
            });
    }

    private static void AnimateEdge(Path path, int sequence)
    {
        var scale = new ScaleTransform(1, 0);
        path.RenderTransform = scale;
        var delay = TimeSpan.FromMilliseconds(Math.Min(sequence * 18, 420));
        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            {
                BeginTime = delay,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private void NodeCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: SessionKnowledgeNode node })
        {
            return;
        }

        _selectedNode = node;
        UpdateDetails();
        if (e.ClickCount == 2 && HasChildren(node.Id))
        {
            ToggleCollapsed(node.Id);
        }

        e.Handled = true;
    }

    private void UpdateDetails()
    {
        if (_selectedNode is null)
        {
            DetailsHeadingText.Text = _strings.Details;
            DetailsTypeText.Text = string.Empty;
            DetailsContentText.Text = _strings.SelectNode;
            CollapseBranchButton.IsEnabled = false;
            return;
        }

        DetailsHeadingText.Text = _selectedNode.Title;
        DetailsTypeText.Text = GetTypeLabel(_selectedNode.Type);
        DetailsContentText.Text = string.IsNullOrWhiteSpace(_selectedNode.Content)
            ? _strings.SelectNode
            : _selectedNode.Content;
        CollapseBranchButton.IsEnabled = HasChildren(_selectedNode.Id);
        CollapseBranchButton.Content = _collapsedNodeIds.Contains(_selectedNode.Id)
            ? _strings.Expand
            : _strings.Collapse;
    }

    private bool HasChildren(string nodeId) =>
        _snapshot.Nodes.Any(node =>
            string.Equals(node.ParentId, nodeId, StringComparison.Ordinal));

    private void ToggleCollapsed(string nodeId)
    {
        if (!_collapsedNodeIds.Add(nodeId))
        {
            _collapsedNodeIds.Remove(nodeId);
        }

        RenderTree(animate: false);
        UpdateDetails();
    }

    private void CollapseBranchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedNode is not null && HasChildren(_selectedNode.Id))
        {
            ToggleCollapsed(_selectedNode.Id);
        }
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e) =>
        SetZoom(_zoom + 0.12);

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e) =>
        SetZoom(_zoom - 0.12);

    private void FitButton_Click(object sender, RoutedEventArgs e) => FitTree();

    private void TreeScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        SetZoom(_zoom + (e.Delta > 0 ? 0.1 : -0.1));
        e.Handled = true;
    }

    private void SetZoom(double value)
    {
        _zoom = Math.Clamp(value, MinimumZoom, MaximumZoom);
        _scaleTransform.ScaleX = _zoom;
        _scaleTransform.ScaleY = _zoom;
        ZoomText.Text = $"{_zoom * 100:0}%";
    }

    private void FitTree()
    {
        if (_layout.Width <= 0 || _layout.Height <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(200, TreeScrollViewer.ViewportWidth - 28);
        var availableHeight = Math.Max(180, TreeScrollViewer.ViewportHeight - 28);
        SetZoom(Math.Min(availableWidth / _layout.Width, availableHeight / _layout.Height));
        TreeScrollViewer.ScrollToHorizontalOffset(0);
        TreeScrollViewer.ScrollToVerticalOffset(0);
    }

    private void FocusNode(string nodeId)
    {
        var placement = _layout.Nodes.FirstOrDefault(node => node.NodeId == nodeId);
        if (placement is null)
        {
            return;
        }

        var x = ((placement.X + (placement.Width / 2)) * _zoom)
            - (TreeScrollViewer.ViewportWidth / 2);
        var y = ((placement.Y + (placement.Height / 2)) * _zoom)
            - (TreeScrollViewer.ViewportHeight / 2);
        TreeScrollViewer.ScrollToHorizontalOffset(Math.Max(0, x));
        TreeScrollViewer.ScrollToVerticalOffset(Math.Max(0, y));
    }

    private void TreeScrollViewer_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (FindNodeCard(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        _isPanning = true;
        _panStart = e.GetPosition(TreeScrollViewer);
        _panHorizontalOffset = TreeScrollViewer.HorizontalOffset;
        _panVerticalOffset = TreeScrollViewer.VerticalOffset;
        TreeScrollViewer.CaptureMouse();
        TreeScrollViewer.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void TreeScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(TreeScrollViewer);
        TreeScrollViewer.ScrollToHorizontalOffset(
            _panHorizontalOffset - (current.X - _panStart.X));
        TreeScrollViewer.ScrollToVerticalOffset(
            _panVerticalOffset - (current.Y - _panStart.Y));
    }

    private void TreeScrollViewer_PreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;
        TreeScrollViewer.ReleaseMouseCapture();
        TreeScrollViewer.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private static Border? FindNodeCard(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Border { Tag: SessionKnowledgeNode } border)
            {
                return border;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private string GetTypeLabel(string type) => type switch
    {
        SessionKnowledgeNodeTypes.Task => _strings.Task,
        SessionKnowledgeNodeTypes.Requirement => _strings.Requirement,
        SessionKnowledgeNodeTypes.Decision => _strings.Decision,
        SessionKnowledgeNodeTypes.Knowledge => _strings.Knowledge,
        SessionKnowledgeNodeTypes.ResultFragment => _strings.ResultFragment,
        SessionKnowledgeNodeTypes.OpenQuestion => _strings.OpenQuestion,
        SessionKnowledgeNodeTypes.Assumption => _strings.Assumption,
        SessionKnowledgeNodeTypes.Source => _strings.Source,
        _ => _strings.Knowledge
    };

    private static Brush GetNodeColor(string type) => BrushFrom(type switch
    {
        SessionKnowledgeNodeTypes.Task => "#7AA2F7",
        SessionKnowledgeNodeTypes.Requirement => "#F0B35B",
        SessionKnowledgeNodeTypes.Decision => "#A78BFA",
        SessionKnowledgeNodeTypes.Knowledge => "#4FD1C5",
        SessionKnowledgeNodeTypes.ResultFragment => "#4ADE80",
        SessionKnowledgeNodeTypes.OpenQuestion => "#60A5FA",
        SessionKnowledgeNodeTypes.Assumption => "#F472B6",
        SessionKnowledgeNodeTypes.Source => "#94A3B8",
        _ => "#94A3B8"
    });

    private string GetCardBackground() =>
        ((SolidColorBrush)Resources["PanelBrush"]).Color.R < 80
            ? "#131C2E"
            : "#F8FAFC";

    private static SolidColorBrush BrushFrom(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));
}
