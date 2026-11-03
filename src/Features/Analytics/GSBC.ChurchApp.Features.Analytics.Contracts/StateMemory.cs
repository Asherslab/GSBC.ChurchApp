using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public sealed class StateMemory
{
    public Dictionary<string, int> NotedWeek { get; set; } = [];

    public bool TakeIfDue(Measure measure, DateOnly weekStart)
    {
        if (NotedWeek.GetValueOrDefault(measure.Key) == weekStart.DayNumber)
            return false;
        NotedWeek[measure.Key] = weekStart.DayNumber;
        return true;
    }
}
