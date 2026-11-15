namespace GSBC.ChurchApp.Platform.Ui.Maps;

public sealed record MapReach(IReadOnlyList<MapFrame> Covered, IReadOnlyList<MapArea> Others, IReadOnlyList<MapMarker> Notes, MapFrame? Limit = null)
{
    private const double SameLine = 1e-7;
    private const int Samples = 8;
    private static readonly IReadOnlyList<MapPoint> World = [new(-85, -180), new(-85, 180), new(85, 180), new(85, -180), new(-85, -180)];

    private IReadOnlyList<IReadOnlyList<MapPoint>>? _edge;

    public IReadOnlyList<IReadOnlyList<MapPoint>> Edge() => _edge ??= Loops();

    public IReadOnlyList<IReadOnlyList<IReadOnlyList<MapPoint>>> Beyond()
    {
        List<IReadOnlyList<MapPoint>> outlines = [.. Edge().Where(loop => Area(loop) > 0)];
        List<IReadOnlyList<MapPoint>> gaps = [.. Edge().Where(loop => Area(loop) < 0)];
        return [[World, .. outlines.Select(Reversed)], .. gaps.Select(gap => (IReadOnlyList<IReadOnlyList<MapPoint>>)[Reversed(gap)])];
    }

    public double ShareOf(MapFrame view)
    {
        int inside = 0;
        for (int row = 0; row < Samples; row++)
            for (int column = 0; column < Samples; column++)
            {
                MapPoint point = new(view.South + (view.North - view.South) * (row + 0.5) / Samples, view.West + (view.East - view.West) * (column + 0.5) / Samples);
                if (Covered.Any(box => Inside(box, point)))
                    inside++;
            }
        return inside / (double)(Samples * Samples);
    }

    public static IReadOnlyList<MapPoint> Ring(MapFrame box) =>
        [new(box.North, box.West), new(box.North, box.East), new(box.South, box.East), new(box.South, box.West), new(box.North, box.West)];

    public static bool Inside(MapFrame box, MapPoint point) =>
        point.Lat >= box.South && point.Lat <= box.North && point.Lon >= box.West && point.Lon <= box.East;

    private List<IReadOnlyList<MapPoint>> Loops()
    {
        List<(MapPoint From, MapPoint To)> sides = [.. Covered.SelectMany(SidesOutside)];
        Dictionary<(long, long), List<int>> startingAt = [];
        for (int index = 0; index < sides.Count; index++)
        {
            (long, long) key = Key(sides[index].From);
            if (!startingAt.TryGetValue(key, out List<int>? list))
                startingAt[key] = list = [];
            list.Add(index);
        }
        bool[] used = new bool[sides.Count];
        List<IReadOnlyList<MapPoint>> loops = [];
        for (int first = 0; first < sides.Count; first++)
        {
            if (used[first])
                continue;
            List<MapPoint> loop = [sides[first].From];
            int at = first;
            while (at >= 0 && !used[at])
            {
                used[at] = true;
                loop.Add(sides[at].To);
                at = startingAt.TryGetValue(Key(sides[at].To), out List<int>? next) ? next.FirstOrDefault(index => !used[index], -1) : -1;
            }
            if (loop.Count >= 4)
                loops.Add(loop);
        }
        return loops;
    }

    private IEnumerable<(MapPoint From, MapPoint To)> SidesOutside(MapFrame box)
    {
        foreach ((double from, double to) in Uncovered(box.West, box.East, Covered.Where(other => Same(other.South, box.North)).Select(other => (other.West, other.East))))
            yield return (new MapPoint(box.North, to), new MapPoint(box.North, from));
        foreach ((double from, double to) in Uncovered(box.West, box.East, Covered.Where(other => Same(other.North, box.South)).Select(other => (other.West, other.East))))
            yield return (new MapPoint(box.South, from), new MapPoint(box.South, to));
        foreach ((double from, double to) in Uncovered(box.South, box.North, Covered.Where(other => Same(other.East, box.West)).Select(other => (other.South, other.North))))
            yield return (new MapPoint(to, box.West), new MapPoint(from, box.West));
        foreach ((double from, double to) in Uncovered(box.South, box.North, Covered.Where(other => Same(other.West, box.East)).Select(other => (other.South, other.North))))
            yield return (new MapPoint(from, box.East), new MapPoint(to, box.East));
    }

    private static List<(double From, double To)> Uncovered(double from, double to, IEnumerable<(double From, double To)> neighbours)
    {
        List<(double From, double To)> left = [(from, to)];
        foreach ((double coverFrom, double coverTo) in neighbours)
            left = [.. left.SelectMany(part => Without(part, coverFrom, coverTo))];
        return [.. left.Where(part => part.To - part.From > SameLine)];
    }

    private static IEnumerable<(double From, double To)> Without((double From, double To) part, double coverFrom, double coverTo)
    {
        if (coverTo <= part.From || coverFrom >= part.To)
        {
            yield return part;
            yield break;
        }
        if (coverFrom > part.From)
            yield return (part.From, coverFrom);
        if (coverTo < part.To)
            yield return (coverTo, part.To);
    }

    private static double Area(IReadOnlyList<MapPoint> loop)
    {
        double sum = 0;
        for (int index = 0; index + 1 < loop.Count; index++)
            sum += loop[index].Lon * loop[index + 1].Lat - loop[index + 1].Lon * loop[index].Lat;
        return sum / 2;
    }

    private static IReadOnlyList<MapPoint> Reversed(IReadOnlyList<MapPoint> loop) => [.. loop.Reverse()];

    private static (long, long) Key(MapPoint point) => ((long)Math.Round(point.Lat / SameLine), (long)Math.Round(point.Lon / SameLine));

    private static bool Same(double one, double other) => Math.Abs(one - other) < SameLine;
}
