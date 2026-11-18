using GSBC.ChurchApp.Features.Outreach.Admin.Map;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public enum OutreachTab
{
    Campaigns,
    Map,
    Days,
    Numbers,
    FollowUps,
    Settings,
}

public static class OutreachTabs
{
    public static OutreachTab Of(string? slug) => slug switch
    {
        "map" => OutreachTab.Map,
        "days" => OutreachTab.Days,
        "numbers" => OutreachTab.Numbers,
        "follow-ups" => OutreachTab.FollowUps,
        "settings" => OutreachTab.Settings,
        _ when OutreachMapJobs.Of(slug) is not null => OutreachTab.Map,
        _ => OutreachTab.Campaigns,
    };

    public static string Href(OutreachTab tab) => tab switch
    {
        OutreachTab.Map => "outreach/map",
        OutreachTab.Days => "outreach/days",
        OutreachTab.Numbers => "outreach/numbers",
        OutreachTab.FollowUps => "outreach/follow-ups",
        OutreachTab.Settings => "outreach/settings",
        _ => "outreach",
    };
}
