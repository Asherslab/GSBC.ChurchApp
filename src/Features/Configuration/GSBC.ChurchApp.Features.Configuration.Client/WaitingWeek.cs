using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public static class WaitingWeek
{
    public sealed record Gathering(DayOfWeek Day, TimeOnly At, string What)
    {
        public DateTime StartsAt { get; init; }
    }

    private static readonly Gathering[] Rhythm =
    [
        new(DayOfWeek.Sunday, new TimeOnly(9, 0), "LIFE Groups"),
        new(DayOfWeek.Sunday, new TimeOnly(10, 30), "Morning worship"),
        new(DayOfWeek.Sunday, new TimeOnly(18, 0), "Evening worship"),
        new(DayOfWeek.Wednesday, new TimeOnly(19, 0), "Bible study and prayer"),
    ];

    public static IReadOnlyList<Gathering> Next(TimeProvider clock, int count)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        DateOnly today = ChurchTime.DayOf(now);
        return Enumerable.Range(0, 8)
            .SelectMany(offset => Rhythm.Where(gathering => gathering.Day == today.AddDays(offset).DayOfWeek)
                .Select(gathering => gathering with { StartsAt = ChurchTime.ToUtc(today.AddDays(offset).ToDateTime(gathering.At)) }))
            .Where(gathering => gathering.StartsAt > now)
            .OrderBy(gathering => gathering.StartsAt)
            .Take(count)
            .ToList();
    }

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
