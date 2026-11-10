namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistryKinds
{
    public const string Ministry = "ministry";
    public const string LifeGroup = "life-group";
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

    public static string Words(string audience) => audience switch
    {
        Attendees => "For attendees and their parents",
        Parents => "For parents",
        Team => "For the team",
        _ => "For everyone",
    };
}

public static class Roles
{
    public const string Leader = "leader";
    public const string Admin = "admin";
    public const string Team = "team";
    public const string Attendee = "attendee";

    public static bool IsValid(string value) => value is Leader or Admin or Team or Attendee;

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
