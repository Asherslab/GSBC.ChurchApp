namespace GSBC.ChurchApp.Features.Outreach.Routing;

public sealed record WalkBudget(double? Minutes = null, double? Kilometres = null)
{
    public bool ByTime => Minutes is not null;

    public double Asked => Minutes is { } minutes ? minutes * 60 : (Kilometres ?? 2) * 1000;

    public double Reached(WalkPlan plan) => ByTime ? plan.Time.TotalSeconds : plan.Metres;

    public double WalkingRadius(RouteSettings settings) =>
        (Minutes is { } minutes ? minutes * 60 * settings.WalkingMetresPerSecond : (Kilometres ?? 2) * 1000) / 2;

    public WalkBudget Times(double factor) => ByTime ? new WalkBudget(Minutes * factor) : new WalkBudget(Kilometres: (Kilometres ?? 2) * factor);
}

public readonly record struct Arc(int Piece, bool Forward)
{
    public Side Side => Forward ? Side.Left : Side.Right;
}

public sealed record WalkLeg(string Street, Side Side, IReadOnlyList<Arc> Arcs, int Letterboxes, double Metres, bool Part)
{
    public const string Unnamed = "Unnamed street";

    public bool Delivers => Letterboxes > 0;

    public string Title => Part ? $"{Street} (part)" : Street;
}

public sealed record WalkHop(int From, IReadOnlyList<int> Pieces, int Target, double Metres);

public sealed record WalkPlan(
    int Start,
    IReadOnlyList<Arc> Arcs,
    IReadOnlyList<WalkLeg> Legs,
    double Metres,
    int Letterboxes,
    TimeSpan Time,
    double ConnectorMetres,
    double LongestConnectorMetres,
    double ApproachMetres,
    int Bundles,
    int BundlesToTake,
    bool NeedsReview)
{
    public WalkHop? Hop { get; init; }

    public bool IsEmpty => Letterboxes == 0;

    public IEnumerable<int> Pieces => Arcs.Select(a => a.Piece).Distinct();

    public IEnumerable<WalkLeg> Deliveries => Legs.Where(l => l.Delivers);

    public IReadOnlyList<string> Streets => Deliveries.Select(l => l.Title).Where(t => t != WalkLeg.Unnamed).Distinct().ToList();
}
