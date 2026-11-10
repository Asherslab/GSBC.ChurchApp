using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

[Integration("A request's private feed rows carry only its person's id, so no other phone syncs them, and the inbox feed carries markers without the words")]
public sealed class PrivacyTests : PrayerHost
{
    [Fact]
    public async Task APrivateRequestReachesOnlyItsPersonsPhones()
    {
        Guid person = Guid.CreateVersion7();
        string id = await SendAsync(Guid.CreateVersion7(), person, "Something only the pastors should know");

        Assert.Empty(await RowsAsync(PrayerFeeds.Wall));
        Assert.All(await RowsAsync(PrayerFeeds.Mine), row => Assert.Equal(person.ToString(), row.ScopeId));
        Assert.Equal(id, PrayerPayloads.Decode<YourRequest>(Assert.Single(await LiveRowsAsync(PrayerFeeds.Mine)).Payload).Id);
        Assert.All(await RowsAsync(PrayerFeeds.Inbox), row => Assert.Empty(row.Payload));
    }

    [Fact]
    public async Task ARequestSentWithoutSigningInIsNotInAnyPersonsFeed()
    {
        await SendAsync(Guid.CreateVersion7(), null, "From a phone", forWall: true);

        Assert.Empty(await RowsAsync(PrayerFeeds.Mine));
    }

    [Fact]
    public async Task SomeoneWhoOnlyLooksAfterTheWallSeesOnlyRequestsThatAskForIt()
    {
        string asked = await SendAsync(Guid.CreateVersion7(), null, "For the wall", forWall: true);
        await SendAsync(Guid.CreateVersion7(), null, "Only the pastors");

        Inbox wallOnly = await WithAsync(provider => provider.GetRequiredService<PastoralCare>().InboxAsync(mayRead: false, "", Cancel));
        Inbox pastoral = await WithAsync(provider => provider.GetRequiredService<PastoralCare>().InboxAsync(mayRead: true, "", Cancel));

        Assert.Equal([asked], wallOnly.Requests.Select(line => line.Id));
        Assert.Equal(2, pastoral.Requests.Count);
    }
}
