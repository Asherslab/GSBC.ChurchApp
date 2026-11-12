using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal static class PrayerRules
{
    public const int LongestText = 2000;
    public const int LongestNote = 2000;
    public const int LongestAnsweredNote = 500;
    public const int LongestName = 60;
    public const int LongestContact = 120;
    public const int RequestsPerDay = 5;
    public const int PrayersPerDay = 300;

    public static readonly TimeSpan Day = TimeSpan.FromDays(1);
    public static readonly TimeSpan OnTheWallFor = TimeSpan.FromDays(30);
    public static readonly TimeSpan LongestOnTheWall = TimeSpan.FromDays(365);
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(365);

    public static string Clip(string value, int longest)
    {
        string trimmed = value.Trim();
        return trimmed.Length <= longest ? trimmed : trimmed[..longest];
    }

    public static bool MaySendMore(int sentInTheLastDay) => sentInTheLastDay < RequestsPerDay;

    public static bool MayPrayMore(int prayedInTheLastDay) => prayedInTheLastDay < PrayersPerDay;

    public static string ShortName(string name)
    {
        string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length switch
        {
            0 => "",
            1 => words[0],
            _ => $"{words[0]} {char.ToUpperInvariant(words[^1][0])}.",
        };
    }

    public static bool IsOnTheWall(RequestRecord request, DateTime now) =>
        request.Wall == WallState.Shown && request.WallUntil > now;

    public static bool HasLeftTheWall(RequestRecord request, DateTime now) =>
        request.Wall == WallState.Shown && request.WallUntil <= now;

    public static bool IsTheirs(RequestRecord request, Guid install, Guid? user) =>
        request.UserId is { } owner ? owner == user : request.InstallId == install;

    public static bool MayModerate(RequestRecord request) => request.Wall != WallState.NotAsked;

    public static bool IsForApprovers(RequestRecord request) => request.Wall is WallState.Waiting or WallState.Shown;

    public static bool IsForThePastoralTeamsPhones(RequestRecord request) => request.Care == CareState.New || IsForApprovers(request);

    public static bool MayGoOnTheWall(RequestRecord request) => MayModerate(request) && request.Wall != WallState.TakenDown;

    public static bool ShowsNameOnTheWall(RequestRecord request, bool staffShowName) => staffShowName && request.ShowName && request.Name.Length > 0;

    public static bool IsDueForDeletion(RequestRecord request, DateTime now) => request.ChangedAt <= now - KeptFor;

    public static DateTime? ClampedUntil(DateTime until, DateTime now) =>
        until <= now || until > now + LongestOnTheWall ? null : until;

    public static WallPost Post(RequestRecord request) => new()
    {
        Id = request.Id.ToString(),
        Text = request.WallText,
        Name = request.WallShowsName ? ShortName(request.Name) : "",
        ShownAt = request.WallShownAt ?? request.SentAt,
        Until = request.WallUntil ?? request.SentAt,
        Prayed = request.Prayed,
        Answered = request.Care == CareState.Answered,
        AnsweredNote = request.Care == CareState.Answered ? request.AnsweredNote : "",
    };

    public static YourRequest Yours(RequestRecord request) => new()
    {
        Id = request.Id.ToString(),
        Text = request.Text,
        SentAt = request.SentAt,
        Wall = request.Wall,
        WallUntil = request.Wall == WallState.Shown ? request.WallUntil : null,
        Prayed = request.Prayed,
        Answered = request.Care == CareState.Answered,
        AnsweredNote = request.AnsweredNote,
        WallName = request.Wall == WallState.Shown && request.WallShowsName ? ShortName(request.Name) : "",
        WallText = request.Wall == WallState.Shown ? request.WallText : "",
    };

    public static WallCase ForApprovers(RequestRecord request) => new()
    {
        Id = request.Id.ToString(),
        Text = request.Text,
        SentAt = request.SentAt,
        Wall = request.Wall,
        Minor = request.Minor,
        ShortName = request.ShowName ? ShortName(request.Name) : "",
        WallText = request.WallText,
        WallShowsName = request.WallShowsName,
        WallUntil = request.Wall == WallState.Shown ? request.WallUntil : null,
        Prayed = request.Prayed,
        ChangedAt = request.ChangedAt,
    };

    public static CareCase ForThePastoralTeam(RequestRecord request, IEnumerable<NoteRecord> notes, IReadOnlyDictionary<Guid, string> names) => new()
    {
        Id = request.Id.ToString(),
        Text = request.Text,
        From = From(request),
        SentAt = request.SentAt,
        Care = request.Care,
        Wall = request.Wall,
        Minor = request.Minor,
        SignedIn = request.UserId is not null,
        WantsContact = request.WantsContact,
        Contact = request.Contact,
        AnsweredNote = request.AnsweredNote,
        Notes = notes.Select(note => new CareNote { Id = note.Id.ToString(), Text = note.Text, Author = names.GetValueOrDefault(note.AuthorId, "Someone"), At = note.At })
            .ToList(),
        ChangedAt = request.ChangedAt,
    };

    public static string From(RequestRecord request) => request.Name.Length > 0 ? request.Name : "No name";

    public static InboxLine Line(RequestRecord request) => new()
    {
        Id = request.Id.ToString(),
        Text = request.Text,
        From = From(request),
        SentAt = request.SentAt,
        Care = request.Care,
        Wall = request.Wall,
        Minor = request.Minor,
        WantsContact = request.WantsContact,
        ChangedAt = request.ChangedAt,
    };
}
