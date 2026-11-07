using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class FollowingTests
{
    [Fact]
    public void NothingAboutACalendarForSignedInPeopleReachesASignedOutPhone()
    {
        CalendarRecord kidsTeam = new() { Id = Guid.CreateVersion7(), Name = "Kids team", Slug = CalendarLinks.SlugOf("Kids team"), Audience = Audience.SignedIn.Text };

        Assert.Equal(StatusCode.Unauthenticated, CalendarFollowing.WhyNotAdd(kidsTeam, signedIn: false, Viewer.Anonymous)?.StatusCode);
        Assert.Null(CalendarFollowing.WhyNotAdd(kidsTeam, signedIn: true, new Viewer(true, _ => false)));
        Assert.Equal([NotificationAudience.SignedIn], EventEditingService.Tellable(kidsTeam));
    }
}
