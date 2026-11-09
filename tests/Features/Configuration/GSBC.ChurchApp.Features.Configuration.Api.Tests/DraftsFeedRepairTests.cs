using System.Security.Claims;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

[Integration("Phones list the drafts the database has: a draft removed without telling the feed comes off phones")]
public sealed class DraftsFeedRepairTests : ConfigurationHost
{
    [Fact]
    public async Task ADraftNoLongerInTheDatabaseComesOffTheDraftsFeed()
    {
        ClaimsPrincipal author = Access.Person(ArrangesHub);
        ClaimsPrincipal publisher = Access.Person(SeesEveryDraft);
        Draft kept = await NewDraftAsync(author, "Kept");
        Draft lost = await NewDraftAsync(author, "Lost");
        await WithDbAsync(db => db.Drafts.Where(draft => draft.Id == Guid.Parse(lost.Id)).ExecuteDeleteAsync(Cancel));

        int removed = await WithDbAsync(db => DraftsFeedRepair.RemoveMissingAsync(db, Cancel));
        int again = await WithDbAsync(db => DraftsFeedRepair.RemoveMissingAsync(db, Cancel));

        Assert.Equal(1, removed);
        Assert.Equal(0, again);
        FeedBatch phone = Assert.Single((await PullAsync(publisher, ConfigurationFeeds.Drafts, 0)).Feeds);
        Assert.Equal([kept.Id], phone.Changes.Where(change => !change.Deleted).Select(change => change.Id));
    }
}
