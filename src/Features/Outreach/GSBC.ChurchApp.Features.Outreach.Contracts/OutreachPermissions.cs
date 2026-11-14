using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachPermissions
{
    public const string PlanNode = "outreach.plan";
    public const string ReportsNode = "outreach.reports.view";
    public const string ReviewNode = "outreach.review";
    public const string SettingsNode = "outreach.settings.change";
    public const string ExpireNode = "outreach.coverage.expire";

    public const string ManagerRole = "outreach-manager";

    public static readonly Permission Plan =
        new(PlanNode, ScopeKind.Ministry, "Plan outreach: campaigns, outreach days, their routes and saved areas", area: "Outreach");

    public static readonly Permission Reports =
        new(ReportsNode, ScopeKind.Ministry, "See what outreach has covered and its totals, and follow an outreach day as it happens", area: "Outreach",
            opensAdminArea: true);

    public static readonly Permission Review =
        new(ReviewNode, ScopeKind.Ministry,
            "Review what walkers report and the follow-ups people asked for at the door, with their contact details, and clear No junk mail and Don't knock marks",
            isSensitive: true, area: "Outreach");

    public static readonly Permission Settings =
        new(SettingsNode, ScopeKind.Ministry, "Change outreach's settings, and the places and addresses outreach never goes to", area: "Outreach");

    public static readonly Permission Expire =
        new(ExpireNode, ScopeKind.Ministry, "Mark streets as not done, so walkers are sent there again before the rest period ends", area: "Outreach");

    public static readonly IReadOnlyList<Permission> All = [Plan, Reports, Review, Settings, Expire];

    public static readonly IReadOnlyList<string> LeaderNodes = [PlanNode, ReportsNode, ReviewNode, SettingsNode, ExpireNode];
}
