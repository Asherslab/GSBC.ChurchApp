using System.Security.Claims;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Api.Tests;

[Integration("A post opened from a link in a browser is shown only to someone its audience includes, and only once it's published")]
public sealed class OpeningAPostTests : NewsHost
{
    private static readonly Guid Elders = Guid.CreateVersion7();
    private static readonly ClaimsPrincipal SignedOut = new();

    [Fact]
    public async Task SomeoneSignedOutIsShownOnlyAPublishedPostForEveryone()
    {
        NewsPost forEveryone = await PublishedAsync(await WriteAsync(Draft()));
        NewsPost forMembers = await PublishedAsync(await WriteAsync(Draft("Members' meeting", Audience.SignedIn.Text)));
        NewsPost forElders = await PublishedAsync(await WriteAsync(Draft("Elders' retreat", Audience.HoldingRole(Elders).Text)));
        NewsPost draft = await WriteAsync(Draft("Not ready yet"));

        Assert.Equal("Carols on the lawn", (await ReadAsync(SignedOut, forEveryone))?.Title);
        Assert.Null(await ReadAsync(SignedOut, forMembers));
        Assert.Null(await ReadAsync(SignedOut, forElders));
        Assert.Null(await ReadAsync(SignedOut, draft));
    }

    [Fact]
    public async Task APostForARoleIsShownToSomeoneInTheRoleAndNotToEveryoneSignedIn()
    {
        NewsPost forElders = await PublishedAsync(await WriteAsync(Draft("Elders' retreat", Audience.HoldingRole(Elders).Text)));

        Assert.Null(await ReadAsync(Grants.Person(), forElders));
        Assert.Equal("Elders' retreat", (await ReadAsync(Grants.Person(Elders), forElders))?.Title);
    }

    [Fact]
    public async Task AnArchivedPostIsNoLongerShownAndTheReaderNeverSeesTheNotificationWords()
    {
        NewsPost draft = Draft();
        draft.TellTitle = "Only for the notification";
        NewsPost post = await PublishedAsync(await WriteAsync(draft));

        Assert.Equal("", (await ReadAsync(SignedOut, post))?.TellTitle);
        await DeskAsync(desk => desk.ArchiveAsync(Publishing, Guid.Parse(post.Id), Cancel));
        Assert.Null(await ReadAsync(SignedOut, post));
    }

    private Task<NewsPost?> ReadAsync(ClaimsPrincipal reader, NewsPost post) =>
        WithAsync(provider => provider.GetRequiredService<NewsReader>().ReadAsync(reader, post.Id, Cancel));
}
