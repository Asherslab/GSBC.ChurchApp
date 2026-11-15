using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public static class OutreachDays
{
    public static OutreachDay? On(OutreachNow now, DateTime utc) => now.Days.FirstOrDefault(day => day.StartsAt <= utc && day.EndsAt > utc);

    public static OutreachDay? Next(OutreachNow now, DateTime utc) => Coming(now, utc).FirstOrDefault();

    public static IEnumerable<OutreachDay> Coming(OutreachNow now, DateTime utc) =>
        now.Days.Where(day => day.StartsAt > utc && Shown(now, day, ChurchTime.DayOf(utc))).OrderBy(day => day.StartsAt);

    private static bool Shown(OutreachNow now, OutreachDay day, DateOnly today) =>
        now.CampaignOf(day.Campaign) is not { } campaign || campaign.General || CampaignStages.Now(campaign, today) != CampaignStage.Preparing;

    public static int Claimed(OutreachNow now, OutreachDay day) =>
        now.RoutesOf(day.Id).Count(route => now.Claims.Any(claim => claim.RouteId == route.Id));
}
