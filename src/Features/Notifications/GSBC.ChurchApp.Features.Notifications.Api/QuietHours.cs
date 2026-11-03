using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal static class QuietHours
{
    private static readonly TimeOnly Starts = new(21, 0);
    private static readonly TimeOnly Ends = new(7, 0);

    public static DateTime NextSendable(DateTime utc)
    {
        DateTime church = ChurchTime.ToChurch(utc);
        TimeOnly time = TimeOnly.FromDateTime(church);
        if (time < Starts && time >= Ends)
            return utc;

        DateTime morning = church.Date.Add(Ends.ToTimeSpan());
        if (time >= Starts)
            morning = morning.AddDays(1);
        return ChurchTime.ToUtc(morning);
    }
}
