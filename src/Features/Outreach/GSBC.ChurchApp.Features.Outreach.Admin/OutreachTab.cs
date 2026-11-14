namespace GSBC.ChurchApp.Features.Outreach.Admin;

public enum OutreachTab
{
    Campaigns,
    Coverage,
    Numbers,
    Places,
    FollowUps,
    Reports,
    Settings,
}

public static class OutreachTabs
{
    public static OutreachTab Of(string? slug) => slug switch
    {
        "coverage" => OutreachTab.Coverage,
        "numbers" => OutreachTab.Numbers,
        "places" => OutreachTab.Places,
        "follow-ups" => OutreachTab.FollowUps,
        "reports" => OutreachTab.Reports,
        "settings" => OutreachTab.Settings,
        _ => OutreachTab.Campaigns,
    };

    public static string Href(OutreachTab tab) => tab switch
    {
        OutreachTab.Coverage => "outreach/coverage",
        OutreachTab.Numbers => "outreach/numbers",
        OutreachTab.Places => "outreach/places",
        OutreachTab.FollowUps => "outreach/follow-ups",
        OutreachTab.Reports => "outreach/reports",
        OutreachTab.Settings => "outreach/settings",
        _ => "outreach",
    };
}
