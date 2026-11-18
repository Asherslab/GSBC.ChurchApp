namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachFeeds
{
    public const string Settings = "outreach.settings";
    public const string Campaigns = "outreach.campaigns";
    public const string Coverage = "outreach.coverage";
    public const string Days = "outreach.days";
    public const string Routes = "outreach.routes";
    public const string Claims = "outreach.claims";
    public const string Skips = "outreach.skips";
    public const string Exclusions = "outreach.exclusions";
    public const string Leaderboards = "outreach.leaderboards";
    public const string Mine = "outreach.mine";
    public const string FollowUps = "outreach.follow-ups";
    public const string Desk = "outreach.desk";

    public static readonly IReadOnlyList<string> Public = [Settings, Campaigns, Coverage, Days, Routes, Claims, Skips, Exclusions, Leaderboards];

    public static readonly IReadOnlyList<string> Private = [Mine, FollowUps];
}
