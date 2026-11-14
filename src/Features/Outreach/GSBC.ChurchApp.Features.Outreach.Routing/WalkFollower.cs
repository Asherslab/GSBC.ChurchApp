namespace GSBC.ChurchApp.Features.Outreach.Routing;

public readonly record struct GpsFix(DateTimeOffset At, GeoPoint Point, double Accuracy);

public sealed record FollowerSettings
{
    public static FollowerSettings Defaults { get; } = new();

    public double WorstAccuracyMetres { get; init; } = 40;

    public double SampleSpacingMetres { get; init; } = 5;

    public double StraightGapMetres { get; init; } = 50;

    public double LongestGapMetres { get; init; } = 600;

    public double FastestMetresPerSecond { get; init; } = 2.5;

    public double CrossingMetres { get; init; } = 14;

    public double SideEvidenceMetres { get; init; } = 4;

    public double EndToleranceMetres { get; init; } = 6;

    public double TurnBackMetres { get; init; } = 12;
}

public sealed class WalkFollower(StreetGraph graph, FollowerSettings? settings = null)
{
    private const double MinSigmaMetres = 8;
    private const double SearchFactor = 2.5;
    private const double MinSearchMetres = 20;
    private const double MaxSearchMetres = 60;
    private const double StayBonus = 1.5;
    private const double JumpPenalty = 4;
    private const double HeadingPenalty = 2.5;
    private const double HeadingWindowMetres = 12;
    private const double CornerMetres = 10;
    private const double DetourFactor = 1.6;
    private const double DetourSlackMetres = 60;

    private readonly FollowerSettings _settings = settings ?? FollowerSettings.Defaults;
    private readonly List<GpsFix> _fixes = [];
    private readonly List<SideStretch> _skipped = [];
    private IReadOnlyList<SideStretch>? _covered;
    private WalkNetwork? _network;
    private double _walked;

    private readonly record struct Sample(Flat Point, double Accuracy, bool Inferred, int Street = -1, double Along = 0, double Offset = 0);

    public IReadOnlyList<GpsFix> Fixes => _fixes;

    public double WalkedMetres
    {
        get
        {
            _ = Covered;
            return _walked;
        }
    }

    public void Add(GpsFix fix)
    {
        if (fix.Accuracy > _settings.WorstAccuracyMetres || _fixes.Count > 0 && fix.At < _fixes[^1].At)
            return;
        _fixes.Add(fix);
        _covered = null;
    }

    public void Skip(SideStretch stretch)
    {
        _skipped.Add(stretch);
        _covered = null;
    }

    public StreetPosition? Where()
    {
        if (_fixes.Count == 0)
            return null;
        return graph.Index.Nearest(graph.Projection.ToFlat(_fixes[^1].Point), Math.Clamp(_fixes[^1].Accuracy * SearchFactor, MinSearchMetres, MaxSearchMetres), s => !graph.Street(s).Kind.IsMainRoad());
    }

    public IReadOnlyList<SideStretch> Covered => _covered ??= Follow();

    public double ShareDone(IEnumerable<SideStretch> sides)
    {
        int total = 0, done = 0;
        foreach (SideStretch side in sides)
        {
            LetterboxRange range = graph.Between(side.Street, side.Side, side.From, side.To);
            for (int b = range.Start; b < range.End; b++)
            {
                if (!graph.LetterboxKindOf(b).IsHome())
                    continue;
                total++;
                double along = graph.LetterboxAlong(b);
                done += Covered.Any(c => c.Street == side.Street && c.Side == side.Side && along >= c.From && along <= c.To) ? 1 : 0;
            }
        }
        return total == 0 ? 0 : done / (double)total;
    }

    private IReadOnlyList<SideStretch> Follow()
    {
        List<Sample> samples = Densify();
        _walked = 0;
        for (int i = 1; i < samples.Count; i++)
            _walked += Flat.Distance(samples[i - 1].Point, samples[i].Point);
        List<Sample> matched = Match(samples);
        List<SideStretch> found = [];
        int start = 0;
        for (int i = 1; i <= matched.Count; i++)
        {
            if (i < matched.Count && matched[i].Street == matched[start].Street)
                continue;
            if (matched[start].Street >= 0)
                found.AddRange(Runs(matched.GetRange(start, i - start)));
            start = i;
        }
        return Subtract(Merge(found.Where(s => graph.Street(s.Street).CanDeliver)), _skipped);
    }

    private List<Sample> Densify()
    {
        List<Sample> samples = [];
        for (int i = 0; i < _fixes.Count; i++)
        {
            Flat point = graph.Projection.ToFlat(_fixes[i].Point);
            if (i > 0)
            {
                Flat before = graph.Projection.ToFlat(_fixes[i - 1].Point);
                double gap = Flat.Distance(before, point);
                double seconds = Math.Max((_fixes[i].At - _fixes[i - 1].At).TotalSeconds, 1);
                if (gap > _settings.StraightGapMetres)
                {
                    samples.Add(new Sample(default, 0, true, -2));
                    if (gap <= _settings.LongestGapMetres && Bridge(before, point, seconds) is { } bridge)
                    {
                        samples.RemoveAt(samples.Count - 1);
                        samples.AddRange(bridge);
                    }
                }
                else
                {
                    int steps = (int)(gap / _settings.SampleSpacingMetres);
                    for (int k = 1; k < steps; k++)
                        samples.Add(new Sample(before + (point - before) * (k / (double)steps), _fixes[i].Accuracy, true));
                }
            }
            samples.Add(new Sample(point, _fixes[i].Accuracy, false));
        }
        return samples;
    }

    private List<Sample>? Bridge(Flat from, Flat to, double seconds)
    {
        if (graph.Index.Nearest(from, MaxSearchMetres) is not { } a || graph.Index.Nearest(to, MaxSearchMetres) is not { } b)
            return null;
        List<(int Street, double From, double To)> route = Route(a, b);
        double metres = route.Sum(r => Math.Abs(r.To - r.From) * graph.Street(r.Street).Length);
        double straight = Flat.Distance(from, to);
        if (route.Count == 0 && a.Street != b.Street || metres > straight * DetourFactor + DetourSlackMetres || metres > seconds * _settings.FastestMetresPerSecond + DetourSlackMetres)
            return null;
        List<Sample> bridge = [];
        foreach ((int street, double start, double end) in route)
        {
            double length = Math.Abs(end - start) * graph.Street(street).Length;
            int steps = Math.Max(1, (int)(length / _settings.SampleSpacingMetres));
            for (int k = 0; k <= steps; k++)
            {
                double along = start + (end - start) * k / steps;
                bridge.Add(new Sample(graph.PointAt(street, along), MinSigmaMetres, true, street, along));
            }
        }
        return bridge;
    }

    private List<(int Street, double From, double To)> Route(StreetPosition a, StreetPosition b)
    {
        if (a.Street == b.Street)
            return [(a.Street, a.Along, b.Along)];
        WalkNetwork network = _network ??= new WalkNetwork(graph);
        Paths paths = new(network);
        int from = network.NodeAt(a.Street, a.Along), to = network.NodeAt(b.Street, b.Along);
        List<int> path = paths.Shortest([from], node => node == to, _settings.LongestGapMetres * DetourFactor, network.CutsOf([network.PieceOf(a.Street, a.Along, 1), network.PieceOf(b.Street, b.Along, 1)]));
        List<(int, double, double)> route = [];
        int at = from;
        foreach (int piece in path)
        {
            Piece p = network[piece];
            bool forward = p.U == at;
            route.Add((p.Street, forward ? p.From : p.To, forward ? p.To : p.From));
            at = network.Other(piece, at);
        }
        return route;
    }

    private List<Sample> Match(List<Sample> samples)
    {
        List<Sample> matched = new(samples.Count);
        int previous = -1;
        for (int i = 0; i < samples.Count; i++)
        {
            Sample sample = samples[i];
            if (sample.Street == -2)
            {
                matched.Add(sample);
                previous = -1;
                continue;
            }
            if (sample.Street >= 0)
            {
                matched.Add(sample);
                previous = sample.Street;
                continue;
            }
            double sigma = Math.Max(sample.Accuracy, MinSigmaMetres);
            double radius = Math.Clamp(sigma * SearchFactor, MinSearchMetres, MaxSearchMetres);
            double? heading = Heading(samples, i);
            double best = double.MaxValue;
            StreetPosition chosen = default;
            foreach (int street in graph.Index.StreetsNear(sample.Point, radius))
            {
                StreetPosition position = graph.Project(street, sample.Point);
                if (position.Distance > radius)
                    continue;
                double cost = position.Distance * position.Distance / (2 * sigma * sigma);
                if (heading is { } walking && position.Along is > 0.001 and < 0.999)
                {
                    double sine = Math.Sin(walking - graph.HeadingAt(street, position.Along));
                    cost += HeadingPenalty * sine * sine;
                }
                if (previous >= 0 && street != previous)
                    cost += Joined(previous, street) ? StayBonus : JumpPenalty;
                if (cost < best)
                    (best, chosen) = (cost, position);
            }
            matched.Add(best < double.MaxValue ? sample with { Street = chosen.Street, Along = chosen.Along, Offset = chosen.Offset } : sample with { Street = -1 });
            previous = best < double.MaxValue ? chosen.Street : previous;
        }
        return matched;
    }

    private double? Heading(List<Sample> samples, int i)
    {
        int back = i, ahead = i;
        while (back > 0 && samples[back - 1].Street != -2 && Flat.Distance(samples[back - 1].Point, samples[i].Point) < HeadingWindowMetres)
            back--;
        while (ahead < samples.Count - 1 && samples[ahead + 1].Street != -2 && Flat.Distance(samples[ahead + 1].Point, samples[i].Point) < HeadingWindowMetres)
            ahead++;
        Flat move = samples[ahead].Point - samples[back].Point;
        return move.Length < HeadingWindowMetres / 2 ? null : move.Bearing;
    }

    private bool Joined(int a, int b)
    {
        ref readonly StreetInfo x = ref graph.Street(a);
        ref readonly StreetInfo y = ref graph.Street(b);
        return x.U == y.U || x.U == y.V || x.V == y.U || x.V == y.V;
    }

    private IEnumerable<SideStretch> Runs(List<Sample> run)
    {
        double length = graph.Street(run[0].Street).Length;
        int start = 0, direction = 0, extremeAt = 0;
        double extreme = run[0].Along;
        for (int i = 1; i < run.Count; i++)
        {
            double along = run[i].Along;
            if (direction == 0)
            {
                double moved = (along - run[start].Along) * length;
                if (Math.Abs(moved) > _settings.TurnBackMetres)
                    (direction, extreme, extremeAt) = (Math.Sign(moved), along, i);
                continue;
            }
            if (direction > 0 ? along >= extreme : along <= extreme)
            {
                (extreme, extremeAt) = (along, i);
                continue;
            }
            if (Math.Abs(along - extreme) * length <= _settings.TurnBackMetres)
                continue;
            if (Stretch(run.GetRange(start, extremeAt - start + 1), direction) is { } stretch)
                yield return stretch;
            (start, direction, extreme, extremeAt) = (extremeAt, -direction, along, i);
        }
        if (Stretch(run.GetRange(start, run.Count - start), direction) is { } last)
            yield return last;
    }

    private SideStretch? Stretch(List<Sample> part, int direction)
    {
        int street = part[0].Street;
        double length = graph.Street(street).Length;
        double low = part.Min(s => s.Along), high = part.Max(s => s.Along);
        if ((high - low) * length < Math.Min(_settings.CrossingMetres, length * 0.5))
            return null;
        double cornerShare = length <= 0 ? 0 : CornerMetres / length;
        List<double> offsets = part.Where(s => !s.Inferred && s.Along > cornerShare && s.Along < 1 - cornerShare).Select(s => s.Offset).ToList();
        double offset = offsets.Count >= 3 ? offsets.Average() : 0;
        Side side = Math.Abs(offset) >= _settings.SideEvidenceMetres ? offset > 0 ? Side.Left : Side.Right
            : direction >= 0 ? Side.Left : Side.Right;
        double tolerance = length <= 0 ? 0 : _settings.EndToleranceMetres / length;
        double from = low - tolerance, to = high + tolerance;
        from = from * length <= RouteDraft.SnapToCornerMetres ? 0 : from;
        to = (1 - to) * length <= RouteDraft.SnapToCornerMetres ? 1 : to;
        return new SideStretch(street, side, Math.Clamp(from, 0, 1), Math.Clamp(to, 0, 1));
    }

    private static List<SideStretch> Merge(IEnumerable<SideStretch> stretches)
    {
        List<SideStretch> merged = [];
        foreach (IGrouping<(int, Side), SideStretch> group in stretches.GroupBy(s => (s.Street, s.Side)).OrderBy(g => g.Key))
        {
            SideStretch? pending = null;
            foreach (SideStretch stretch in group.OrderBy(s => s.From))
            {
                if (pending is { } current && stretch.From <= current.To)
                {
                    pending = current with { To = Math.Max(current.To, stretch.To) };
                    continue;
                }
                if (pending is { } done)
                    merged.Add(done);
                pending = stretch;
            }
            if (pending is { } last)
                merged.Add(last);
        }
        return merged;
    }

    private static List<SideStretch> Subtract(List<SideStretch> stretches, List<SideStretch> removed)
    {
        foreach (SideStretch cut in removed)
        {
            stretches = stretches.SelectMany(s => s.Street != cut.Street || s.Side != cut.Side || s.To <= cut.From || s.From >= cut.To
                ? [s]
                : new[] { s with { To = cut.From }, s with { From = cut.To } }.Where(p => p.To > p.From)).ToList();
        }
        return stretches;
    }
}
