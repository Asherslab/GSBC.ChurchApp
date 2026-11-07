using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed record SubjectTotal(string Measure, string Subject, long Count);

internal static class InsightsTallies
{
    public const int TrendWeeks = 12;
    public const int DaysInAWeek = 7;
    private const int TopEntries = 5;

    private static readonly UsageGroup[] Groups = [UsageGroup.NotSignedIn, UsageGroup.SignedIn, UsageGroup.OnATeam];

    public static List<GroupTally> GroupsOf(IReadOnlyCollection<DailyCountRecord> thisWeek) =>
        Groups.Select(group => new GroupTally { Group = group, Phones = Tally.Of(thisWeek.Where(count => count.Group == group).Sum(count => count.Count)) }).ToList();

    public static List<WeekPoint> Weeks(IReadOnlyCollection<DailyCountRecord> weekly, DateOnly first) =>
        Enumerable.Range(0, TrendWeeks)
            .Select(week => first.AddDays(DaysInAWeek * week))
            .Select(start => new WeekPoint
            {
                WeekStart = start.DayNumber,
                Phones = Tally.Of(weekly.Where(count => count.Day >= start && count.Day < start.AddDays(DaysInAWeek)).Sum(count => count.Count)),
            })
            .ToList();

    public static List<SubjectTotal> TopOf(Measure measure, IEnumerable<SubjectTotal> totals) =>
        totals.Where(total => total.Measure == measure.Key).OrderByDescending(total => total.Count).Take(TopEntries).ToList();

    public static TopList Ranked(Measure measure, IEnumerable<SubjectTotal> top, IReadOnlyDictionary<string, string> names) => new()
    {
        Measure = measure.Key,
        Label = measure.Label,
        Counts = measure.Counts,
        Entries = top.Select(total => new LabelTally { Label = names.GetValueOrDefault(total.Subject, "Something since removed"), Count = Tally.Of(total.Count) }).ToList(),
    };

    public static TopList Stated(Measure measure, IReadOnlyCollection<SubjectTotal> totals) => new()
    {
        Measure = measure.Key,
        Label = measure.Label,
        Counts = measure.Counts,
        Entries = measure.States!
            .Select(state => (state.Label, Count: totals.Where(total => total.Measure == measure.Key && total.Subject == state.Key).Sum(total => total.Count)))
            .Where(state => state.Count > 0)
            .Select(state => new LabelTally { Label = state.Label, Count = Tally.Of(state.Count) })
            .ToList(),
    };
}
