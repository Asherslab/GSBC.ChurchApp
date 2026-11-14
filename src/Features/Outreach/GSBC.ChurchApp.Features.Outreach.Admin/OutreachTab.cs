namespace GSBC.ChurchApp.Features.Outreach.Admin;

public enum OutreachTab
{
    Campaigns,
    Days,
    Areas,
    FollowUps,
    Reports,
    Settings,
}

public static class OutreachTabs
{
    public static OutreachTab Of(string? slug) => slug switch
    {
        "days" => OutreachTab.Days,
        "areas" => OutreachTab.Areas,
        "follow-ups" => OutreachTab.FollowUps,
        "reports" => OutreachTab.Reports,
        "settings" => OutreachTab.Settings,
        _ => OutreachTab.Campaigns,
    };

    public static string Href(OutreachTab tab) => tab switch
    {
        OutreachTab.Days => "outreach/days",
        OutreachTab.Areas => "outreach/areas",
        OutreachTab.FollowUps => "outreach/follow-ups",
        OutreachTab.Reports => "outreach/reports",
        OutreachTab.Settings => "outreach/settings",
        _ => "outreach",
    };
}
