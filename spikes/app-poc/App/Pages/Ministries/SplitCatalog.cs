using Gsbc.AppPoc.Data.Ministries;

namespace Gsbc.AppPoc.Pages.Ministries;

public sealed record SplitOption(ViewingSplit Id, string Name, string Summary, bool Recommended = false);

public static class SplitCatalog
{
    public static readonly IReadOnlyList<SplitOption> All =
    [
        new(ViewingSplit.TeamRoom, "Team room", "The ministry page is the public front door. Members get a separate team room for rosters, chats and team notices.", true),
        new(ViewingSplit.Zones, "Two zones", "One page in two parts: what everyone sees, then a shaded team-only part below it."),
        new(ViewingSplit.Marked, "Marked", "One page, one list per kind. Members see everything together, with members-only items marked by a lock."),
    ];

    public static SplitOption For(ViewingSplit id) => All.First(s => s.Id == id);

    public static string RoleHint(ViewerRole role) => role switch
    {
        ViewerRole.Visitor => "Not in any ministry. Join Young Adults or Men's to see joining work.",
        ViewerRole.Member => "In Choir, Young Adults and Children's Ministry. Being in Choir makes you a Music member too.",
        ViewerRole.Leader => "Leads Music, and so its four teams; a member of Young Adults.",
        _ => "Can open and run every ministry, and message all serving teams.",
    };

    public static string RoleName(ViewerRole role) => role switch
    {
        ViewerRole.Visitor => "Visitor",
        ViewerRole.Member => "Member",
        ViewerRole.Leader => "Leader",
        _ => "Staff",
    };
}
