using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class LifeGroupsCalendarTests
{
    private static readonly TimeOnly Nine = new(9, 0), QuarterToTen = new(9, 45), Ten = new(10, 0), QuarterToEleven = new(10, 45);

    [Fact]
    public void RoundSundaysAreOnAtTheirRoundsTimeAndEverySundayBetweenIsNotOnThroughTheLastRound()
    {
        RoundRecord autumn = Round(Nine, QuarterToTen, 0, 1, 3);
        RoundRecord summer = Round(Ten, QuarterToEleven, 6, 7);

        KeptRegularEvent plan = LifeGroupSundays.Plan([summer, autumn]);

        Assert.Equal([(Sunday(0), Nine), (Sunday(1), Nine), (Sunday(3), Nine), (Sunday(6), Ten), (Sunday(7), Ten)], plan.On.Select(date => (date.Date, date.Starts)));
        Assert.Equal(QuarterToEleven, plan.On[^1].Ends);
        Assert.Equal([(Sunday(2), LifeGroupSundays.TakenOff), (Sunday(4), LifeGroupSundays.Between), (Sunday(5), LifeGroupSundays.Between)],
            plan.NotOn.Select(gap => (gap.Date, gap.Why)));
        Assert.Equal(MinistriesDestinations.LifeGroups.Path, plan.OpensAt);
    }

    [Fact]
    public void ChangingTheRoundsChangesThePlanAndNoRoundsLeavesNoDates()
    {
        RoundRecord autumn = Round(Nine, QuarterToTen, 0, 1, 2);
        RoundRecord summer = Round(Nine, QuarterToTen, 5);
        Assert.Equal([Sunday(3), Sunday(4)], LifeGroupSundays.Plan([autumn, summer]).NotOn.Select(gap => gap.Date));

        summer.Sundays = [Sunday(3)];
        KeptRegularEvent moved = LifeGroupSundays.Plan([autumn, summer]);

        Assert.Empty(moved.NotOn);
        Assert.Equal(Sunday(3), moved.On[^1].Date);
        Assert.Empty(LifeGroupSundays.Plan([]).On);
    }

    [Fact]
    public void AnAllYearGroupMeetsOnlyInRoundsAndSittingOneOutKeepsItOffForThatRoundOnly()
    {
        Round autumn = new() { Id = "autumn", Sundays = [Day(0), Day(1)] };
        Round summer = new() { Id = "summer", Sundays = [Day(4), Day(5)] };
        SittingOut youth = new() { GroupId = "youth", RoundId = summer.Id, Note = "Joining the combined group" };

        Assert.True(LifeGroupRounds.Runs("youth", "", autumn, [youth]));
        Assert.False(LifeGroupRounds.Runs("youth", "", summer, [youth]));
        Assert.Equal("Joining the combined group", LifeGroupRounds.SitsOut("youth", summer, [youth])?.Note);
        Assert.True(LifeGroupRounds.Runs("kids", "", summer, [youth]));
        Assert.False(LifeGroupRounds.Runs("study", autumn.Id, summer, []));
        Assert.Equal([Sunday(0), Sunday(1), Sunday(4), Sunday(5)],
            LifeGroupSundays.Plan([Round(Nine, QuarterToTen, 0, 1), Round(Nine, QuarterToTen, 4, 5)]).On.Select(date => date.Date));
    }

    [Fact]
    public void ASetIsBeingPlannedUntilItShowsInTheAppThenNowOnFromItsFirstSundayAndFinishedAfterItsLast()
    {
        Round set = new() { Id = "next", Sundays = [Day(2), Day(3)], ShowsFrom = Day(1) };
        DateOnly today = Sunday(0);

        Assert.Equal(RoundStage.BeingPlanned, LifeGroupRounds.StageOf(set, today));
        Assert.Equal(RoundStage.ShownInTheApp, LifeGroupRounds.StageOf(set, Sunday(1)));
        Assert.Equal(RoundStage.ShownInTheApp, LifeGroupRounds.StageOf(new Round { Sundays = [Day(2)] }, today));
        Assert.Equal(RoundStage.NowOn, LifeGroupRounds.StageOf(set, Sunday(3)));
        Assert.Equal(RoundStage.Finished, LifeGroupRounds.StageOf(set, Sunday(3).AddDays(1)));
    }

    private static RoundRecord Round(TimeOnly starts, TimeOnly ends, params int[] weeks) =>
        new() { Id = Guid.CreateVersion7(), Sundays = weeks.Select(Sunday).ToList(), Starts = starts, Ends = ends };

    private static DateOnly Sunday(int week) => new DateOnly(2026, 11, 8).AddDays(7 * week);

    private static string Day(int week) => Sunday(week).ToString("yyyy-MM-dd");
}
