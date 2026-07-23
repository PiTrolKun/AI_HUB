using AIHub.Models;

namespace AIHub.Services;

public sealed record SessionTreeNodePlacement(
    string NodeId,
    double X,
    double Y,
    double Width,
    double Height,
    int Depth);

public sealed record SessionTreeEdgePlacement(
    string ParentId,
    string ChildId,
    double StartX,
    double StartY,
    double EndX,
    double EndY);

public sealed class SessionTreeLayoutResult
{
    public double Width { get; init; }
    public double Height { get; init; }
    public List<SessionTreeNodePlacement> Nodes { get; init; } = [];
    public List<SessionTreeEdgePlacement> Edges { get; init; } = [];
}

public static class SessionTreeLayoutEngine
{
    public const double CardWidth = 230;
    public const double CardHeight = 104;
    public const double HorizontalGap = 28;
    public const double VerticalGap = 66;
    public const double CanvasMargin = 56;

    public static SessionTreeLayoutResult Calculate(
        SessionKnowledgeTreeSnapshot snapshot,
        IReadOnlySet<string>? collapsedNodeIds = null)
    {
        if (!snapshot.HasNodes)
        {
            return new SessionTreeLayoutResult
            {
                Width = 800,
                Height = 500
            };
        }

        var byId = snapshot.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        if (!byId.ContainsKey(snapshot.RootId))
        {
            return new SessionTreeLayoutResult
            {
                Width = 800,
                Height = 500
            };
        }

        var children = snapshot.Nodes
            .Where(node => node.ParentId is not null && byId.ContainsKey(node.ParentId))
            .GroupBy(node => node.ParentId!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(node => node.Sequence).ToList(),
                StringComparer.Ordinal);
        var subtreeWidths = new Dictionary<string, double>(StringComparer.Ordinal);
        Measure(snapshot.RootId, children, collapsedNodeIds, subtreeWidths);

        var placements = new List<SessionTreeNodePlacement>();
        Place(
            snapshot.RootId,
            0,
            CanvasMargin,
            children,
            collapsedNodeIds,
            subtreeWidths,
            placements);

        var placementById = placements.ToDictionary(item => item.NodeId, StringComparer.Ordinal);
        var edges = placements
            .Where(item => byId[item.NodeId].ParentId is not null
                && placementById.ContainsKey(byId[item.NodeId].ParentId!))
            .Select(item =>
            {
                var parent = placementById[byId[item.NodeId].ParentId!];
                return new SessionTreeEdgePlacement(
                    parent.NodeId,
                    item.NodeId,
                    parent.X + (parent.Width / 2),
                    parent.Y + parent.Height,
                    item.X + (item.Width / 2),
                    item.Y);
            })
            .ToList();
        var width = placements.Count == 0
            ? 800
            : placements.Max(item => item.X + item.Width) + CanvasMargin;
        var height = placements.Count == 0
            ? 500
            : placements.Max(item => item.Y + item.Height) + CanvasMargin;
        return new SessionTreeLayoutResult
        {
            Width = Math.Max(800, width),
            Height = Math.Max(500, height),
            Nodes = placements,
            Edges = edges
        };
    }

    private static double Measure(
        string nodeId,
        IReadOnlyDictionary<string, List<SessionKnowledgeNode>> children,
        IReadOnlySet<string>? collapsedNodeIds,
        IDictionary<string, double> widths)
    {
        if (collapsedNodeIds?.Contains(nodeId) == true
            || !children.TryGetValue(nodeId, out var nodeChildren)
            || nodeChildren.Count == 0)
        {
            widths[nodeId] = CardWidth;
            return CardWidth;
        }

        var childWidth = nodeChildren.Sum(child =>
            Measure(child.Id, children, collapsedNodeIds, widths));
        childWidth += HorizontalGap * (nodeChildren.Count - 1);
        widths[nodeId] = Math.Max(CardWidth, childWidth);
        return widths[nodeId];
    }

    private static void Place(
        string nodeId,
        int depth,
        double subtreeLeft,
        IReadOnlyDictionary<string, List<SessionKnowledgeNode>> children,
        IReadOnlySet<string>? collapsedNodeIds,
        IReadOnlyDictionary<string, double> widths,
        ICollection<SessionTreeNodePlacement> placements)
    {
        var subtreeWidth = widths[nodeId];
        var x = subtreeLeft + ((subtreeWidth - CardWidth) / 2);
        var y = CanvasMargin + (depth * (CardHeight + VerticalGap));
        placements.Add(new SessionTreeNodePlacement(
            nodeId,
            x,
            y,
            CardWidth,
            CardHeight,
            depth));

        if (collapsedNodeIds?.Contains(nodeId) == true
            || !children.TryGetValue(nodeId, out var nodeChildren))
        {
            return;
        }

        var childLeft = subtreeLeft;
        foreach (var child in nodeChildren)
        {
            Place(
                child.Id,
                depth + 1,
                childLeft,
                children,
                collapsedNodeIds,
                widths,
                placements);
            childLeft += widths[child.Id] + HorizontalGap;
        }
    }
}
