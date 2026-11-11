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

    [Fact]
    public void EveryUnmetRequirementIsFlaggedInWords()
    {
        PositionRecord cameras = new() { Name = "Cameras", NeedsBlueCard = true, NeedsTraining = true, NeedsTrained = true };
        IReadOnlyList<string> flags = RosterRules.Flags(cameras, Sunday, [new AwayDays(Sunday.AddDays(-1), Sunday)],
            new SafeguardingStanding(Guid.NewGuid(), HoldsBlueCard: false, TrainingCurrent: false), trained: false, alsoOn: ["Sound"]);

        Assert.Equal([RosterRules.AwayFlag, RosterRules.NoBlueCardFlag, RosterRules.NoTrainingFlag, "Not trained for cameras", "Also on sound that day"], flags);
    }

    [Fact]
    public void SomeoneWhoMeetsEverythingHasNoFlags()
    {
        PositionRecord cameras = new() { Name = "Cameras", NeedsBlueCard = true, NeedsTraining = true, NeedsTrained = true };

        Assert.Empty(RosterRules.Flags(cameras, Sunday, [new AwayDays(Sunday.AddDays(1), Sunday.AddDays(3))],
            new SafeguardingStanding(Guid.NewGuid(), HoldsBlueCard: true, TrainingCurrent: true), trained: true, alsoOn: []));
    }

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
        Assert.Null(RosterRules.WhyNotAway(Sunday, Sunday, Sunday));
        Assert.NotNull(RosterRules.WhyNotAway(Sunday, Sunday.AddDays(-1), Sunday.AddDays(-5)));
        Assert.NotNull(RosterRules.WhyNotAway(Sunday.AddDays(-3), Sunday.AddDays(-2), Sunday));
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
}
