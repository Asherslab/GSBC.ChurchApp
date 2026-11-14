namespace GSBC.ChurchApp.Features.Outreach.Routing;

public readonly record struct CoveredStretch(SideStretch Stretch, DateOnly Walked);

public sealed record CampaignCoverage(string Campaign, IReadOnlyList<CoveredStretch> Covered)
{
    public IReadOnlySet<uint> Reopened { get; init; } = new HashSet<uint>();
}

public sealed record OutreachMarks
{
    public static OutreachMarks None { get; } = new();

    public IReadOnlySet<uint> NoJunkMail { get; init; } = new HashSet<uint>();

    public IReadOnlySet<uint> Hostile { get; init; } = new HashSet<uint>();

    public IReadOnlySet<uint> Excluded { get; init; } = new HashSet<uint>();

    public IReadOnlySet<uint> ShownToWalkers { get; init; } = new HashSet<uint>();

    public IReadOnlyList<IReadOnlyList<GeoPoint>> ExcludedZones { get; init; } = [];
}

public enum LetterboxState : byte
{
    Open,
    Covered,
    NotAHome,
    NoJunkMail,
    Hostile,
    Excluded,
}

public sealed class Openness
{
    private readonly WalkNetwork _network;
    private readonly LetterboxState[] _state;
    private readonly bool[] _streetExcluded;
    private readonly int[] _openWhole;
    private readonly double[] _secondsWhole;

    public Openness(WalkNetwork network, IReadOnlyList<CampaignCoverage> carried, DateOnly today, RouteSettings settings, WalkKind walk = WalkKind.Letterbox, OutreachMarks? marks = null)
    {
        _network = network;
        Settings = settings;
        Walk = walk;
        StreetGraph graph = network.Graph;
        marks ??= OutreachMarks.None;
        Marks = marks;
        _state = new LetterboxState[graph.LetterboxCount];
        _streetExcluded = new bool[graph.StreetCount];

        for (int b = 0; b < _state.Length; b++)
        {
            uint key = graph.LetterboxAddressKey(b);
            _state[b] = !graph.LetterboxKindOf(b).IsHome() ? LetterboxState.NotAHome
                : key != 0 && marks.Hostile.Contains(key) ? LetterboxState.Hostile
                : key != 0 && marks.Excluded.Contains(key) ? LetterboxState.Excluded
                : walk == WalkKind.Letterbox && key != 0 && marks.NoJunkMail.Contains(key) ? LetterboxState.NoJunkMail
                : LetterboxState.Open;
        }
        foreach (IReadOnlyList<GeoPoint> zone in marks.ExcludedZones)
            ExcludeZone(zone);
        MarkCovered(carried, today);
        _openWhole = new int[graph.StreetCount * 2];
        _secondsWhole = new double[graph.StreetCount * 2];
        for (int s = 0; s < graph.StreetCount; s++)
        {
            for (int side = 0; side < 2; side++)
            {
                _openWhole[s * 2 + side] = Count(s, (Side)side, LetterboxState.Open);
                _secondsWhole[s * 2 + side] = Seconds(s, (Side)side);
            }
        }
    }

    public RouteSettings Settings { get; }

    public OutreachMarks Marks { get; }

    public WalkKind Walk { get; }

    public WalkNetwork Network => _network;

    public LetterboxState State(int letterbox) => _state[letterbox];

    public bool IsExcludedStreet(int street) => _streetExcluded[street];

    public int Open(int piece, Side side) =>
        _network[piece].Partial ? Count(piece, side, LetterboxState.Open) : _openWhole[piece * 2 + (int)side];

    public int Open(int piece) => Open(piece, Side.Left) + Open(piece, Side.Right);

    public int Count(int piece, Side side, LetterboxState wanted)
    {
        LetterboxRange range = _network.Letterboxes(piece, side);
        int count = 0;
        for (int b = range.Start; b < range.End; b++)
            count += _state[b] == wanted ? 1 : 0;
        return count;
    }

    public bool CanDeliver(int piece) => _network.Street(piece).CanDeliver && !_streetExcluded[_network[piece].Street];

    public bool Live(int piece) => CanDeliver(piece) && Open(piece) > 0;

    public bool Rested(int piece) =>
        CanDeliver(piece) && Open(piece) == 0 && Count(piece, Side.Left, LetterboxState.Covered) + Count(piece, Side.Right, LetterboxState.Covered) > 0;

    public double DeliverySeconds(int piece, Side side) =>
        !CanDeliver(piece) ? 0 : _network[piece].Partial ? Seconds(piece, side) : _secondsWhole[piece * 2 + (int)side];

    private double Seconds(int piece, Side side)
    {
        LetterboxRange range = _network.Letterboxes(piece, side);
        double seconds = 0;
        for (int b = range.Start; b < range.End; b++)
        {
            if (_state[b] == LetterboxState.Open)
                seconds += Settings.SecondsPer(_network.Graph.LetterboxKindOf(b), Walk);
        }
        return seconds;
    }

    public double WalkSeconds(double metres) => metres / Settings.WalkingMetresPerSecond;

    public double BothSidesSeconds(int piece) =>
        WalkSeconds(2 * _network[piece].Length) + DeliverySeconds(piece, Side.Left) + DeliverySeconds(piece, Side.Right);

    private void MarkCovered(IReadOnlyList<CampaignCoverage> carried, DateOnly today)
    {
        if (carried.Count == 0)
            return;
        StreetGraph graph = _network.Graph;
        DateOnly since = today.AddDays(-Settings.RestDays);
        byte[] coveredBy = new byte[_state.Length];
        int[] stamp = new int[_state.Length];
        for (int c = 0; c < carried.Count; c++)
        {
            foreach (CoveredStretch covered in carried[c].Covered)
            {
                if (covered.Walked <= since || covered.Stretch.Street < 0 || covered.Stretch.Street >= graph.StreetCount)
                    continue;
                LetterboxRange range = graph.Between(covered.Stretch.Street, covered.Stretch.Side, covered.Stretch.From, covered.Stretch.To);
                for (int b = range.Start; b < range.End; b++)
                {
                    uint key = graph.LetterboxAddressKey(b);
                    if (stamp[b] == c + 1 || key != 0 && carried[c].Reopened.Contains(key))
                        continue;
                    stamp[b] = c + 1;
                    coveredBy[b]++;
                }
            }
        }
        for (int b = 0; b < _state.Length; b++)
        {
            if (_state[b] == LetterboxState.Open && coveredBy[b] >= carried.Count)
                _state[b] = LetterboxState.Covered;
        }
    }

    private void ExcludeZone(IReadOnlyList<GeoPoint> zone)
    {
        if (zone.Count < 3)
            return;
        StreetGraph graph = _network.Graph;
        Flat[] polygon = zone.Select(graph.Projection.ToFlat).ToArray();
        Flat low = new(polygon.Min(p => p.X), polygon.Min(p => p.Y));
        Flat high = new(polygon.Max(p => p.X), polygon.Max(p => p.Y));
        foreach (int street in graph.Index.StreetsIn(low, high))
        {
            if (Geometry.Inside(polygon, graph.PointAt(street, 0.5)))
                _streetExcluded[street] = true;
            for (int side = 0; side < 2; side++)
            {
                LetterboxRange range = graph.Letterboxes(street, (Side)side);
                for (int b = range.Start; b < range.End; b++)
                {
                    if (_state[b] == LetterboxState.Open && Geometry.Inside(polygon, graph.LetterboxFlat(b, 10)))
                        _state[b] = LetterboxState.Excluded;
                }
            }
        }
    }
}

public static class Geometry
{
    public static bool Inside(IReadOnlyList<Flat> polygon, Flat point)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Flat a = polygon[i], b = polygon[j];
            if (a.Y > point.Y != b.Y > point.Y && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    public static double? Crossing(Flat p1, Flat p2, Flat q1, Flat q2)
    {
        double d1 = Flat.Cross(q2 - q1, p1 - q1), d2 = Flat.Cross(q2 - q1, p2 - q1);
        double d3 = Flat.Cross(p2 - p1, q1 - p1), d4 = Flat.Cross(p2 - p1, q2 - p1);
        return d1 * d2 < 0 && d3 * d4 < 0 ? d1 / (d1 - d2) : null;
    }

    public static double AngleBetween(double a, double b)
    {
        double turn = (b - a + Math.PI) % (2 * Math.PI);
        return (turn < 0 ? turn + 2 * Math.PI : turn) - Math.PI;
    }
}
