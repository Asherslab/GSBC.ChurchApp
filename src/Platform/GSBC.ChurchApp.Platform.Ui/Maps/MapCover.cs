namespace GSBC.ChurchApp.Platform.Ui.Maps;

internal sealed class MapCover
{
    private const double SameLine = 1e-7;
    private const double Nudge = 1e-9;

    private readonly double[] _lons;
    private readonly double[] _lats;
    private readonly bool[,] _filled;

    public MapCover(IReadOnlyList<MapFrame> inside, IReadOnlyList<MapFrame> without)
    {
        List<MapFrame> boxes = [.. inside.Concat(without).Where(box => box.East - box.West > SameLine && box.North - box.South > SameLine)];
        _lons = Lines(boxes.SelectMany(box => new[] { box.West, box.East }));
        _lats = Lines(boxes.SelectMany(box => new[] { box.South, box.North }));
        _filled = new bool[Math.Max(0, _lons.Length - 1), Math.Max(0, _lats.Length - 1)];
        Mark(inside, true);
        Mark(without, false);
    }

    public IReadOnlyList<IReadOnlyList<MapPoint>> Loops()
    {
        List<((int, int) From, (int, int) To)> sides = [.. Sides()];
        Dictionary<(int, int), List<int>> startingAt = [];
        for (int index = 0; index < sides.Count; index++)
        {
            if (!startingAt.TryGetValue(sides[index].From, out List<int>? list))
                startingAt[sides[index].From] = list = [];
            list.Add(index);
        }
        bool[] used = new bool[sides.Count];
        List<IReadOnlyList<MapPoint>> loops = [];
        for (int first = 0; first < sides.Count; first++)
        {
            if (used[first])
                continue;
            List<(int, int)> corners = [sides[first].From];
            int at = first;
            while (at >= 0 && !used[at])
            {
                used[at] = true;
                corners.Add(sides[at].To);
                at = Next(sides, startingAt.GetValueOrDefault(sides[at].To) ?? [], used, sides[at]);
            }
            if (corners.Count >= 5)
                loops.Add(Straightened(corners));
        }
        return loops;
    }

    public IReadOnlyList<IReadOnlyList<IReadOnlyList<MapPoint>>> Polygons()
    {
        IReadOnlyList<IReadOnlyList<MapPoint>> loops = Loops();
        List<IReadOnlyList<MapPoint>> outlines = [.. loops.Where(loop => Area(loop) > 0)];
        List<List<IReadOnlyList<MapPoint>>> polygons = [.. outlines.Select(outline => new List<IReadOnlyList<MapPoint>> { outline })];
        foreach (IReadOnlyList<MapPoint> hole in loops.Where(loop => Area(loop) < 0))
        {
            MapPoint probe = BesideFirstSide(hole);
            int owner = Enumerable.Range(0, outlines.Count).Where(index => Contains(outlines[index], probe)).DefaultIfEmpty(-1).MinBy(index => index < 0 ? double.MaxValue : Area(outlines[index]));
            if (owner >= 0)
                polygons[owner].Add(hole);
        }
        return polygons;
    }

    public MapPoint? LargestPiece()
    {
        (double Size, MapPoint Middle)? best = null;
        for (int column = 0; column < _filled.GetLength(0); column++)
            for (int row = 0; row < _filled.GetLength(1); row++)
            {
                if (!_filled[column, row])
                    continue;
                double size = (_lons[column + 1] - _lons[column]) * (_lats[row + 1] - _lats[row]);
                if (best is null || size > best.Value.Size)
                    best = (size, new MapPoint((_lats[row] + _lats[row + 1]) / 2, (_lons[column] + _lons[column + 1]) / 2));
            }
        return best?.Middle;
    }

    private static double Area(IReadOnlyList<MapPoint> loop)
    {
        double sum = 0;
        for (int index = 0; index + 1 < loop.Count; index++)
            sum += loop[index].Lon * loop[index + 1].Lat - loop[index + 1].Lon * loop[index].Lat;
        return sum / 2;
    }

    private static double[] Lines(IEnumerable<double> values)
    {
        List<double> lines = [];
        foreach (double value in values.Order())
            if (lines.Count == 0 || value - lines[^1] > SameLine)
                lines.Add(value);
        return [.. lines];
    }

    private static int IndexOf(double[] lines, double value)
    {
        int found = Array.BinarySearch(lines, value);
        if (found >= 0)
            return found;
        int after = ~found;
        return after > 0 && (after == lines.Length || value - lines[after - 1] < lines[after] - value) ? after - 1 : after;
    }

    private void Mark(IReadOnlyList<MapFrame> boxes, bool filled)
    {
        foreach (MapFrame box in boxes)
            for (int column = IndexOf(_lons, box.West); column < IndexOf(_lons, box.East); column++)
                for (int row = IndexOf(_lats, box.South); row < IndexOf(_lats, box.North); row++)
                    _filled[column, row] = filled;
    }

    private bool Filled(int column, int row) =>
        column >= 0 && row >= 0 && column < _filled.GetLength(0) && row < _filled.GetLength(1) && _filled[column, row];

    private IEnumerable<((int, int) From, (int, int) To)> Sides()
    {
        for (int column = 0; column < _filled.GetLength(0); column++)
            for (int row = 0; row < _filled.GetLength(1); row++)
            {
                if (!_filled[column, row])
                    continue;
                if (!Filled(column, row - 1))
                    yield return ((column, row), (column + 1, row));
                if (!Filled(column + 1, row))
                    yield return ((column + 1, row), (column + 1, row + 1));
                if (!Filled(column, row + 1))
                    yield return ((column + 1, row + 1), (column, row + 1));
                if (!Filled(column - 1, row))
                    yield return ((column, row + 1), (column, row));
            }
    }

    private static int Next(List<((int, int) From, (int, int) To)> sides, List<int> candidates, bool[] used, ((int, int) From, (int, int) To) arriving)
    {
        (int x, int y) heading = Direction(arriving);
        return candidates.Where(index => !used[index]).DefaultIfEmpty(-1).MaxBy(index => index < 0 ? int.MinValue : Turn(heading, Direction(sides[index])));
    }

    private static (int, int) Direction(((int Column, int Row) From, (int Column, int Row) To) side) =>
        (Math.Sign(side.To.Column - side.From.Column), Math.Sign(side.To.Row - side.From.Row));

    private static int Turn((int X, int Y) heading, (int X, int Y) next) => heading.X * next.Y - heading.Y * next.X;

    private IReadOnlyList<MapPoint> Straightened(List<(int Column, int Row)> corners)
    {
        List<(int Column, int Row)> kept = [];
        int count = corners.Count - 1;
        for (int index = 0; index < count; index++)
        {
            (int Column, int Row) before = corners[(index + count - 1) % count];
            (int Column, int Row) here = corners[index];
            (int Column, int Row) after = corners[index + 1];
            bool straight = (before.Column == here.Column && here.Column == after.Column) || (before.Row == here.Row && here.Row == after.Row);
            if (!straight)
                kept.Add(here);
        }
        kept.Add(kept[0]);
        return [.. kept.Select(corner => new MapPoint(_lats[corner.Row], _lons[corner.Column]))];
    }

    private static MapPoint BesideFirstSide(IReadOnlyList<MapPoint> loop)
    {
        (MapPoint from, MapPoint to) = (loop[0], loop[1]);
        double length = Math.Sqrt(Math.Pow(to.Lat - from.Lat, 2) + Math.Pow(to.Lon - from.Lon, 2));
        return new MapPoint((from.Lat + to.Lat) / 2 + (to.Lon - from.Lon) / length * Nudge, (from.Lon + to.Lon) / 2 - (to.Lat - from.Lat) / length * Nudge);
    }

    private static bool Contains(IReadOnlyList<MapPoint> loop, MapPoint point)
    {
        bool inside = false;
        for (int index = 0, previous = loop.Count - 1; index < loop.Count; previous = index++)
            if ((loop[index].Lat > point.Lat) != (loop[previous].Lat > point.Lat)
                && point.Lon < (loop[previous].Lon - loop[index].Lon) * (point.Lat - loop[index].Lat) / (loop[previous].Lat - loop[index].Lat) + loop[index].Lon)
                inside = !inside;
        return inside;
    }
}
