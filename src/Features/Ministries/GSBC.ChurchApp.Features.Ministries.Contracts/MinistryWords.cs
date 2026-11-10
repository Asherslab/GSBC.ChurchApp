namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistryNames
{
    public static string Possessive(string name) => name.EndsWith('s') ? $"{name}'" : $"{name}'s";
}

public sealed record MinistryPart(string Key, string Name, string Does, PartFromBelow? FromBelow = null);

public sealed record PartFromBelow(string Title, string Does);

public static class MinistryParts
{
    public const string News = "news";
    public const string Calendar = "calendar";
    public const string Files = "files";

    public static readonly IReadOnlyList<MinistryPart> All =
    [
        new(News, "News", "Posts from its leaders, for everyone or for its people",
            new PartFromBelow("Show news from the ministries under it",
                "Their posts show here too, each saying which ministry it's from. People see only the posts they'd see in that ministry.")),
        new(Calendar, "Calendar", "Its calendars, with their events in people's week and calendar links",
            new PartFromBelow("Show the calendars of the ministries under it",
                "Their events show on this calendar too, each saying which ministry it's from. People see only what they'd see in that ministry.")),
        new(Files, "Files", "Documents and links for its people"),
    ];

    public static MinistryPart Of(string key) => All.Single(part => part.Key == key);

    public static bool IsOn(IEnumerable<string> partsOff, string part) => !partsOff.Contains(part);

    public static bool HasTab(MinistryPage page, string part) =>
        IsOn(page.Ministry.PartsOff, part) && part switch
        {
            News => page.Posts.Count > 0 || page.You.MayPostFor.Count > 0,
            Calendar => page.Calendars.Count > 0 || page.You.MayEditSettings,
            Files => page.Files.Count > 0 || page.You.MayPostFor.Count > 0,
            _ => false,
        };

    public static List<string> Valid(IEnumerable<string> partsOff) => All.Select(part => part.Key).Where(partsOff.Contains).ToList();

    public static List<string> ValidFromBelow(IEnumerable<string> partsFromBelow) =>
        All.Where(part => part.FromBelow is not null).Select(part => part.Key).Where(partsFromBelow.Contains).ToList();
}

public static class Joining
{
    public const string Open = "open";
    public const string Ask = "ask";
    public const string Invite = "invite";
    public const string None = "none";

    public static bool IsValid(string value, bool noneAllowed) => value is Open or Ask or Invite || (noneAllowed && value == None);
}

public static class Places
{
    public const string Team = "team";
    public const string Attending = "attend";

    public static bool IsValid(string value) => value is Team or Attending;
}

public static class Audiences
{
    public const string Everyone = "everyone";
    public const string Attendees = "attendees";
    public const string Parents = "parents";
    public const string Team = "team";

    public static readonly IReadOnlyList<string> All = [Everyone, Attendees, Parents, Team];

    public static bool IsValid(string value) => All.Contains(value);

    public static string? NodeOf(string audience) => audience switch
    {
        Attendees => MinistriesPermissions.AttendeesViewNode,
        Parents => MinistriesPermissions.ParentsViewNode,
        Team => MinistriesPermissions.TeamViewNode,
        _ => null,
    };
}

public static class Roles
{
    public const string Leader = "leader";
    public const string Admin = "admin";
    public const string Team = "team";
    public const string Attendee = "attendee";

    public static bool IsValid(string value) => value is Leader or Admin or Team or Attendee;

    public static string? KeptAfterTakingOff(string role) => role is Leader or Admin ? Team : null;

    public static string KeyOf(string role) => role switch
    {
        Leader => MinistriesPermissions.LeaderRole,
        Admin => MinistriesPermissions.AdminRole,
        Team => MinistriesPermissions.TeamRole,
        _ => MinistriesPermissions.AttendeeRole,
    };
}

public static class FileKinds
{
    public const string Document = "document";
    public const string Link = "link";
}
