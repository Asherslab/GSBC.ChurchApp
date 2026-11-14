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
    public const double DoneShare = 0.95;

    public static BoardState State(int claims, double done) =>
        done >= DoneShare ? BoardState.Done
        : done > 0 ? BoardState.Walking
        : claims > 0 ? BoardState.Claimed
        : BoardState.Open;

    public static int Percent(int done, int of) => of == 0 ? 0 : (int)Math.Round(100.0 * done / of);

    public static string Words(BoardState state, double done) => state switch
    {
        BoardState.Done => "Done",
        BoardState.Walking => $"{(int)Math.Round(done * 100)}% walked",
        BoardState.Claimed => "Not started",
        _ => "Open",
    };
}
