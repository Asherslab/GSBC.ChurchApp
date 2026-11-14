using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public static class OutreachDays
{
    public static OutreachDay? On(OutreachNow now, DateTime utc) => now.Days.FirstOrDefault(day => day.StartsAt <= utc && day.EndsAt > utc);

    public static OutreachDay? Next(OutreachNow now, DateTime utc) => now.Days.Where(day => day.StartsAt > utc).MinBy(day => day.StartsAt);

    public static int Claimed(OutreachNow now, OutreachDay day) =>
        now.RoutesOf(day.Id).Count(route => now.Claims.Any(claim => claim.RouteId == route.Id));

    public static string ClaimedBy(OutreachNow now, DayRoute route)
    {
        List<string> names = [.. now.ClaimsOf(route.Id).Select(claim => claim.FirstName)];
        return names.Count switch
        {
            0 => "",
            1 or 2 => string.Join(", ", names),
            _ => $"{names[0]}, {names[1]} + {names.Count - 2}",
        };
    }
}
