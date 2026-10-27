namespace Gsbc.AppPoc.Data;

public sealed record ServiceOccurrence(ServiceTime Service, DateTime Start)
{
    public DateTime End => Start + Service.Length;
}

public sealed class ChurchClock(SampleContent content)
{
    private const int DaysInWeek = 7;
    private static readonly DateTime LivePreviewMoment = new(2026, 11, 1, 10, 47, 0);

    public bool PreviewLive { get; set; }

    public DateTime Now => PreviewLive ? LivePreviewMoment : DateTime.Now;

    public ServiceOccurrence? LiveNow() =>
        Upcoming(Now.AddHours(-3)).TakeWhile(o => o.Start <= Now).FirstOrDefault(o => o.Service.Streamed && Now < o.End);

    public ServiceOccurrence NextService() => Upcoming(Now).First(o => o.Service.Name != "LIFE Groups");

    public IEnumerable<ServiceOccurrence> Upcoming(DateTime from)
    {
        for (var day = from.Date; day < from.Date.AddDays(DaysInWeek + 1); day = day.AddDays(1))
        {
            foreach (var service in content.Services.Where(s => s.Day == day.DayOfWeek).OrderBy(s => s.Start))
            {
                var start = day + service.Start.ToTimeSpan();
                if (start + service.Length > from)
                {
                    yield return new(service, start);
                }
            }
        }
    }

    public string Relative(DateTime moment)
    {
        var days = (moment.Date - Now.Date).Days;
        return days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            > 1 and < 7 => moment.ToString("dddd"),
            _ => moment.ToString("ddd d MMM"),
        };
    }

    public static string Time(DateTime moment) =>
        moment.Minute == 0 ? moment.ToString("h tt").ToLowerInvariant().Replace(" ", "") : moment.ToString("h:mm tt").ToLowerInvariant().Replace(" ", "");
}
