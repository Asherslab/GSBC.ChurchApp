using GSBC.ChurchApp.Features.Prayer.Contracts;

namespace GSBC.ChurchApp.Features.Prayer.Client;

public enum Sending
{
    Sent,
    Waiting,
    CouldNotSend,
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
