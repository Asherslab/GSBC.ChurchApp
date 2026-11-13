namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistryLogic
{
    public static bool MaySee(string audience, Func<string, bool> holdsHere, Func<string, bool> holdsBelow) => audience switch
    {
        Audiences.Everyone => true,
        Audiences.Team => holdsHere(MinistriesPermissions.TeamViewNode) || holdsBelow(MinistriesPermissions.TeamViewNode),
        _ when Audiences.NodeOf(audience) is { } node => holdsHere(node),
        _ => false,
    };

    public static IReadOnlyList<string> PostableAudiences(string attendJoin) =>
        attendJoin == Joining.None ? [Audiences.Everyone, Audiences.Team] : Audiences.All;

    public static string Yours(IReadOnlyCollection<string> roles, IReadOnlyCollection<string> childrenAttending, bool leadsFromAbove) =>
        roles.Contains(Roles.Leader) || leadsFromAbove ? "You lead it"
        : roles.Contains(Roles.Admin) ? "You look after it"
        : roles.Contains(Roles.Team) ? "On the team"
        : childrenAttending.Count > 0 ? $"{Names(childrenAttending)} {(childrenAttending.Count == 1 ? "attends" : "attend")}"
        : roles.Contains(Roles.Attendee) ? "You come along"
        : "";

    public static bool IsHidden(MinistryDetails ministry) => ministry.Hidden || ministry.HiddenBy.Length > 0;

    public static bool IsOn(MinistryDetails ministry, string part) => MinistryParts.IsOn(ministry.PartsOff, part);

    private static string Names(IReadOnlyCollection<string> names) =>
        names.Count <= 1 ? names.FirstOrDefault() ?? "" : $"{string.Join(", ", names.Take(names.Count - 1))} and {names.Last()}";
}
