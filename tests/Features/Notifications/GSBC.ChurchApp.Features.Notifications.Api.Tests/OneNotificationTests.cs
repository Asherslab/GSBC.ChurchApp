using GSBC.ChurchApp.Features.Notifications.Contracts;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

public sealed class OneNotificationTests
{
    private const string InSeries = "sermons.series.new", AnySermon = "sermons.new";
    private static readonly string[] MostSpecificFirst = [InSeries, AnySermon];

    [Fact]
    public void SomeoneWithSeveralReasonsGetsTheMostSpecificOnce()
    {
        Assert.Equal(new ChosenNotification<string>(InSeries, Push: true), Choose(follows: [InSeries, AnySermon], muted: []));
        Assert.Equal(new ChosenNotification<string>(AnySermon, Push: true), Choose(follows: [AnySermon], muted: []));
        Assert.Null(Choose(follows: [], muted: []));
    }

    [Fact]
    public void MutingTheMostSpecificFallsBackToTheNextAndMutingEveryReasonLeavesItInTheInboxWithoutAPush()
    {
        Assert.Equal(new ChosenNotification<string>(AnySermon, Push: true), Choose(follows: [InSeries, AnySermon], muted: [InSeries]));
        Assert.Equal(new ChosenNotification<string>(InSeries, Push: false), Choose(follows: [InSeries, AnySermon], muted: [InSeries, AnySermon]));
    }

    [Fact]
    public void APhoneNotSignedInShowsOneRowForRelatedNotificationsAndKeepsUnrelatedOnes()
    {
        string oneOf = Guid.CreateVersion7().ToString();
        InboxItem[] reached =
        [
            new() { Id = "a", Kind = AnySermon, OneOf = oneOf, Rank = 1 },
            new() { Id = "b", Kind = InSeries, OneOf = oneOf, Rank = 0 },
            new() { Id = "c", Kind = AnySermon },
        ];

        Assert.Equal(["b", "c"], OneNotification.OnePerThing(reached, _ => true).Select(item => item.Id));
        Assert.Equal(["a", "c"], OneNotification.OnePerThing(reached, kind => kind != InSeries).Select(item => item.Id));
    }

    private static ChosenNotification<string>? Choose(string[] follows, string[] muted) =>
        OneNotification.Choose(MostSpecificFirst, follows.Contains, kind => !muted.Contains(kind));
}
