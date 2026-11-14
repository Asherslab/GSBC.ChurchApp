using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;

internal readonly record struct NearestPiece(int Line, int Piece, double Distance, double Along, double Cross, Flat Direction);

internal sealed class SegmentGrid(double cellMetres)
{
    private readonly Dictionary<(long, long), List<(int Line, int Piece)>> _cells = [];
    private readonly List<Flat[]> _lines = [];

    public int Add(Flat[] line)
    {
        int index = _lines.Count;
        _lines.Add(line);
        for (int i = 1; i < line.Length; i++)
        {
            (long minX, long minY) = Cell(new Flat(Math.Min(line[i - 1].X, line[i].X), Math.Min(line[i - 1].Y, line[i].Y)));
            (long maxX, long maxY) = Cell(new Flat(Math.Max(line[i - 1].X, line[i].X), Math.Max(line[i - 1].Y, line[i].Y)));
            for (long x = minX; x <= maxX; x++)
                for (long y = minY; y <= maxY; y++)
                {
                    if (!_cells.TryGetValue((x, y), out List<(int, int)>? pieces))
                        _cells[(x, y)] = pieces = [];
                    pieces.Add((index, i - 1));
                }
        }
        return index;
    }

    public NearestPiece? Nearest(Flat point, Func<int, bool>? lineFilter = null, double reachMetres = 0)
    {
        int rings = Math.Max(1, (int)Math.Ceiling(reachMetres / cellMetres));
        (long cx, long cy) = Cell(point);
        NearestPiece? best = null;
        for (long x = cx - rings; x <= cx + rings; x++)
            for (long y = cy - rings; y <= cy + rings; y++)
            {
                if (!_cells.TryGetValue((x, y), out List<(int Line, int Piece)>? pieces))
                    continue;
                foreach ((int line, int piece) in pieces)
                {
                    if (lineFilter is not null && !lineFilter(line))
                        continue;
                    NearestPiece candidate = Measure(point, line, piece);
                    if (best is null || candidate.Distance < best.Value.Distance)
                        best = candidate;
                }
            }
        return best;
    }

    private NearestPiece Measure(Flat point, int line, int piece)
    {
        Flat a = _lines[line][piece];
        Flat direction = _lines[line][piece + 1] - a;
        double lengthSquared = Flat.Dot(direction, direction);
        double t = lengthSquared == 0 ? 0 : Math.Clamp(Flat.Dot(point - a, direction) / lengthSquared, 0, 1);
        Flat closest = a + direction * t;
        return new NearestPiece(line, piece, Flat.Distance(point, closest), t, Flat.Cross(direction, point - a), direction);
    }

    private (long, long) Cell(Flat point) => ((long)Math.Floor(point.X / cellMetres), (long)Math.Floor(point.Y / cellMetres));
}
