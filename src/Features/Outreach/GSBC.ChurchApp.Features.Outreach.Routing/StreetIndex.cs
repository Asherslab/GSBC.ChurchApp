namespace GSBC.ChurchApp.Features.Outreach.Routing;

public sealed class StreetIndex
{
    private const double CellMetres = 100;

    private readonly StreetGraph _graph;
    private readonly double _minX;
    private readonly double _minY;
    private readonly int _columns;
    private readonly int _rows;
    private readonly int[] _cellStart;
    private readonly int[] _cellStreets;
    private readonly int[] _nodeCellStart;
    private readonly int[] _cellNodes;

    internal StreetIndex(StreetGraph graph)
    {
        _graph = graph;
        double maxX = double.MinValue, maxY = double.MinValue;
        _minX = double.MaxValue;
        _minY = double.MaxValue;
        foreach (Flat point in graph.ShapeFlat)
        {
            _minX = Math.Min(_minX, point.X);
            _minY = Math.Min(_minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }
        if (graph.ShapeFlat.Length == 0)
            _minX = _minY = maxX = maxY = 0;
        _columns = (int)((maxX - _minX) / CellMetres) + 1;
        _rows = (int)((maxY - _minY) / CellMetres) + 1;

        List<(int Cell, int Street)> pairs = [];
        HashSet<int> cells = [];
        for (int s = 0; s < graph.StreetCount; s++)
        {
            cells.Clear();
            ReadOnlySpan<Flat> line = graph.Shape(s);
            for (int i = 0; i < line.Length - 1; i++)
            {
                double length = Flat.Distance(line[i], line[i + 1]);
                int steps = Math.Max(1, (int)(length / (CellMetres / 2)));
                for (int k = 0; k <= steps; k++)
                    cells.Add(CellOf(line[i] + (line[i + 1] - line[i]) * (k / (double)steps)));
            }
            foreach (int cell in cells)
                pairs.Add((cell, s));
        }
        (_cellStart, _cellStreets) = Bucket(pairs, _columns * _rows);

        List<(int Cell, int Node)> nodePairs = [];
        for (int n = 0; n < graph.NodeCount; n++)
        {
            if (graph.StreetsAt(n).Length > 0)
                nodePairs.Add((CellOf(graph.NodeFlat(n)), n));
        }
        (_nodeCellStart, _cellNodes) = Bucket(nodePairs, _columns * _rows);
    }

    public IEnumerable<int> StreetsNear(Flat point, double radius)
    {
        HashSet<int> seen = [];
        foreach (int cell in CellsAround(point, radius))
        {
            for (int i = _cellStart[cell]; i < _cellStart[cell + 1]; i++)
            {
                if (seen.Add(_cellStreets[i]))
                    yield return _cellStreets[i];
            }
        }
    }

    public IEnumerable<int> NodesNear(Flat point, double radius)
    {
        foreach (int cell in CellsAround(point, radius))
        {
            for (int i = _nodeCellStart[cell]; i < _nodeCellStart[cell + 1]; i++)
            {
                if (Flat.Distance(_graph.NodeFlat(_cellNodes[i]), point) <= radius)
                    yield return _cellNodes[i];
            }
        }
    }

    public IEnumerable<int> StreetsIn(Flat low, Flat high)
    {
        HashSet<int> seen = [];
        int c0 = Column(low.X), c1 = Column(high.X), r0 = Row(low.Y), r1 = Row(high.Y);
        for (int r = r0; r <= r1; r++)
        {
            for (int c = c0; c <= c1; c++)
            {
                int cell = r * _columns + c;
                for (int i = _cellStart[cell]; i < _cellStart[cell + 1]; i++)
                {
                    if (seen.Add(_cellStreets[i]))
                        yield return _cellStreets[i];
                }
            }
        }
    }

    public StreetPosition? Nearest(Flat point, double maxMetres, Func<int, bool>? accept = null)
    {
        StreetPosition? best = null;
        for (double radius = Math.Min(CellMetres, maxMetres); ; radius = Math.Min(radius * 2, maxMetres))
        {
            foreach (int street in StreetsNear(point, radius))
            {
                if (accept is not null && !accept(street))
                    continue;
                StreetPosition position = _graph.Project(street, point);
                if (position.Distance <= maxMetres && (best is null || position.Distance < best.Value.Distance))
                    best = position;
            }
            if (best is not null && best.Value.Distance <= radius || radius >= maxMetres)
                return best;
        }
    }

    public int? NearestNode(Flat point, double maxMetres, Func<int, bool>? accept = null)
    {
        for (double radius = Math.Min(CellMetres, maxMetres); ; radius = Math.Min(radius * 2, maxMetres))
        {
            int? best = null;
            double bestDistance = double.MaxValue;
            foreach (int node in NodesNear(point, radius))
            {
                double distance = Flat.Distance(_graph.NodeFlat(node), point);
                if (distance < bestDistance && (accept is null || accept(node)))
                    (best, bestDistance) = (node, distance);
            }
            if (best is not null || radius >= maxMetres)
                return best;
        }
    }

    private IEnumerable<int> CellsAround(Flat point, double radius)
    {
        int c0 = Column(point.X - radius), c1 = Column(point.X + radius), r0 = Row(point.Y - radius), r1 = Row(point.Y + radius);
        for (int r = r0; r <= r1; r++)
        {
            for (int c = c0; c <= c1; c++)
                yield return r * _columns + c;
        }
    }

    private int Column(double x) => Math.Clamp((int)((x - _minX) / CellMetres), 0, _columns - 1);

    private int Row(double y) => Math.Clamp((int)((y - _minY) / CellMetres), 0, _rows - 1);

    private int CellOf(Flat point) => Row(point.Y) * _columns + Column(point.X);

    private static (int[] Start, int[] Items) Bucket(List<(int Cell, int Item)> pairs, int cells)
    {
        int[] start = new int[cells + 1];
        foreach ((int cell, _) in pairs)
            start[cell + 1]++;
        for (int c = 0; c < cells; c++)
            start[c + 1] += start[c];
        int[] fill = start[..^1].ToArray();
        int[] items = new int[pairs.Count];
        foreach ((int cell, int item) in pairs)
            items[fill[cell]++] = item;
        return (start, items);
    }
}
