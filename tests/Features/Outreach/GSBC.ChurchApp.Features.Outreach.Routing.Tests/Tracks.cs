namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

internal sealed record TrackStyle(double EverySeconds, double NoiseMetres, double DriftMetres, double KerbMetres = 7, double MetresPerSecond = 1.2, int Seed = 20261114)
{
    public static TrackStyle InHand { get; } = new(1, 4, 2);

    public static TrackStyle InPocket { get; } = new(45, 10, 4);

    public static TrackStyle Clean { get; } = new(1, 0, 0);
}

internal static class Tracks
{
    public static readonly DateTimeOffset Began = new(2026, 11, 14, 9, 0, 0, TimeSpan.FromHours(10));

    public static List<GpsFix> Walk(WalkNetwork network, IEnumerable<Arc> arcs, TrackStyle style)
    {
        List<Flat> path = [];
        foreach (Arc arc in arcs)
        {
            IReadOnlyList<Flat> line = network.FlatLine(arc.Piece);
            List<Flat> walking = arc.Forward ? [.. line] : [.. line.Reverse()];
            for (int i = 0; i < walking.Count - 1; i++)
            {
                Flat along = walking[i + 1] - walking[i];
                if (along.Length < 0.01)
                    continue;
                Flat left = new Flat(-along.Y, along.X) * (style.KerbMetres / along.Length);
                path.Add(walking[i] + left);
                path.Add(walking[i + 1] + left);
            }
        }
        return Sample(network.Graph, path, style);
    }

    public static List<GpsFix> Straight(StreetGraph graph, Flat from, Flat to, TrackStyle style) => Sample(graph, [from, to], style);

    private static List<GpsFix> Sample(StreetGraph graph, List<Flat> path, TrackStyle style)
    {
        Random random = new(style.Seed);
        double Gaussian() => Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
        List<GpsFix> fixes = [];
        double step = style.EverySeconds * style.MetresPerSecond, next = 0, walked = 0;
        Flat drift = new(0, 0);
        for (int i = 0; i < path.Count - 1; i++)
        {
            double length = Flat.Distance(path[i], path[i + 1]);
            while (next <= walked + length)
            {
                Flat at = path[i] + (path[i + 1] - path[i]) * (length <= 0 ? 0 : (next - walked) / length);
                drift = new Flat(Math.Clamp(drift.X + Gaussian() * style.DriftMetres * 0.2, -style.DriftMetres, style.DriftMetres),
                    Math.Clamp(drift.Y + Gaussian() * style.DriftMetres * 0.2, -style.DriftMetres, style.DriftMetres));
                Flat noisy = at + drift + new Flat(Gaussian(), Gaussian()) * style.NoiseMetres;
                fixes.Add(new GpsFix(Began.AddSeconds(next / style.MetresPerSecond), graph.Projection.ToGeo(noisy), Math.Max(5, style.NoiseMetres * 1.5)));
                next += step;
            }
            walked += length;
        }
        return fixes;
    }

    public static HashSet<int> Homes(StreetGraph graph, IEnumerable<SideStretch> sides)
    {
        HashSet<int> homes = [];
        foreach (SideStretch side in sides)
        {
            LetterboxRange range = graph.Between(side.Street, side.Side, side.From, side.To);
            for (int b = range.Start; b < range.End; b++)
            {
                if (graph.LetterboxKindOf(b).IsHome())
                    homes.Add(b);
            }
        }
        return homes;
    }
}
