namespace GSBC.ChurchApp.Features.Outreach.Routing;

public readonly record struct Stretch(int Street, double From, double To)
{
    public const int Thousandths = 1000;

    public static Stretch Whole(int street) => new(street, 0, 1);

    public bool IsWhole => From <= 0 && To >= 1;

    public double Length(StreetGraph graph) => (To - From) * graph.Street(Street).Length;

    public bool Contains(double along) => along >= From && (along < To || To >= 1);

    public int FromThousandths => (int)Math.Round(From * Thousandths);

    public int ToThousandths => (int)Math.Round(To * Thousandths);

    public static Stretch FromThousandthsOf(int street, int from, int to) =>
        new(street, Math.Clamp(Math.Min(from, to), 0, Thousandths) / (double)Thousandths, Math.Clamp(Math.Max(from, to), 0, Thousandths) / (double)Thousandths);
}

public readonly record struct SideStretch(int Street, Side Side, double From, double To)
{
    public Stretch Stretch => new(Street, From, To);

    public static SideStretch Whole(int street, Side side) => new(street, side, 0, 1);
}

public readonly record struct StretchKey(StreetKey Street, Side Side, int From, int To)
{
    public static StretchKey Of(StreetGraph graph, SideStretch stretch) =>
        new(graph.KeyOf(stretch.Street), stretch.Side, (int)Math.Round(stretch.From * Stretch.Thousandths), (int)Math.Round(stretch.To * Stretch.Thousandths));

    public SideStretch? In(StreetGraph graph) =>
        graph.StreetOf(Street) is { } street ? new SideStretch(street, Side, From / (double)Stretch.Thousandths, To / (double)Stretch.Thousandths) : null;
}
