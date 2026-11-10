using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal static class MinistryRules
{
    public const int LongestName = 60;
    public const int LongestSummary = 140;
    public const int LongestText = 4000;
    public const int LongestTitle = 120;

    public static bool MaySee(string audience, Func<string, bool> holdsHere, Func<string, bool> holdsBelow) => audience switch
    {
        Audiences.Everyone => true,
        Audiences.Team => holdsHere(MinistriesPermissions.TeamViewNode) || holdsBelow(MinistriesPermissions.TeamViewNode),
        _ when Audiences.NodeOf(audience) is { } node => holdsHere(node),
        _ => false,
    };

    public static JoinDecision Decide(string joining, string place) => joining switch
    {
        Joining.Open => JoinDecision.Join,
        Joining.Ask => JoinDecision.Ask,
        Joining.Invite => JoinDecision.Refuse(place == Places.Team
            ? "Leaders invite people to this team. Ask one of them."
            : "Leaders add people to this. Ask one of them."),
        _ => JoinDecision.Refuse("This ministry doesn't take attendees."),
    };

    public static string? WhyNotPost(string title, string body, string audience, IReadOnlyCollection<string> mayPostFor)
    {
        if (title.Trim().Length is 0 or > LongestTitle)
            return $"Give it a title of up to {LongestTitle} characters.";
        if (body.Trim().Length > LongestText)
            return $"Keep it to {LongestText} characters.";
        if (!Audiences.IsValid(audience))
            return "Choose who it's for.";
        return mayPostFor.Contains(audience) ? null : "This ministry can't post to that audience.";
    }

    public static IReadOnlyList<string> PostableAudiences(string attendJoin) =>
        attendJoin == Joining.None ? [Audiences.Everyone, Audiences.Team] : Audiences.All;

    public static string? WhyNotDetails(MinistryDetails details, bool topLevel)
    {
        if (details.Name.Trim().Length is 0 or > LongestName)
            return $"Give it a name of up to {LongestName} characters.";
        if (details.Summary.Trim().Length > LongestSummary)
            return $"Keep the line about it to {LongestSummary} characters.";
        if (details.About.Trim().Length > LongestText)
            return $"Keep the about to {LongestText} characters.";
        if (!Joining.IsValid(details.TeamJoin, noneAllowed: false))
            return "Choose how people join the team.";
        if (!Joining.IsValid(details.AttendJoin, noneAllowed: true))
            return "Choose how people come along.";
        return details.Kind is MinistryKinds.Ministry or MinistryKinds.LifeGroup ? null : "That isn't a kind of ministry.";
    }

    public static string PartName(string part) => part switch
    {
        MinistryParts.News => "News",
        MinistryParts.Calendar => "Calendar",
        _ => "Files",
    };

    public static string Yours(IReadOnlyCollection<string> roles, IReadOnlyCollection<string> childrenAttending, bool leadsFromAbove) =>
        roles.Contains(Roles.Leader) || leadsFromAbove ? "You lead it"
        : roles.Contains(Roles.Admin) ? "You look after it"
        : roles.Contains(Roles.Team) ? "On the team"
        : childrenAttending.Count > 0 ? $"{Names(childrenAttending)} {(childrenAttending.Count == 1 ? "attends" : "attend")}"
        : roles.Contains(Roles.Attendee) ? "You come along"
        : "";

    public static string Names(IReadOnlyCollection<string> names) =>
        names.Count <= 1 ? names.FirstOrDefault() ?? "" : $"{string.Join(", ", names.Take(names.Count - 1))} and {names.Last()}";
}

internal sealed record JoinDecision(bool Joins, bool Asks, string? Refusal)
{
    public static readonly JoinDecision Join = new(true, false, null);
    public static readonly JoinDecision Ask = new(false, true, null);

    public static JoinDecision Refuse(string why) => new(false, false, why);
}
