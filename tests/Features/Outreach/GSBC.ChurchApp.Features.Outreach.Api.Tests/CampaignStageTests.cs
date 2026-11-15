using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

public sealed class CampaignStageTests : OutreachHost
{
    private static readonly DateOnly Starts = new(2026, 12, 1);
    private static readonly DateOnly Ends = new(2026, 12, 20);

    [Theory]
    [InlineData("2026-11-30", CampaignStage.Preparing)]
    [InlineData("2026-12-01", CampaignStage.Ongoing)]
    [InlineData("2026-12-12", CampaignStage.Ongoing)]
    [InlineData("2026-12-13", CampaignStage.WrappingUp)]
    [InlineData("2026-12-20", CampaignStage.WrappingUp)]
    [InlineData("2026-12-21", CampaignStage.Finished)]
    public void ItsDatesMoveACampaignAlong(string today, CampaignStage expected) =>
        Assert.Equal(expected, CampaignStages.Initial(Starts, Ends, 7, DateOnly.Parse(today)));

    [Fact]
    public void AMoveByHandHoldsUntilTheNextDateComes()
    {
        DateOnly movedBack = new(2026, 12, 5);

        Assert.Equal(CampaignStage.Preparing, CampaignStages.Advance(CampaignStage.Preparing, movedBack, Starts, Ends, 7, new DateOnly(2026, 12, 12)));
        Assert.Equal(CampaignStage.WrappingUp, CampaignStages.Advance(CampaignStage.Preparing, movedBack, Starts, Ends, 7, new DateOnly(2026, 12, 13)));
        Assert.Equal(CampaignStage.Ongoing, CampaignStages.Advance(CampaignStage.Ongoing, new DateOnly(2026, 12, 14), Starts, Ends, 7, new DateOnly(2026, 12, 20)));
    }

    [Theory]
    [InlineData(CampaignStage.Preparing, false)]
    [InlineData(CampaignStage.Ongoing, true)]
    [InlineData(CampaignStage.WrappingUp, true)]
    [InlineData(CampaignStage.Finished, false)]
    public void WalkersCarryOnlyCampaignsThatAreOn(CampaignStage stage, bool carried)
    {
        Campaign campaign = new() { Stage = stage, StageSetOn = "2026-11-14" };

        Assert.Equal(carried, OutreachRules.IsActive(campaign, new DateOnly(2026, 11, 14)));
        Assert.True(OutreachRules.IsActive(new Campaign { General = true, Stage = CampaignStage.Finished }, new DateOnly(2026, 11, 14)));
    }

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
