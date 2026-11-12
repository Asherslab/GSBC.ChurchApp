using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal static class MinistryRules
{
    public const int LongestName = 60;
    private const int LongestSummary = 140;
    private const int LongestText = 4000;
    public const int LongestTitle = 120;
    public const int LongestLine = 200;

    public static JoinDecision Decide(string joining, string place) => joining switch
    {
        Joining.Open => JoinDecision.Join,
        Joining.Ask => JoinDecision.Ask,
        Joining.Invite => JoinDecision.Refuse(place == Places.Team
            ? "Leaders invite people to this team. Ask one of them."
            : "Leaders add people to this. Ask one of them."),
        _ => JoinDecision.Refuse("This ministry doesn't take attendees."),
    };

    public static string WhyNotChangeRole(string role) => role switch
    {
        Roles.Leader => "Only the leaders above this ministry, or staff, can make or unmake a leader here.",
        Roles.Admin => "Only the leaders above this ministry, or staff, can make or unmake an admin here.",
        _ => "You can't change who's in this ministry.",
    };

    public static bool NeedsParentsYes(string place, bool minor, bool askingForThemselves) => place == Places.Team && minor && askingForThemselves;

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

    public static string? WhyNotDetails(MinistryDetails details)
    {
        if (details.Name.Trim().Length is 0 or > LongestName)
            return $"Give it a name of up to {LongestName} characters.";
        if (details.Summary.Trim().Length > LongestSummary)
            return $"Keep the line about it to {LongestSummary} characters.";
        if (details.About.Trim().Length > LongestText)
            return $"Keep the about to {LongestText} characters.";
        if (!Joining.IsValid(details.TeamJoin, noneAllowed: false))
            return "Choose how people join the team.";
        return Joining.IsValid(details.AttendJoin, noneAllowed: true) ? null : "Choose how people come along.";
    }

    public static string? WhyNotMove(MinistryTree tree, MinistryRecord ministry, MinistryRecord? under, Func<MinistryRecord?, bool> mayAddUnder)
    {
        if (under?.Id == ministry.ParentId)
            return under is null ? "It's already a top-level ministry." : $"It already sits under {under.Name}.";
        if (tree.WouldLoop(ministry.Id, under?.Id))
            return "A ministry can't sit under itself or under a ministry that's under it.";
        if (!mayAddUnder(under))
            return under is null ? "Only staff make a ministry top-level." : $"You can't add ministries under {under.Name}.";
        if (under is not null && tree.IsHidden(under.Id) && tree.SeenFrom(ministry.Id).Any(id => tree.Find(id)!.ShowsInLifeGroups))
            return $"{under.Name} is hidden, and Life Groups are shown to everyone, so a Life Group can't sit under it.";
        return null;
    }

    public static string? WhyNotHide(MinistryTree tree, Guid id, Guid? parentId, MinistryDetails wanted)
    {
        bool hiddenAbove = parentId is { } parent && tree.IsHidden(parent);
        if (wanted.ShowsInLifeGroups && (wanted.Hidden || hiddenAbove))
            return hiddenAbove
                ? $"{tree.Find(parentId!.Value)?.Name} is hidden, and Life Groups are shown to everyone, so this can't show in Life Groups."
                : "Life Groups are shown to everyone, so a Life Group can't be hidden.";
        if (wanted.Hidden && tree.Below(id).Any(below => below.ShowsInLifeGroups))
            return "A Life Group sits under it, and Life Groups are shown to everyone, so it can't be hidden.";
        return null;
    }
}

internal sealed record JoinDecision(bool Joins, bool Asks, string? Refusal)
{
    public static readonly JoinDecision Join = new(true, false, null);
    public static readonly JoinDecision Ask = new(false, true, null);

    public static JoinDecision Refuse(string why) => new(false, false, why);
}
