using GSBC.ChurchApp.Features.Notifications.Contracts;

namespace GSBC.ChurchApp.Features.Notifications.Api.Tests;

public sealed class OneNotificationTests
{
    private const string InSeries = "sermons.series.new", AnySermon = "sermons.new";
    private static readonly string[] MostSpecificFirst = [InSeries, AnySermon];

    [Fact]
    public void SomeoneWithSeveralReasonsGetsTheMostSpecificOnce()
    {
        Assert.Equal(InSeries, Choose(follows: [InSeries, AnySermon], muted: []));
        Assert.Equal(AnySermon, Choose(follows: [AnySermon], muted: []));
        Assert.Null(Choose(follows: [], muted: []));
    }

    [Fact]
    public void TurningOffTheMostSpecificFallsBackToTheNextAndTurningOffEveryReasonSendsNothing()
    {
        Assert.Equal(AnySermon, Choose(follows: [InSeries, AnySermon], muted: [InSeries]));
        Assert.Null(Choose(follows: [InSeries, AnySermon], muted: [InSeries, AnySermon]));
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
        Assert.Empty(OneNotification.OnePerThing(reached, _ => false));
    }

    private static string? Choose(string[] follows, string[] muted) =>
        OneNotification.Choose(MostSpecificFirst, follows.Contains, kind => !muted.Contains(kind));
}
