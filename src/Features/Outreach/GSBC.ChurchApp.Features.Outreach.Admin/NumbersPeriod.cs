using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public enum NumbersSpan
{
    ThirteenWeeks,
    ThisYear,
    TwelveMonths,
}

public sealed record NumbersPeriod(NumbersSpan Span, DateOnly From, DateOnly To, DateOnly PreviousFrom, DateOnly PreviousTo)
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-AU");

    public static NumbersPeriod Of(NumbersSpan span, DateOnly today)
    {
        DateOnly to = today.AddDays(1);
        return span switch
        {
            NumbersSpan.ThisYear => new(span, new DateOnly(today.Year, 1, 1), to, new DateOnly(today.Year - 1, 1, 1), to.AddYears(-1)),
            NumbersSpan.TwelveMonths => Months(span, new DateOnly(today.Year, today.Month, 1).AddMonths(-11), to),
            _ => Weeks(span, ChurchTime.WeekStart(today).AddDays(-7 * 12), to),
        };
    }

    private static NumbersPeriod Weeks(NumbersSpan span, DateOnly from, DateOnly to) => new(span, from, to, from.AddDays(-7 * 13), from);

    private static NumbersPeriod Months(NumbersSpan span, DateOnly from, DateOnly to) => new(span, from, to, from.AddMonths(-12), from);

    public TotalsBy By => Span == NumbersSpan.ThirteenWeeks ? TotalsBy.Week : TotalsBy.Month;

    public string Before => Span switch
    {
        NumbersSpan.ThisYear => "the same months last year",
        NumbersSpan.TwelveMonths => "the 12 months before",
        _ => "the 13 weeks before",
    };

    public string Bucket => By == TotalsBy.Week ? "Week of" : "Month";

    public DateTime FromUtc => Utc(From);

    public DateTime ToUtc => Utc(To);

    public DateTime PreviousFromUtc => Utc(PreviousFrom);

    public DateTime PreviousToUtc => Utc(PreviousTo);

    public IReadOnlyList<DateOnly> Starts => StartsBetween(From, To);

    public IReadOnlyList<DateOnly> PreviousStarts => StartsBetween(PreviousFrom, PreviousTo);

    public IReadOnlyList<TrendPoint> Points(IReadOnlyList<TotalsRow> rows, IReadOnlyList<DateOnly> starts, bool withYear = false)
    {
        Dictionary<DateOnly, int> letterboxes = rows.GroupBy(row => ChurchTime.DayOf(row.Starts)).ToDictionary(group => group.Key, group => group.Sum(row => row.Letterboxes));
        return starts.Select(start => letterboxes.GetValueOrDefault(start))
            .Zip(starts, (count, start) => new TrendPoint(Label(start, withYear), count, count.ToString("N0", English)))
            .ToList();
    }

    public static string Change(int now, int before, string beforeWords) =>
        before == 0 ? now == 0 ? $"None in {beforeWords} either" : $"None in {beforeWords}"
        : (int)Math.Round((now - before) * 100.0 / before) is var percent && percent == 0 ? $"The same as {beforeWords}"
        : percent > 0 ? $"Up {percent}% on {beforeWords}"
        : $"Down {-percent}% on {beforeWords}";

    private string Label(DateOnly start, bool withYear) =>
        start.ToString(By == TotalsBy.Week ? withYear ? "d MMM yyyy" : "d MMM" : withYear ? "MMM yyyy" : "MMM", English);

    private List<DateOnly> StartsBetween(DateOnly from, DateOnly to)
    {
        List<DateOnly> starts = [];
        for (DateOnly start = from; start < to; start = By == TotalsBy.Week ? start.AddDays(7) : start.AddMonths(1))
            starts.Add(start);
        return starts;
    }

    private static DateTime Utc(DateOnly day) => ChurchTime.ToUtc(day.ToDateTime(TimeOnly.MinValue));
}
