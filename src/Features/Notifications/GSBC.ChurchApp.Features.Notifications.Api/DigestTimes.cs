using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal static class DigestTimes
{
    public static DateTime? Next(DigestSchedule schedule, DateTime startedAt, DateTime now)
    {
        DateTime[] soon = schedule.After.Select(after => startedAt + after).Where(time => time > now).Order().ToArray();
        if (soon.Length > 0)
            return soon[0];
        if (schedule.DailyAt is not { } dailyAt)
            return null;

        DateOnly day = ChurchTime.DayOf(now);
        DateTime today = ChurchTime.ToUtc(day.ToDateTime(dailyAt));
        return today > now ? today : ChurchTime.ToUtc(day.AddDays(1).ToDateTime(dailyAt));
    }
}
