namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class QuietHours
{
    public static readonly TimeOnly Starts = new(21, 0);
    public static readonly TimeOnly Ends = new(7, 0);

    public static DateTime? HeldUntil(DateTime churchTime)
    {
        TimeOnly time = TimeOnly.FromDateTime(churchTime);
        if (time < Starts && time >= Ends)
            return null;

        DateTime morning = churchTime.Date.Add(Ends.ToTimeSpan());
        return time >= Starts ? morning.AddDays(1) : morning;
    }
}
