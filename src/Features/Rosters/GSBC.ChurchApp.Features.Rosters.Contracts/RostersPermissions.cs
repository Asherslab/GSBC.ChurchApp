using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Rosters.Contracts;

public static class RostersPermissions
{
    public const string ReplyNode = "ministries.roster.reply";
    public const string EditNode = "ministries.roster.edit";
    public const string AwayNode = "ministries.roster.away";

    public static readonly Permission Reply =
        new(ReplyNode, ScopeKind.Ministry, "See a ministry's roster and answer for their own dates", area: "Ministries");

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Ministry, "Manage a ministry's roster: its positions, its dates and who's on", area: "Ministries");

    public static readonly Permission Away =
        new(AwayNode, ScopeKind.Global, "See and change anyone's days away, from their page in the admin panel", area: "Ministries");

    public static readonly IReadOnlyList<Permission> All = [Reply, Edit, Away];
}
