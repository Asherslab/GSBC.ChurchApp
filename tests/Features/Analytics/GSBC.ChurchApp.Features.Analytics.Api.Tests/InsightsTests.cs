using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

public sealed class InsightsTests
{
    private const string BibleTile = "3f1c9a52-6a1b-4a8e-9c1e-0d6c1c6f1a01";
    private static readonly DateOnly Monday = new(2026, 11, 2);

    [Fact]
    public void CountsBelowFiveNeverLeaveTheServerAsNumbers()
    {
        List<GroupTally> groups = InsightsTallies.GroupsOf([Opened(Monday, UsageGroup.NotSignedIn, 3), Opened(Monday, UsageGroup.SignedIn, 9)]);
        TopList tiles = InsightsTallies.Ranked(TestMeasures.TileTapped, [new SubjectTotal(TestMeasures.TileTapped.Key, BibleTile, 2)],
            new Dictionary<string, string> { [BibleTile] = "Bible" });

        GroupTally notSignedIn = Assert.Single(groups, group => group.Group == UsageGroup.NotSignedIn);
        Assert.Equal((true, 0L), (notSignedIn.Phones!.FewerThanFive, notSignedIn.Phones.Value));
        Assert.Equal(9, Assert.Single(groups, group => group.Group == UsageGroup.SignedIn).Phones!.Value);
        LabelTally bible = Assert.Single(tiles.Entries);
        Assert.Equal(("Bible", true, 0L), (bible.Label, bible.Count!.FewerThanFive, bible.Count.Value));
    }

    [Fact]
    public void TheWeeklyTrendFollowsMondayToSundayInBrisbane()
    {
        DateOnly today = ChurchTime.Today(new TestClock(new DateTime(2026, 11, 1, 23, 0, 0, DateTimeKind.Utc)));
        DateOnly first = ChurchTime.WeekStart(today).AddDays(-7 * (InsightsTallies.TrendWeeks - 1));

        List<WeekPoint> weeks = InsightsTallies.Weeks([Opened(today.AddDays(-1), UsageGroup.SignedIn, 6), Opened(today, UsageGroup.SignedIn, 8)], first);

        Assert.Equal(Monday, today);
        Assert.Equal(InsightsTallies.TrendWeeks, weeks.Count);
        Assert.Equal(Monday.DayNumber, weeks[^1].WeekStart);
        Assert.Equal([6, 8], weeks.TakeLast(2).Select(week => week.Phones!.Value));
    }

    [Fact]
    public void AStateShowsInItsDeclaredWordsWithSmallCountsHidden()
    {
        string key = TestMeasures.Notifications.Key;

        TopList state = InsightsTallies.Stated(TestMeasures.Notifications,
            [new SubjectTotal(key, "on", 6), new SubjectTotal(key, "off", 1), new SubjectTotal("another.measure", "on", 40)]);

        Assert.Equal("Notifications", state.Label);
        Assert.Equal(["On", "Turned off"], state.Entries.Select(entry => entry.Label));
        Assert.Equal(6, state.Entries[0].Count!.Value);
        Assert.True(state.Entries[1].Count!.FewerThanFive);
    }

    private static DailyCountRecord Opened(DateOnly day, UsageGroup group, long phones) =>
        new() { Day = day, Measure = AnalyticsMeasures.ActiveWeek.Key, Group = group, Count = phones };
}
