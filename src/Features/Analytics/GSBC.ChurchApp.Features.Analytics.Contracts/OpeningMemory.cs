using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public sealed class OpeningMemory
{
    private const int DaysInAWeek = 7;
    private const int MonthsInAYear = 12;

    public int LastDay { get; set; }
    public int LastWeek { get; set; }
    public int LastMonth { get; set; }
    public bool Installed { get; set; }

    public bool IsNewWeek(DateOnly weekStart) => LastWeek != weekStart.DayNumber;

    public IReadOnlyList<Measure> Open(DateOnly today, DateOnly weekStart)
    {
        int week = weekStart.DayNumber, month = today.Year * MonthsInAYear + today.Month;
        List<Measure> counts = [];
        if (!Installed)
            counts.Add(AnalyticsMeasures.Installed);
        if (LastDay != today.DayNumber)
            counts.Add(AnalyticsMeasures.ActiveDay);
        if (LastWeek != week)
        {
            counts.Add(AnalyticsMeasures.ActiveWeek);
            if (LastWeek == week - DaysInAWeek)
                counts.Add(AnalyticsMeasures.Returning);
        }
        if (LastMonth != month)
            counts.Add(AnalyticsMeasures.ActiveMonth);

        Installed = true;
        LastDay = today.DayNumber;
        LastWeek = week;
        LastMonth = month;
        return counts;
    }
}
