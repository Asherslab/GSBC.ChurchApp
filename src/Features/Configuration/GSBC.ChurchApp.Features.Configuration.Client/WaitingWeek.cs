using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public static class WaitingWeek
{
    public static DateTime ThisSunday(TimeProvider clock)
    {
        DateOnly today = ChurchTime.Today(clock);
        return ChurchTime.ToUtc(today.AddDays(((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7).ToDateTime(new TimeOnly(10, 30)));
    }

    public static DateTime LastSunday(TimeProvider clock)
    {
        DateOnly today = ChurchTime.Today(clock);
        return ChurchTime.ToUtc(today.AddDays(-(int)today.DayOfWeek).ToDateTime(new TimeOnly(10, 30)));
    }
}
