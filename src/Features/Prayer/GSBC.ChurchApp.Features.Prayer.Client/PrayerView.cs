using GSBC.ChurchApp.Features.Prayer.Contracts;

namespace GSBC.ChurchApp.Features.Prayer.Client;

public enum Sending
{
    Sent,
    Waiting,
    CouldNotSend,
}

public enum WallAction
{
    Show,
    KeepPrivate,
}

public sealed record YourLine(
    string Id,
    string Text,
    DateTime SentAt,
    Sending Sending,
    string? Problem,
    WallState Wall,
    DateTime? WallUntil,
    int Prayed,
    bool Answered,
    string AnsweredNote,
    string WallName,
    bool AskedForWall,
    bool Known);

public sealed record WallLine(WallPost Post, bool PrayedHere, bool Yours);

public sealed record PrayerView(IReadOnlyList<YourLine> Yours, IReadOnlyList<WallLine> Wall)
{
    public static readonly PrayerView Empty = new([], []);

    public YourLine? Find(string id) => Yours.FirstOrDefault(line => line.Id == id);
}

public sealed record PrayerAsk(string Text, bool ForWall, bool ShowName, string Name, bool WantsContact, string Contact);

public sealed record StaffPending(string What, bool Failed, string? Problem);

public sealed record StaffCase(string Id, WallCase? Wall, CareCase? Care, StaffPending? Pending)
{
    public string Text => Wall?.Text ?? Care?.Text ?? "";

    public DateTime SentAt => Wall?.SentAt ?? Care?.SentAt ?? default;

    public bool Minor => Wall?.Minor ?? Care?.Minor ?? false;

    public bool AsksForTheWall => Wall?.Wall == WallState.Waiting;

    public bool OnTheWall => Wall?.Wall == WallState.Shown;

    public bool IsNew => Care?.Care == CareState.New;
}

public sealed record StaffView(IReadOnlyList<StaffCase> Cases, bool MayCare)
{
    public static readonly StaffView Empty = new([], false);

    public IReadOnlyList<StaffCase> ForTheWall => Cases.Where(item => item.AsksForTheWall).ToList();

    public IReadOnlyList<StaffCase> New => Cases.Where(item => item.IsNew && !item.AsksForTheWall).ToList();

    public IReadOnlyList<StaffCase> OnTheWall => Cases.Where(item => item.OnTheWall && !item.IsNew).ToList();

    public int Waiting => Cases.Count(item => (item.AsksForTheWall || item.IsNew) && item.Pending is null);

    public StaffCase? Find(string id) => Cases.FirstOrDefault(item => item.Id == id);
}
