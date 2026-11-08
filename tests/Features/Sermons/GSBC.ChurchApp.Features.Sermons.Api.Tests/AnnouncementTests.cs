using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class AnnouncementTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ANewSermonInASeriesIsOneNotificationTellingSeriesFollowersFirstAndEveryoneFollowingSermonsOtherwise()
    {
        string harbour = Guid.CreateVersion7().ToString();
        Sermon sermon = new() { Id = Guid.CreateVersion7().ToString(), Title = "Safe Harbour: Part 5", PreachedAt = Now.AddDays(-1), SpeakerName = "Jo Ellery", SeriesId = harbour, SeriesName = "Safe Harbour" };

        IReadOnlyList<Notification> mostSpecificFirst = SermonAnnouncer.Announcements(sermon, Now);

        Assert.Equal([(SermonsPermissions.HearSeries.Node, harbour), (SermonsPermissions.HearNewNode, null)], mostSpecificFirst.Select(Audience));
        Assert.Equal([harbour, SermonsNotifications.AllSermonsScope], mostSpecificFirst.Select(notification => notification.ScopeId));
        Assert.All(mostSpecificFirst, notification => Assert.Equal($"sermon/{sermon.Id}", notification.Subject));
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
