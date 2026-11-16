using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal static class PrayerRules
{
    public const int LongestText = 2000;
    public const int LongestNote = 2000;
    public const int LongestAnsweredNote = 500;
    public const int LongestName = 60;
    public const int LongestChurchName = 40;
    public const string ChurchNameToStart = "GSBC";
    public const int RequestsPerDay = 5;
    public const int PrayersPerDay = 300;

    public static readonly TimeSpan Day = TimeSpan.FromDays(1);
    public static readonly TimeSpan OnTheWallFor = TimeSpan.FromDays(30);
    public static readonly TimeSpan LongestOnTheWall = TimeSpan.FromDays(365);
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(365);
    private static readonly TimeSpan ThanksShownFor = TimeSpan.FromDays(3);

    public static string Clip(string value, int longest)
    {
        string trimmed = value.Trim();
        return trimmed.Length <= longest ? trimmed : trimmed[..longest];
    }

    public static bool MaySendMore(int sentInTheLastDay) => sentInTheLastDay < RequestsPerDay;

    public static bool MayPrayMore(int prayedInTheLastDay) => prayedInTheLastDay < PrayersPerDay;

    public static string ShortName(string name) => PrayerNames.Short(name);

    public static string WallName(RequestRecord request) => request.FromChurch ? request.Name : ShortName(request.Name);

    private static bool IsShared(RequestRecord request) => request.FromChurch || request.ForSomeoneElse;

    public static bool MayDelete(RequestRecord request, bool mayRead) => mayRead || IsShared(request);

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

    public static DateTime UntilWithThanks(DateTime? until, DateTime now) =>
        until is { } day && day <= now + ThanksShownFor ? day : now + ThanksShownFor;

    public static DateTime? ClampedUntil(DateTime until, DateTime now) =>
        until <= now || until > now + LongestOnTheWall ? null : until;

    public static WallPost Post(RequestRecord request) => new()
    {
        Id = request.Id.ToString(),
        Text = request.WallText,
        Name = request.WallShowsName ? WallName(request) : "",
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
        WallName = request.Wall == WallState.Shown && request.WallShowsName ? WallName(request) : "",
        WallText = request.Wall == WallState.Shown ? request.WallText : "",
    };

    public static WallCase ForApprovers(RequestRecord request) => new()
    {
        Id = request.Id.ToString(),
        Text = request.Text,
        SentAt = request.SentAt,
        Wall = request.Wall,
        Minor = request.Minor,
        ShortName = request.ShowName ? WallName(request) : "",
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
        AnsweredNote = request.AnsweredNote,
        Notes = notes.Select(note => new CareNote { Id = note.Id.ToString(), Text = note.Text, Author = names.GetValueOrDefault(note.AuthorId, "Someone"), At = note.At })
            .ToList(),
        ChangedAt = request.ChangedAt,
        FromChurch = request.FromChurch,
        ForSomeoneElse = request.ForSomeoneElse,
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
        ChangedAt = request.ChangedAt,
    };
}
