using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class AnnouncementTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ANewSermonIsToldToEveryoneFollowingSermonsAndAgainToFollowersOfItsSeries()
    {
        string harbour = Guid.CreateVersion7().ToString();
        Sermon sermon = new() { Id = Guid.CreateVersion7().ToString(), Title = "Safe Harbour: Part 5", PreachedAt = Now.AddDays(-1), SpeakerName = "Jo Ellery", SeriesId = harbour, SeriesName = "Safe Harbour" };

        IReadOnlyList<Notification> told = SermonAnnouncer.Announcements(sermon, Now);

        Assert.Equal(2, told.Count);
        Assert.Equal((SermonsPermissions.HearNewNode, null), Audience(told[0]));
        Assert.Equal(SermonsNotifications.AllSermonsScope, told[0].ScopeId);
        Assert.Equal((SermonsPermissions.HearSeries.Node, harbour), Audience(told[1]));
        Assert.Equal(harbour, told[1].ScopeId);
        Assert.All(told, notification => Assert.StartsWith("/sermon/", notification.Link));
        Assert.Single(SermonAnnouncer.Announcements(new Sermon { Id = sermon.Id, Title = "Walking In The Light", PreachedAt = Now }, Now));
    }

    [Fact]
    public void ASermonOlderThanAFewDaysIsNotAnnounced() =>
        Assert.Empty(SermonAnnouncer.Announcements(new Sermon { Id = Guid.CreateVersion7().ToString(), PreachedAt = Now - SermonAnnouncer.NewFor - TimeSpan.FromMinutes(1) }, Now));

    private static (string Node, string? Scope) Audience(Notification notification)
    {
        NotificationAudience.HoldingAudience audience = Assert.IsType<NotificationAudience.HoldingAudience>(notification.Audience);
        return (audience.Permission.Node, audience.ObjectId);
    }
}
