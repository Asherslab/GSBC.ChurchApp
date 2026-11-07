using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class ListeningProgressTests
{
    private const string Sermon = "sermon";
    private static readonly DateTime Start = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void EveryDeviceEndsTheSameWhateverOrderEntriesArriveInAndHoweverOften()
    {
        Random random = new(20261104);
        for (int round = 0; round < 200; round++)
        {
            List<ListeningEntry> entries = Enumerable.Range(0, 5).Select(_ => RandomEntry(random)).ToList();
            ListeningEntry inOrder = entries.Aggregate(ListeningProgress.Merge);
            ListeningEntry shuffled = entries.OrderBy(_ => random.Next()).Aggregate(ListeningProgress.Merge);
            ListeningEntry twice = entries.Concat(entries).OrderBy(_ => random.Next()).Aggregate(ListeningProgress.Merge);
            ListeningEntry grouped = ListeningProgress.Merge(entries.Take(2).Aggregate(ListeningProgress.Merge), entries.Skip(2).Aggregate(ListeningProgress.Merge));

            Assert.Equivalent(inOrder, shuffled);
            Assert.Equivalent(inOrder, twice);
            Assert.Equivalent(inOrder, grouped);
        }
    }

    [Fact]
    public void TheMostRecentPositionWinsAndAnOlderDeviceNeverDragsYouBack()
    {
        ListeningEntry phone = ListeningProgress.Heard(Sermon, 1500, 2400, Start.AddHours(2));
        ListeningEntry oldTablet = ListeningProgress.Heard(Sermon, 2000, 2400, Start);
        ListeningEntry restartedOnLaptop = ListeningProgress.Heard(Sermon, 20, 2400, Start.AddHours(3));

        Assert.Equal(1500, ListeningProgress.Merge(oldTablet, phone).Position);
        Assert.Equal(20, ListeningProgress.Merge(ListeningProgress.Merge(phone, oldTablet), restartedOnLaptop).Position);
    }

    [Fact]
    public void FinishedAndWatchedAreStickyUntilMarkedNotPlayed()
    {
        ListeningEntry finished = ListeningProgress.Heard(Sermon, 2390, 2400, Start);
        ListeningEntry watched = ListeningProgress.Watched(Sermon, Start.AddMinutes(1));
        ListeningEntry listeningAgain = ListeningProgress.Heard(Sermon, 300, 2400, Start.AddHours(1));
        ListeningEntry merged = new[] { finished, watched, listeningAgain }.Aggregate(ListeningProgress.Merge);
        Assert.True(ListeningProgress.IsFinished(merged));
        Assert.True(ListeningProgress.IsWatched(merged));
        Assert.False(ListeningProgress.IsUnderway(merged));

        ListeningEntry cleared = ListeningProgress.Merge(merged, ListeningProgress.NotPlayed(Sermon, Start.AddHours(2)));
        Assert.False(ListeningProgress.IsFinished(cleared));
        Assert.False(ListeningProgress.IsWatched(cleared));
        Assert.Equal(0, cleared.Position);

        ListeningEntry finishedLater = ListeningProgress.Merge(cleared, ListeningProgress.Played(Sermon, Start.AddHours(3)));
        Assert.True(ListeningProgress.IsFinished(finishedLater));
    }

    [Theory]
    [InlineData(2369, 2400, false)]
    [InlineData(2371, 2400, true)]
    [InlineData(583, 600, true)]
    [InlineData(580, 600, false)]
    [InlineData(10, 0, false)]
    public void ReachingTheLastThirtySecondsCountsAsFinished(double position, double duration, bool finished) =>
        Assert.Equal(finished, ListeningProgress.ReachesTheEnd(position, duration));

    [Fact]
    public void ATimeFromAPhoneWhoseClockRunsFastIsHeldToAFewMinutesAhead()
    {
        ListeningEntry clamped = SermonsService.Clamped(ListeningProgress.Heard(Sermon, 900, 2400, Start.AddDays(3)), Start);

        Assert.Equal(Start + SermonsService.ClockSlack, clamped.HeardAt);
        Assert.Equal(Start.AddMinutes(-5), SermonsService.Clamped(ListeningProgress.Heard(Sermon, 900, 2400, Start.AddMinutes(-5)), Start).HeardAt);
    }

    private static ListeningEntry RandomEntry(Random random) => new()
    {
        SermonId = Sermon,
        Position = random.Next(0, 4) * 300,
        Duration = 2400,
        HeardAt = Maybe(random),
        FinishedAt = Maybe(random),
        WatchedAt = Maybe(random),
        ClearedAt = Maybe(random),
    };

    private static DateTime? Maybe(Random random) => random.Next(3) == 0 ? null : Start.AddMinutes(random.Next(0, 6) * 10);
}
