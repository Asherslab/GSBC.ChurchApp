namespace GSBC.ChurchApp.Features.Outreach.Client;

public enum BoardState
{
    Open,
    Claimed,
    Walking,
    Done,
}

public static class DayBoards
{
    private const double DoneShare = 0.95;

    public static BoardState State(int claims, double done) =>
        done >= DoneShare ? BoardState.Done
        : done > 0 ? BoardState.Walking
        : claims > 0 ? BoardState.Claimed
        : BoardState.Open;

    private const int GapsNamed = 4;

    public static string Gaps(IReadOnlyList<int> open) => open.Count switch
    {
        0 => "",
        1 => $"Someone just arrived? Route {open[0]} has no one yet.",
        <= GapsNamed => $"Someone just arrived? Routes {string.Join(", ", open.Take(open.Count - 1))} and {open[^1]} have no one yet.",
        _ => $"Someone just arrived? {open.Count} routes have no one yet, such as {string.Join(", ", open.Take(GapsNamed - 1))} and {open[GapsNamed - 1]}.",
    };

    public static int Percent(int done, int of) => of == 0 ? 0 : (int)Math.Round(100.0 * done / of);

    public static string Words(BoardState state, double done) => state switch
    {
        BoardState.Done => "Done",
        BoardState.Walking => $"{(int)Math.Round(done * 100)}% walked",
        BoardState.Claimed => "Not started",
        _ => "Open",
    };
}
