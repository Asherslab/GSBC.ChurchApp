using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed record JoinChoice(string PersonId, string Place, string Title, string Detail, string Action, bool Asks, string? Refusal);

public static class JoinChoices
{
    public static IReadOnlyList<JoinChoice> For(MinistryPage page)
    {
        MinistryDetails ministry = page.Ministry;
        YourPlace you = page.You;
        List<JoinChoice> choices = [];
        if (Offered(ministry.AttendJoin, Waiting(you, "", Places.Attending)) && !you.Roles.Contains(Roles.Attendee))
            choices.Add(Choice("", Places.Attending, "Me, to come along", ministry.AttendJoin, "", Waiting(you, "", Places.Attending)));
        foreach (ChildPlace child in you.Children)
            if (Offered(ministry.AttendJoin, child.Asked) && !child.Attends)
                choices.Add(Choice(child.Id, Places.Attending, $"{child.Name}, to come along", ministry.AttendJoin, child.Name,
                    child.Asked, child.Age is { } age ? $"{age}" : ""));
        bool askedForTeam = you.Asked.Any(ask => ask.As == Places.Team && ask.AskedBy.Length == 0);
        if (Offered(ministry.TeamJoin, askedForTeam) && !you.Roles.Contains(Roles.Team) && !you.Roles.Contains(Roles.Leader) && !you.Roles.Contains(Roles.Admin))
            choices.Add(you.Minor ? ParentFirst(ministry.TeamJoin, you) : Choice("", Places.Team, "Me, on the team", ministry.TeamJoin, "", Waiting(you, "", Places.Team)));
        return choices;
    }

    private static bool Offered(string joining, bool asked) => joining is Joining.Open or Joining.Ask || (joining == Joining.Invite && asked);

    public static bool AnyOpen(MinistryPage page) => For(page).Any(choice => choice.Refusal is null);

    private static bool Waiting(YourPlace you, string personId, string place) =>
        you.Asked.Any(ask => ask.As == place && (personId.Length == 0 ? ask.AskedBy.Length == 0 : ask.PersonId == personId));

    private static JoinChoice ParentFirst(string joining, YourPlace you)
    {
        AskLine? asked = you.Asked.FirstOrDefault(ask => ask.As == Places.Team && ask.AskedBy.Length == 0);
        string? refusal = asked is { WaitingForParent: true } ? "Already asked. Waiting for a parent of yours."
            : asked is not null ? "Already asked. Waiting for a leader."
            : null;
        return new JoinChoice("", Places.Team, "Me, on the team",
            joining == Joining.Open ? "A parent of yours says yes, then you're on the team" : "A parent of yours says yes, then a leader does",
            "Ask a parent", joining == Joining.Ask, refusal);
    }

    private static JoinChoice Choice(string personId, string place, string title, string joining, string childName, bool waiting, string age = "")
    {
        string detail = joining == Joining.Open
            ? place == Places.Team ? "You're on the team straight away" : $"{(childName.Length > 0 ? childName + " is" : "You're")} in straight away"
            : "A leader says yes, then you'll hear in the app";
        if (age.Length > 0)
            detail = $"{age} · {detail}";
        string action = joining == Joining.Open ? "Join" : childName.Length > 0 ? $"Ask for {childName}" : "Ask to join";
        string? refusal = waiting ? "Already asked. Waiting for a leader." : null;
        return new JoinChoice(personId, place, title, detail, action, joining == Joining.Ask, refusal);
    }
}
