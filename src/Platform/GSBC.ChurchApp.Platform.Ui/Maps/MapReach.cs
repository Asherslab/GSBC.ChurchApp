namespace GSBC.ChurchApp.Platform.Ui.Maps;

public sealed record MapReach(IReadOnlyList<MapFrame> Covered, IReadOnlyList<MapArea> Others, IReadOnlyList<MapMarker> Notes, MapFrame? Limit = null, IReadOnlyList<MapPoint>? Addable = null)
{
    private const int Samples = 8;
    private static readonly MapFrame World = new(-85, -180, 85, 180);

    private IReadOnlyList<IReadOnlyList<MapPoint>>? _edge;

    public IReadOnlyList<IReadOnlyList<MapPoint>> Edge() => _edge ??= new MapCover(Covered, []).Loops();

    public IReadOnlyList<IReadOnlyList<IReadOnlyList<MapPoint>>> Beyond() => new MapCover([World], Covered).Polygons();

    public IReadOnlyList<IReadOnlyList<MapPoint>> Closed()
    {
        if (Addable is not { Count: >= 3 } addable)
            return [];
        IReadOnlyList<MapPoint> outer = Ring(World);
        List<MapPoint> hole = [.. addable, addable[0]];
        if (Math.Sign(Turning(hole)) == Math.Sign(Turning(outer)))
            hole.Reverse();
        return [outer, hole];
    }

    private static double Turning(IReadOnlyList<MapPoint> ring)
    {
        double sum = 0;
        for (int index = 1; index < ring.Count; index++)
            sum += (ring[index].Lon - ring[index - 1].Lon) * (ring[index].Lat + ring[index - 1].Lat);
        return sum;
    }

    public IReadOnlyList<IReadOnlyList<IReadOnlyList<MapPoint>>> Outside(MapFrame box) => new MapCover([box], Covered).Polygons();

    public MapPoint? FreeSpot(MapFrame box)
    {
        MapPoint middle = new((box.South + box.North) / 2, (box.West + box.East) / 2);
        return Covers(middle) ? new MapCover([box], Covered).LargestPiece() : middle;
    }

    private bool Covers(MapPoint point) => Covered.Any(box => Inside(box, point));

    public double ShareOf(MapFrame view)
    {
        int inside = 0;
        for (int row = 0; row < Samples; row++)
            for (int column = 0; column < Samples; column++)
            {
                MapPoint point = new(view.South + (view.North - view.South) * (row + 0.5) / Samples, view.West + (view.East - view.West) * (column + 0.5) / Samples);
                if (Covers(point))
                    inside++;
            }
        return inside / (double)(Samples * Samples);
    }

    public static IReadOnlyList<MapPoint> Ring(MapFrame box) =>
        [new(box.North, box.West), new(box.North, box.East), new(box.South, box.East), new(box.South, box.West), new(box.North, box.West)];

    public static bool Inside(MapFrame box, MapPoint point) =>
        point.Lat >= box.South && point.Lat <= box.North && point.Lon >= box.West && point.Lon <= box.East;
}
