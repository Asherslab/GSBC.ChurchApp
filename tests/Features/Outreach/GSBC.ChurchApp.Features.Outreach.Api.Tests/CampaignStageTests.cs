using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

[Integration("A campaign's stage and telling the church are kept in Postgres and its feed")]
public sealed class CampaignStageTests : OutreachHost
{
    [Fact]
    public async Task TheChurchIsToldOnceWhenACampaignWrapsUp()
    {
        string id = await DatedAsync("Carols", ChurchTime.DayOf(Clock.GetUtcNow().UtcDateTime).AddDays(-3), ChurchTime.DayOf(Clock.GetUtcNow().UtcDateTime).AddDays(20));

        await MoveAsync(id, CampaignStage.WrappingUp);
        await MoveAsync(id, CampaignStage.Ongoing);
        await MoveAsync(id, CampaignStage.WrappingUp);
        Clock.Advance(TimeSpan.FromDays(14));
        await KeepAsync();

        Notification told = Assert.Single(Told.Raised);
        Assert.Equal(OutreachNotifications.EndingSoon, told.Kind);
        Assert.Equal(NotificationAudience.Everyone, told.Audience);
        Assert.Equal("Carols ends on Fri 4 Dec", told.Title);
    }

    [Fact]
    public async Task KeepingMovesACampaignOnItsDatesAndTellsTheChurch()
    {
        DateOnly today = ChurchTime.DayOf(Clock.GetUtcNow().UtcDateTime);
        string id = await DatedAsync("Carols", today.AddDays(2), today.AddDays(12));
        Assert.Equal(CampaignStage.Preparing, (await StoredAsync(id)).Stage);

        Clock.Advance(TimeSpan.FromDays(2));
        await KeepAsync();
        Assert.Equal(CampaignStage.Ongoing, (await StoredAsync(id)).Stage);
        Assert.Empty(Told.Raised);

        Clock.Advance(TimeSpan.FromDays(3));
        await KeepAsync();
        Assert.Equal(CampaignStage.WrappingUp, (await StoredAsync(id)).Stage);
        Assert.Equal(CampaignStage.WrappingUp, Assert.Single(await FeedAsync<Campaign>(OutreachFeeds.Campaigns), campaign => campaign.Id == id).Stage);
        Assert.Single(Told.Raised);

        Clock.Advance(TimeSpan.FromDays(8));
        await KeepAsync();
        Assert.Equal(CampaignStage.Finished, (await StoredAsync(id)).Stage);
        Assert.Single(Told.Raised);
    }

    [Fact]
    public async Task AFinishedCampaignKeepsItsDatesButTakesTheBundlesMade()
    {
        DateOnly today = ChurchTime.DayOf(Clock.GetUtcNow().UtcDateTime);
        string id = await DatedAsync("Easter", today.AddDays(-30), today.AddDays(-2));
        Campaign finished = OutreachViews.Campaign(await StoredAsync(id));

        await Assert.ThrowsAnyAsync<Grpc.Core.RpcException>(() => PlanAsync(planning =>
            planning.SaveCampaignAsync(new CampaignSave { Campaign = Copy(finished, campaign => campaign.EndsOn = OutreachRules.Format(today.AddDays(5))) }, Cancel)));
        await PlanAsync(planning => planning.SaveCampaignAsync(new CampaignSave { Campaign = Copy(finished, campaign => campaign.BundlesMade = 80) }, Cancel));

        Assert.Equal((CampaignStage.Finished, 80), ((await StoredAsync(id)).Stage, (await StoredAsync(id)).BundlesMade));
    }

    private static Campaign Copy(Campaign campaign, Action<Campaign> change)
    {
        Campaign copy = OutreachPayloads.Decode<Campaign>(OutreachPayloads.Encode(campaign));
        change(copy);
        return copy;
    }

    private async Task<string> DatedAsync(string name, DateOnly starts, DateOnly ends)
    {
        string id = Guid.CreateVersion7().ToString();
        await PlanAsync(planning => planning.SaveCampaignAsync(new CampaignSave
        {
            Campaign = new Campaign { Id = id, Name = name, StartsOn = OutreachRules.Format(starts), EndsOn = OutreachRules.Format(ends), WrapUpDays = 7 },
        }, Cancel));
        return id;
    }

    private Task MoveAsync(string id, CampaignStage stage) =>
        PlanAsync(planning => planning.MoveCampaignAsync(new CampaignMove { Id = id, Stage = stage }, Cancel));

    private async Task<CampaignRecord> StoredAsync(string id) =>
        Assert.Single(await RecordsAsync(db => db.Campaigns.Where(campaign => campaign.Id == Guid.Parse(id))));
}
