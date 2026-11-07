using System.Security.Claims;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

[Integration("The drafts feed reaches only the people a draft is shared with, and unsharing resets their copy")]
public sealed class DraftFeedTests : ConfigurationHost
{
    [Fact]
    public async Task OnlyPeopleADraftIsSharedWithReceiveItAndUnsharingTakesItOffTheirPhone()
    {
        ClaimsPrincipal author = Access.Person(ArrangesHub);
        ClaimsPrincipal pastor = Access.Person();
        ClaimsPrincipal member = Access.Person();
        ClaimsPrincipal publisher = Access.Person(SeesEveryDraft);
        Draft draft = await NewDraftAsync(author);
        await SaveAsync(author, draft, LinkTile("Christmas"));
        await Drafts(editor => editor.ShareAsync(author, new ShareRequest { DraftId = draft.Id, UserId = FakeAccess.IdOf(pastor).ToString() }, sharing: true, Cancel));

        FeedBatch pastorsPhone = Assert.Single((await PullAsync(pastor, ConfigurationFeeds.Drafts, 0)).Feeds);
        Assert.Equal([draft.Id], pastorsPhone.Changes.Select(change => change.Id));
        Assert.Equal([draft.Id], Assert.Single((await PullAsync(publisher, ConfigurationFeeds.Drafts, 0)).Feeds).Changes.Select(change => change.Id));
        Assert.Empty(Assert.Single((await PullAsync(member, ConfigurationFeeds.Drafts, 0)).Feeds).Changes);

        await Drafts(editor => editor.ShareAsync(author, new ShareRequest { DraftId = draft.Id, UserId = FakeAccess.IdOf(pastor).ToString() }, sharing: false, Cancel));

        FeedBatch afterUnsharing = Assert.Single((await PullAsync(pastor, ConfigurationFeeds.Drafts, pastorsPhone.Cursor)).Feeds);
        Assert.True(afterUnsharing.Reset);
        Assert.Empty(Assert.Single((await PullAsync(pastor, ConfigurationFeeds.Drafts, 0)).Feeds).Changes);
    }
}
