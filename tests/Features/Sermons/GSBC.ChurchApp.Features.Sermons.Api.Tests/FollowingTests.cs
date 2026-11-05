using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class FollowingTests : SermonsHost
{
    [Fact]
    public async Task OnlyFollowersHearOfANewSermonOnceAndTheFirstReadAnnouncesNothing()
    {
        string first = Sample("feed.rss");
        Website.ServeFeed(first[..first.IndexOf("    <item>\n      <title>Walking In The Light", StringComparison.Ordinal)]
            + first[first.IndexOf("    <item>\n      <title>Safe Harbour: Part 4", StringComparison.Ordinal)..]);
        await ReadTheWebsiteAsync();
        Assert.Empty(Notifications.Raised);

        Website.FeedETag = "W/\"two\"";
        Website.ServeFeed(first.Replace("<title>Walking In The Light</title>", "<title>Safe Harbour: Part 5 - Harbour Of Joy</title>")
            .Replace("/2026-11-04-walking-in-the-light<", "/2026-11-04-walking-in-the-light<"));
        await ReadTheWebsiteAsync();
        await ReadTheWebsiteAsync(force: true);

        Notification raised = Assert.Single(Notifications.Raised);
        NotificationAudience.HoldingAudience audience = Assert.IsType<NotificationAudience.HoldingAudience>(raised.Audience);
        Assert.Equal(SermonsPermissions.HearNewNode, audience.Permission.Node);
        Assert.Equal(SermonsNotifications.AllSermonsScope, raised.ScopeId);
        Assert.StartsWith("/sermon/", raised.Link);
    }

    [Fact]
    public async Task AFollowWithoutSigningInBelongsToThePhoneAndASignedInFollowToThePerson()
    {
        await ReadTheWebsiteAsync();
        string harbour = (await CatalogueAsync()).First(sermon => sermon.SeriesName == "Safe Harbour").SeriesId!;
        Guid phone = Guid.CreateVersion7(), person = Guid.CreateVersion7();

        Follows phoneFollows = await WithAsync(provider => provider.GetRequiredService<SermonFollowing>()
            .FollowAsync(phone, null, new FollowRequest { SeriesId = harbour, Follow = true }, Cancel));
        Follows personFollows = await WithAsync(provider => provider.GetRequiredService<SermonFollowing>()
            .FollowAsync(phone, person, new FollowRequest { Follow = true }, Cancel));

        Assert.Equal([harbour], phoneFollows.SeriesIds);
        Assert.False(phoneFollows.NewSermons);
        Assert.True(personFollows.NewSermons);
        Assert.Contains((phone, SermonsPermissions.SeriesFollowerRole, (string?)harbour, true), Roles.Held);
        Assert.Contains((person, SermonsPermissions.FollowerRole, (string?)null, false), Roles.Held);

        await WithAsync(provider => provider.GetRequiredService<SermonFollowing>()
            .FollowAsync(phone, null, new FollowRequest { SeriesId = harbour, Follow = false }, Cancel));
        Assert.DoesNotContain(Roles.Held, held => held.Holder == phone);
    }
}
