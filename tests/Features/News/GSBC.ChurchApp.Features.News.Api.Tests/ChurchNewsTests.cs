using Grpc.Core;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Contracts.News;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Api.Tests;

[Integration("Church news reaches readers only once published, through the feed its audience can see, and tells people once")]
public sealed class ChurchNewsTests : NewsHost
{
    [Fact]
    public async Task ADraftNeverReachesReadersAndPublishingPutsItInTheFeedItsAudienceCanSee()
    {
        NewsPost forEveryone = await WriteAsync(Draft());
        NewsPost forMembers = await WriteAsync(Draft("Members' meeting", Audience.SignedIn.Text));

        Assert.Empty(await LiveRowsAsync(NewsFeeds.Posts));
        Assert.Empty(await LiveRowsAsync(NewsFeeds.PrivatePosts));
        Assert.Equal(2, (await LiveRowsAsync(NewsFeeds.Desk)).Count);

        await PublishedAsync(forEveryone);
        await PublishedAsync(forMembers);

        Assert.Equal(["Carols on the lawn"], (await ReadersSeeAsync(NewsFeeds.Posts)).Select(post => post.Title));
        Assert.Equal(Audience.SignedIn.Text, Assert.Single(await LiveRowsAsync(NewsFeeds.PrivatePosts)).ScopeId);
    }

    [Fact]
    public async Task ChangingWhoAPostIsForMovesItBetweenTheFeeds()
    {
        NewsPost post = await PublishedAsync(await WriteAsync(Draft()));

        post.Audience = Audience.SignedIn.Text;
        post.New = false;
        await WriteAsync(post);

        Assert.Empty(await LiveRowsAsync(NewsFeeds.Posts));
        Assert.Single(await LiveRowsAsync(NewsFeeds.PrivatePosts));
    }

    [Fact]
    public async Task PublishingTellsEveryoneItsForOnceOpeningThePostAndChangesDoNotTellAgain()
    {
        NewsPost draft = Draft();
        draft.Urgent = true;
        NewsPost post = await PublishedAsync(await WriteAsync(draft));

        post.Title = "Carols on the lawn: bring a torch";
        post.New = false;
        await WriteAsync(post);
        await PublishedAsync(post);

        Notification told = Assert.Single(Told.Raised);
        Assert.Equal(NewsNotifications.Post, told.Kind);
        Assert.Equal(NotificationAudience.Everyone, told.Audience);
        Assert.Equal($"/news/{post.Id}", told.Link);
        Assert.Equal("Bring a picnic rug.", told.Body);
        Assert.True(told.Urgent);
        Assert.Equal(Pastor, told.Actor);
    }

    [Fact]
    public async Task APostWithTellingOffIsPublishedQuietly()
    {
        await PublishedAsync(await WriteAsync(Draft(tell: false)));

        Assert.Empty(Told.Raised);
        Assert.Single(await LiveRowsAsync(NewsFeeds.Posts));
    }

    [Fact]
    public async Task AWriterCannotPublishButAsksAndPublishingSettlesTheAsk()
    {
        NewsPost post = await WriteAsync(Draft(), Writing);

        await Assert.ThrowsAsync<RpcException>(() => DeskAsync(desk => desk.PublishAsync(Writing, Guid.Parse(post.Id), null, Cancel)));
        await DeskAsync(desk => desk.AskToPublishAsync(Writing, Guid.Parse(post.Id), Cancel));
        Notification waiting = Assert.Single(Told.Raised);
        Assert.Equal(NewsNotifications.WaitingToPublish, waiting.Kind);

        await PublishedAsync(post);

        Assert.Contains(waiting.Subject!, Told.Settled);
        Assert.Single(await LiveRowsAsync(NewsFeeds.Posts));
    }

    [Fact]
    public async Task AWriterCannotChangeOrDeleteWhatIsAlreadyPublished()
    {
        NewsPost post = await PublishedAsync(await WriteAsync(Draft(), Writing));
        post.New = false;

        await Assert.ThrowsAsync<RpcException>(() => WriteAsync(post, Writing));
        await Assert.ThrowsAsync<RpcException>(() => DeskAsync(desk => desk.DeleteAsync(Writing, Guid.Parse(post.Id), Cancel)));
    }

    [Fact]
    public async Task AScheduledPostStaysOutOfSightUntilItsTimeThenGoesOutOnceEvenWithTwoServers()
    {
        NewsPost post = await WriteAsync(Draft());
        DateTime at = Clock.GetUtcNow().UtcDateTime.AddHours(2);
        await DeskAsync(desk => desk.PublishAsync(Publishing, Guid.Parse(post.Id), at, Cancel));

        await DeskAsync(desk => desk.PublishDueAsync(Cancel));
        Assert.Empty(await LiveRowsAsync(NewsFeeds.Posts));

        Clock.Advance(TimeSpan.FromHours(2));
        await Task.WhenAll(DeskAsync(desk => desk.PublishDueAsync(Cancel)), DeskAsync(desk => desk.PublishDueAsync(Cancel)));

        Assert.Single(await LiveRowsAsync(NewsFeeds.Posts));
        Assert.Single(Told.Raised);
    }

    [Fact]
    public async Task DeletingAPublishedPostTakesItFromEveryPhoneAndRecallsItsNotification()
    {
        NewsPost post = await PublishedAsync(await WriteAsync(Draft()));

        await DeskAsync(desk => desk.DeleteAsync(Publishing, Guid.Parse(post.Id), Cancel));

        Assert.Empty(await LiveRowsAsync(NewsFeeds.Posts));
        Assert.Empty(await LiveRowsAsync(NewsFeeds.Desk));
        Assert.Contains(NewsTelling.Subject(Guid.Parse(post.Id)), Told.Recalled);
    }

    [Fact]
    public async Task APhoneSendingTheSameNewPostTwiceMakesOnePost()
    {
        NewsPost draft = Draft();

        await WriteAsync(draft);
        await WriteAsync(draft);

        Assert.Single(await LiveRowsAsync(NewsFeeds.Desk));
    }

    [Fact]
    public async Task ThePostsPicturesAreTheLibrarysAndItsFirstPictureLeadsIt()
    {
        NewsPost draft = Draft();
        draft.Body = new RichText
        {
            Markdown = "![The lawn](media:lawn \"Last year\")\n\nBring a rug.\n\n![Form](media:form)",
            Media = [new MediaRef { Id = "lawn", ContentType = "image/jpeg", Url = "https://elsewhere.example/x.jpg" }, new MediaRef { Id = "form", ContentType = "application/pdf" }],
        };

        NewsPost post = await PublishedAsync(await WriteAsync(draft));

        NewsPost read = Assert.Single(await ReadersSeeAsync(NewsFeeds.Posts));
        Assert.Equal("https://media.example/lawn", read.Picture?.Url);
        Assert.Equal(["lawn", "form"], Media.Uses[NewsDesk.MediaOwner(Guid.Parse(post.Id))]);
    }

    [Fact]
    public async Task DeletingAnAccountForgetsTheirNameOnThePostsTheyWrote()
    {
        await PublishedAsync(await WriteAsync(Draft(), Writing));

        await WithAsync(async provider =>
        {
            await provider.GetRequiredService<NewsPersonData>().ForgetAsync(Office, Cancel);
            return true;
        });

        Assert.Equal("", Assert.Single(await ReadersSeeAsync(NewsFeeds.Posts)).Author);
    }
}

[Integration("A ministry's post goes into church news when staff share it or answer its leaders' ask, follows the ministry's edits, and leaves when withdrawn")]
public sealed class SharedMinistryPostTests : NewsHost
{
    private static readonly Guid KidsMinistry = Guid.CreateVersion7();
    private static readonly Guid PostId = Guid.CreateVersion7();

    private SharedPost Post(string title = "Holiday program volunteers needed") => new(
        ChurchNews.FromMinistries, PostId, KidsMinistry, "Kids ministry", title, new RichText { Markdown = "Three mornings in January." }, "Kids Leader",
        Clock.GetUtcNow().UtcDateTime.AddHours(-1), null, $"/ministries/{KidsMinistry}/news");

    [Fact]
    public async Task ALeadersAskWaitsForStaffAndAYesPutsItInChurchNewsSayingWhereItCameFrom()
    {
        await SharesAsync(shares => shares.AskAsync(Post(), Leader, Cancel));

        Assert.Empty(await LiveRowsAsync(NewsFeeds.Posts));
        NewsPost asked = Assert.Single(await ReadersSeeAsync(NewsFeeds.Desk));
        Assert.Equal((NewsState.Asked, "Kids Leader"), (asked.State, asked.AskedBy));
        Assert.Equal(NewsNotifications.Asked, Assert.Single(Told.Raised).Kind);

        await DeskAsync(desk => desk.AnswerAsync(Publishing, Guid.Parse(asked.Id), yes: true, tell: true, Cancel));

        NewsPost shared = Assert.Single(await ReadersSeeAsync(NewsFeeds.Posts));
        Assert.Equal(("Kids ministry", $"/ministries/{KidsMinistry}/news", true), (shared.From, shared.FromLink, shared.Shared));
        Assert.Contains(Told.Raised, told => told.Kind == NewsNotifications.Post);
        Assert.Contains(Told.Raised, told => told.Kind == NewsNotifications.Answered && told.Audience == NotificationAudience.Person(Leader));
        Assert.Equal(SharedState.Shared, await StateAsync());
    }

    [Fact]
    public async Task SayingNoKeepsItInTheMinistryOnly()
    {
        await SharesAsync(shares => shares.AskAsync(Post(), Leader, Cancel));
        string id = Assert.Single(await ReadersSeeAsync(NewsFeeds.Desk)).Id;

        await DeskAsync(desk => desk.AnswerAsync(Publishing, Guid.Parse(id), yes: false, tell: false, Cancel));

        Assert.Empty(await LiveRowsAsync(NewsFeeds.Posts));
        Assert.Equal(SharedState.Declined, await StateAsync());
    }

    [Fact]
    public async Task TheMinistrysEditsFlowThroughAndChurchNewsCannotChangeIt()
    {
        await SharesAsync(shares => shares.ShareAsync(Post(), Pastor, tellPeople: false, Cancel));

        await SharesAsync(shares => shares.ChangedAsync(Post("Holiday program: two more helpers needed"), Cancel));

        NewsPost shared = Assert.Single(await ReadersSeeAsync(NewsFeeds.Posts));
        Assert.Equal("Holiday program: two more helpers needed", shared.Title);
        shared.New = false;
        await Assert.ThrowsAsync<RpcException>(() => WriteAsync(shared));
    }

    [Fact]
    public async Task WithdrawingItTakesItOutOfChurchNewsAndRecallsItsNotification()
    {
        await SharesAsync(shares => shares.ShareAsync(Post(), Pastor, tellPeople: true, Cancel));
        string id = Assert.Single(await ReadersSeeAsync(NewsFeeds.Posts)).Id;

        await SharesAsync(shares => shares.WithdrawAsync(ChurchNews.FromMinistries, PostId, Cancel));

        Assert.Empty(await LiveRowsAsync(NewsFeeds.Posts));
        Assert.Contains(NewsTelling.Subject(Guid.Parse(id)), Told.Recalled);
        Assert.Equal(SharedState.None, await StateAsync());
    }

    private Task<SharedState> StateAsync() => WithAsync(async provider =>
    {
        IChurchNews shares = provider.GetRequiredService<IChurchNews>();
        return (await shares.StatesAsync(ChurchNews.FromMinistries, [PostId], Cancel)).GetValueOrDefault(PostId);
    });
}
