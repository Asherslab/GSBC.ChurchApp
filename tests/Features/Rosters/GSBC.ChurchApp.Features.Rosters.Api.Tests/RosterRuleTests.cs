using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Rosters.Api.Tests;

public sealed class RosterRuleTests
{
    private static readonly DateOnly Sunday = new(2026, 11, 15);
    private static readonly DateTime Now = TestClock.Start;
    private static readonly DateTime Later = Now.AddDays(7);
    private static readonly DateRecord EveningService = new() { StartsAt = new DateTime(2026, 11, 15, 8, 0, 0, DateTimeKind.Utc), EndsAt = new DateTime(2026, 11, 15, 9, 30, 0, DateTimeKind.Utc) };

    [Fact]
    public void EveryUnmetRequirementIsFlaggedInWords()
    {
        PositionRecord cameras = new() { Name = "Cameras", NeedsBlueCard = true, NeedsTraining = true, NeedsTrained = true };
        IReadOnlyList<string> flags = RosterRules.Flags(cameras, EveningService, [Away(Sunday.AddDays(-1), Sunday)],
            new SafeguardingStanding(Guid.NewGuid(), HoldsBlueCard: false, TrainingCurrent: false), aloneTraining: true, alsoOn: ["Presenter PC"]);

        Assert.Equal([AwayRules.AwayFlag, RosterRules.NoBlueCardFlag, RosterRules.NoTrainingFlag, Training.AloneFlag, "Also on Presenter PC that day"], flags);
    }

    [Fact]
    public void SomeoneWhoMeetsEverythingHasNoFlags()
    {
        PositionRecord cameras = new() { Name = "Cameras", NeedsBlueCard = true, NeedsTraining = true, NeedsTrained = true };

        Assert.Empty(RosterRules.Flags(cameras, EveningService, [Away(Sunday.AddDays(1), Sunday.AddDays(3))],
            new SafeguardingStanding(Guid.NewGuid(), HoldsBlueCard: true, TrainingCurrent: true), aloneTraining: false, alsoOn: []));
    }

    [Fact]
    public void ATraineeNeedsSomeoneTrainedOnTheSamePositionWhicheverWasPutOnFirst()
    {
        Assert.Equal(["tom"], Training.Alone([("tom", "sound", true, true)]));
        Assert.Empty(Training.Alone([("tom", "sound", true, true), ("jo", "sound", true, false)]));
        Assert.Empty(Training.Alone([("jo", "sound", true, false), ("tom", "sound", true, true)]));
        Assert.Equal(["tom"], Training.Alone([("tom", "sound", true, true), ("jo", "cameras", true, false)]));
        Assert.Equal(["tom"], Training.Alone([("tom", "sound", true, true), ("jo", "sound", false, false)]));
        Assert.Equal(["lily", "tom"], Training.Alone([("tom", "sound", true, true), ("lily", "sound", true, true)]).Order());
    }

    [Fact]
    public void ATraineeDoesntFillAPlace() =>
        Assert.False(new Assignment { State = Replies.Accepted, Training = true }.Fills);

    [Theory]
    [InlineData(WhoSees.Everything, 3)]
    [InlineData(WhoSees.WithYou, 3)]
    [InlineData(WhoSees.Yours, 1)]
    public void TheMinistrysSettingDecidesHowMuchOfADateYouSee(string whoSees, int seen)
    {
        RosterDate date = Date(("you", null), ("jo", null), ("sam", null));

        Assert.Equal(seen, RosterRules.Visible(date, whoSees, leads: false, new HashSet<string> { "you" })!.Assignments.Count);
    }

    [Fact]
    public void WithYouLeavesOutDatesYoureNotOn()
    {
        RosterDate date = Date(("jo", null), ("sam", null));

        Assert.Null(RosterRules.Visible(date, WhoSees.WithYou, leads: false, new HashSet<string> { "you" }));
        Assert.Equal(2, RosterRules.Visible(date, WhoSees.WithYou, leads: true, new HashSet<string> { "you" })!.Assignments.Count);
    }

    [Fact]
    public void APlaceOpenForTheTeamToTakeShowsEvenWhenYouSeeOnlyYourOwn()
    {
        RosterDate date = Date(("jo", new SwapLine { State = SwapStates.Open, Way = SwapWays.Team }), ("sam", null));

        Assert.Equal(["jo"], RosterRules.Visible(date, WhoSees.Yours, leads: false, new HashSet<string> { "you" })!.Assignments.Select(each => each.PersonId));
    }

    [Fact]
    public void ASwapAskedOfSomeoneDirectlyShowsOnlyToThem()
    {
        RosterDate date = Date(("jo", new SwapLine { State = SwapStates.Open, Way = SwapWays.Direct, AskedToId = "sam" }));

        Assert.Null(RosterRules.Visible(date, WhoSees.Yours, leads: false, new HashSet<string> { "you" }));
        Assert.NotNull(RosterRules.Visible(date, WhoSees.Yours, leads: false, new HashSet<string> { "sam" }));
    }

    [Fact]
    public void OnlyThePersonOrSomeoneWhoMayActForThemCanAnswer()
    {
        Assert.NotNull(RosterRules.WhyNotReply(mayAct: false, archived: false, repliesOn: true, Later, Now));
        Assert.Null(RosterRules.WhyNotReply(mayAct: true, archived: false, repliesOn: true, Later, Now));
    }

    [Fact]
    public void AnArchivedMinistrysRosterCantChange()
    {
        Assert.Equal("This ministry is archived, so its roster can't change.", RosterRules.WhyNotChange(archived: true, Later, Now));
        Assert.NotNull(RosterRules.WhyNotReply(mayAct: true, archived: true, repliesOn: true, Later, Now));
    }

    [Fact]
    public void ASwapCanOnlyBeAskedTheWaysTheMinistryAllows()
    {
        RosterSettings teamOnly = new() { OpenSwaps = OpenSwaps.Team };
        RosterSettings both = new() { OpenSwaps = OpenSwaps.Team, DirectSwaps = true };
        Assert.NotNull(RosterRules.WhyNotAskSwap(true, false, RosterRules.WayFor(teamOnly, SwapWays.Direct), Replies.Accepted, false, Later, Now, toIsOnTeam: true));
        Assert.Null(RosterRules.WhyNotAskSwap(true, false, RosterRules.WayFor(both, SwapWays.Direct), Replies.Accepted, false, Later, Now, toIsOnTeam: true));
        Assert.NotNull(RosterRules.WhyNotAskSwap(true, false, RosterRules.WayFor(both, SwapWays.Direct), Replies.Accepted, false, Later, Now, toIsOnTeam: false));
        Assert.NotNull(RosterRules.WhyNotAskSwap(true, false, RosterRules.WayFor(new RosterSettings { OpenSwaps = OpenSwaps.Off, DirectSwaps = true }, SwapWays.Open),
            Replies.Accepted, false, Later, Now, toIsOnTeam: false));
    }

    [Theory]
    [InlineData(OpenSwaps.Team, SwapWays.Approve, SwapWays.Team)]
    [InlineData(OpenSwaps.Approve, SwapWays.Team, SwapWays.Approve)]
    [InlineData(OpenSwaps.Approve, SwapWays.Open, SwapWays.Approve)]
    public void WhetherALeaderConfirmsIsTheMinistrysChoiceNeverTheAskers(string ministryChose, string askedFor, string swapGoes) =>
        Assert.Equal(swapGoes, RosterRules.WayFor(new RosterSettings { OpenSwaps = ministryChose }, askedFor));

    [Theory]
    [InlineData(SwapWays.Team, SwapStates.Done)]
    [InlineData(SwapWays.Direct, SwapStates.Done)]
    [InlineData(SwapWays.Approve, SwapStates.Offered)]
    public void OnlyAnApprovedSwapWaitsForALeaderAfterSomeoneTakesIt(string way, string after) => Assert.Equal(after, RosterRules.AfterTaking(way));

    [Fact]
    public void ADirectSwapCanOnlyBeTakenByWhoeverWasAsked()
    {
        Guid asked = Guid.NewGuid(), someoneElse = Guid.NewGuid();
        SwapRecord swap = new() { Way = SwapWays.Direct, State = SwapStates.Open, AskedTo = asked };

        Assert.NotNull(RosterRules.WhyNotTake(swap, someoneElse, Guid.NewGuid(), takerOnTeam: true, mayAct: true, archived: false, Later, Now));
        Assert.Null(RosterRules.WhyNotTake(swap, asked, Guid.NewGuid(), takerOnTeam: true, mayAct: true, archived: false, Later, Now));
    }

    [Fact]
    public void AwayDatesNeedOnlyToMakeSense()
    {
        Assert.Null(AwayRules.WhyNot(Save(Sunday, Sunday), Sunday));
        Assert.Null(AwayRules.WhyNot(Save(Sunday, null, weekly: [0], until: "12:00"), Sunday));
        Assert.NotNull(AwayRules.WhyNot(Save(Sunday, null), Sunday));
        Assert.NotNull(AwayRules.WhyNot(Save(Sunday, Sunday.AddDays(-1)), Sunday.AddDays(-5)));
        Assert.NotNull(AwayRules.WhyNot(Save(Sunday.AddDays(-3), Sunday.AddDays(-2)), Sunday));
        Assert.NotNull(AwayRules.WhyNot(Save(Sunday, null, weekly: []), Sunday));
        Assert.NotNull(AwayRules.WhyNot(Save(Sunday, Sunday, from: "17:00", until: "12:00"), Sunday));
    }

    [Fact]
    public void RepeatingDaysAwayCoverOnlyTheirWeekdaysTimesAndNotTheExceptions()
    {
        AwaySpan sundayMornings = new()
        {
            From = Wire(Sunday), Weekly = true, Weekdays = [(int)DayOfWeek.Sunday], EndTime = "12:00", Except = [Wire(Sunday.AddDays(14))],
        };

        Assert.True(AwayRules.Covers(sundayMornings, At(Sunday, 9), At(Sunday, 10, 15), allDay: false));
        Assert.True(AwayRules.Covers(sundayMornings, At(Sunday.AddDays(7 * 30), 10, 30), At(Sunday.AddDays(7 * 30), 12), allDay: false));
        Assert.False(AwayRules.Covers(sundayMornings, At(Sunday, 18), At(Sunday, 19, 30), allDay: false));
        Assert.False(AwayRules.Covers(sundayMornings, At(Sunday.AddDays(14), 9), At(Sunday.AddDays(14), 10), allDay: false));
        Assert.False(AwayRules.Covers(sundayMornings, At(Sunday.AddDays(3), 9), At(Sunday.AddDays(3), 10), allDay: false));
        Assert.False(AwayRules.Covers(sundayMornings, At(Sunday.AddDays(-7), 9), At(Sunday.AddDays(-7), 10), allDay: false));
        Assert.Equal(AwayRules.AwayThenFlag, AwayRules.FlagFor([sundayMornings], At(Sunday, 9), At(Sunday, 10), allDay: false));
        Assert.Equal(AwayRules.AwayFlag, AwayRules.FlagFor([sundayMornings, Away(Sunday, Sunday)], At(Sunday, 9), At(Sunday, 10), allDay: false));
    }

    [Fact]
    public void AReminderComesOnceInsideTheMinistrysDaysBeforeAndNotForADecline()
    {
        DateTime starts = Now.AddDays(2);

        Assert.True(RosterRules.ReminderDue(Replies.Accepted, starts, Now, daysBefore: 2, remindedAt: null));
        Assert.False(RosterRules.ReminderDue(Replies.Accepted, starts, Now, daysBefore: 1, remindedAt: null));
        Assert.False(RosterRules.ReminderDue(Replies.Accepted, starts, Now, daysBefore: 2, remindedAt: Now));
        Assert.False(RosterRules.ReminderDue(Replies.Declined, starts, Now, daysBefore: 2, remindedAt: null));
        Assert.False(RosterRules.ReminderDue(Replies.Accepted, starts, Now, daysBefore: 0, remindedAt: null));
    }

    [Fact]
    public void ANudgeComesOnceForAnUnansweredDateAfterTheMinistrysDays()
    {
        DateTime starts = Now.AddDays(10);

        Assert.True(RosterRules.NudgeDue(Replies.Asked, Now.AddDays(-3), starts, Now, afterDays: 3, nudgedAt: null));
        Assert.False(RosterRules.NudgeDue(Replies.Asked, Now.AddDays(-2), starts, Now, afterDays: 3, nudgedAt: null));
        Assert.False(RosterRules.NudgeDue(Replies.Accepted, Now.AddDays(-3), starts, Now, afterDays: 3, nudgedAt: null));
        Assert.False(RosterRules.NudgeDue(Replies.Asked, Now.AddDays(-3), starts, Now, afterDays: 3, nudgedAt: Now));
    }

    private static RosterDate Date(params (string Person, SwapLine? Swap)[] on) => new()
    {
        Id = "d", StartsAt = Later,
        Assignments = on.Select((each, index) => new Assignment { Id = $"a{index}", PersonId = each.Person, Swap = each.Swap }).ToList(),
    };

    private static AwaySpan Away(DateOnly from, DateOnly to) => new() { From = Wire(from), To = Wire(to) };

    private static AwaySave Save(DateOnly first, DateOnly? last, int[]? weekly = null, string from = "", string until = "") => new()
    {
        From = Wire(first), To = last is { } day ? Wire(day) : "", Weekly = weekly is not null, Weekdays = [.. weekly ?? []], StartTime = from, EndTime = until,
    };

    private static DateTime At(DateOnly day, int hour, int minute = 0) => day.ToDateTime(new TimeOnly(hour, minute));

    private static string Wire(DateOnly day) => day.ToString(AwayRules.DayFormat, System.Globalization.CultureInfo.InvariantCulture);
}
