using System.Security.Claims;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

[Integration("Staff phones sync only what their role may see: approvers get only wall requests, without contact details or notes; an approval queued offline is applied once")]
public sealed class StaffPhoneTests : PrayerHost
{
    private static readonly Guid Phone = Guid.CreateVersion7();

    [Fact]
    public async Task APrayerWallApproversPhoneNeverReceivesARequestOnlyForThePastoralTeam()
    {
        string forWall = await SendAsync(Phone, null, "For the wall", forWall: true);
        string onlyPastors = await SendAsync(Phone, null, "Only the pastors");

        Assert.Equal([forWall], (await LiveRowsAsync(PrayerFeeds.Approving)).Select(row => row.RecordId));
        Assert.DoesNotContain(await RowsAsync(PrayerFeeds.Approving), row => row.RecordId == onlyPastors);
        Assert.Equal([forWall, onlyPastors], (await LiveRowsAsync(PrayerFeeds.Care)).Select(row => row.RecordId));
    }

    [Fact]
    public async Task AnApproverSeesNeitherTheirContactDetailsNorTheirFullName()
    {
        Guid person = Guid.CreateVersion7();
        People.Names[person] = "Sam Sample";
        await AppAsync(app => app.SendAsync(Phone, person, new PrayerRequestSend
        {
            Id = Guid.NewGuid().ToString(), Text = "For my job interview", ForWall = true, ShowName = true, WantsContact = true, Contact = "0400 000 000",
        }, Cancel));

        WallCase approving = PrayerPayloads.Decode<WallCase>(Assert.Single(await LiveRowsAsync(PrayerFeeds.Approving)).Payload);
        CareCase care = PrayerPayloads.Decode<CareCase>(Assert.Single(await LiveRowsAsync(PrayerFeeds.Care)).Payload);

        Assert.Equal("Sam S.", approving.ShortName);
        Assert.Equal("0400 000 000", care.Contact);
        Assert.Equal("Sam Sample", care.From);
    }

    [Fact]
    public async Task AChildsRequestStillWaitsForApprovalAndTheApproversPhoneSaysTheyAreUnder18()
    {
        Guid child = Guid.CreateVersion7();
        People.Ages[child] = 12;
        await SendAsync(Phone, child, "For my friend at school", forWall: true);

        WallCase waiting = PrayerPayloads.Decode<WallCase>(Assert.Single(await LiveRowsAsync(PrayerFeeds.Approving)).Payload);

        Assert.True(waiting.Minor);
        Assert.Equal(WallState.Waiting, waiting.Wall);
        Assert.Empty(await WallAsync());
    }

    [Fact]
    public async Task AnApprovalQueuedOfflineIsAppliedOnceHoweverOftenItIsSent()
    {
        string id = await SendAsync(Phone, null, "For rain on the farms", forWall: true);
        string queued = Guid.NewGuid().ToString();

        await ShowAsync(id, queuedId: queued);
        await ShowAsync(id, queuedId: queued);

        Assert.Single(await WallAsync());
        Assert.Single(await StepsAsync(id), step => step.StartsWith("Put it on the prayer wall", StringComparison.Ordinal));
        Assert.DoesNotContain(await StepsAsync(id), step => step.StartsWith("Changed it", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnApprovalThatArrivesAfterThePersonTookItDownLeavesItOff()
    {
        string id = await SendAsync(Phone, null, "For my brother", forWall: true);
        await AppAsync(app => app.TakeDownAsync(Phone, null, new RequestRef { Id = id }, Cancel));

        await ShowAsync(id);

        Assert.Empty(await WallAsync());
        Assert.Equal(WallState.TakenDown, (await RecordAsync(id))!.Wall);
        Assert.Empty(await LiveRowsAsync(PrayerFeeds.Approving));
    }

    [Fact]
    public async Task OnceDecidedAndFollowedUpARequestLeavesStaffPhones()
    {
        string id = await SendAsync(Phone, null, "For our marriage", forWall: true);

        await StaffAsync(care => care.KeepPrivateAsync(Staff, new RequestRef { Id = id }, null, Cancel));
        Assert.Empty(await LiveRowsAsync(PrayerFeeds.Approving));
        Assert.Single(await LiveRowsAsync(PrayerFeeds.Care));

        await StaffAsync(care => care.CareAsync(Staff, new CareChange { Id = id, Care = CareState.FollowedUp }, null, Cancel));
        Assert.Empty(await LiveRowsAsync(PrayerFeeds.Care));
    }

    [Fact]
    public async Task RequestsSentBeforeStaffPhonesSyncedReachThemOnce()
    {
        string waiting = await SendAsync(Phone, null, "For the wall", forWall: true);
        string onlyPastors = await SendAsync(Phone, null, "Only the pastors");
        await WithAsync(provider => provider.GetRequiredService<Data.PrayerDbContext>().FeedChanges
            .Where(change => change.Feed == PrayerFeeds.Approving || change.Feed == PrayerFeeds.Care).ExecuteDeleteAsync(Cancel));

        await WithAsync(provider => provider.GetRequiredService<PrayerKeeping>().RunAsync(Cancel));
        await WithAsync(provider => provider.GetRequiredService<PrayerKeeping>().RunAsync(Cancel));

        Assert.Equal([waiting], (await RowsAsync(PrayerFeeds.Approving)).Select(row => row.RecordId));
        Assert.Equal([waiting, onlyPastors], (await RowsAsync(PrayerFeeds.Care)).Select(row => row.RecordId));
    }

    [Fact]
    public async Task APhoneWithoutAPrayerRoleCanSeeNoStaffFeed()
    {
        ServiceCollection services = new();
        services.AddPrayerApi();
        List<FeedDefinition> feeds = services.Select(service => service.ImplementationInstance).OfType<FeedDefinition>().ToList();
        ClaimsPrincipal member = new(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test"));

        foreach (string staffFeed in new[] { PrayerFeeds.Inbox, PrayerFeeds.Approving, PrayerFeeds.Care })
        {
            FeedDefinition feed = feeds.Single(definition => definition.Name == staffFeed);
            Assert.False(await FeedVisibility.CanSeeFeedAsync(feed, member, new Holding(), Cancel));
        }
        Assert.True(await FeedVisibility.CanSeeFeedAsync(feeds.Single(feed => feed.Name == PrayerFeeds.Approving), member, new Holding(PrayerPermissions.WallNode), Cancel));
        Assert.False(await FeedVisibility.CanSeeFeedAsync(feeds.Single(feed => feed.Name == PrayerFeeds.Care), member, new Holding(PrayerPermissions.WallNode), Cancel));
    }

    private async Task<List<string>> StepsAsync(string id) =>
        await WithAsync(provider => Task.FromResult(provider.GetRequiredService<Data.PrayerDbContext>().Steps
            .Where(step => step.RequestId == Guid.Parse(id)).Select(step => step.What).ToList()));

    private sealed class Holding(params string[] nodes) : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(nodes.Contains(node));

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);
    }
}
