using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

[Integration("Coverage is stored per campaign and its feed rows are rebuilt from the stored coverage when walks, expiry and the rest period change it")]
public sealed class CoverageTests : OutreachHost
{
    [Fact]
    public async Task AWalkCarryingTwoCampaignsCoversEachAndOneCampaignsWalkLeavesTheOtherOpen()
    {
        string carols = await CampaignAsync("Carols 2026");
        await WalkAsync(walking => walking.RecordAsync(Phone, null,
            Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3")], 400, TimeSpan.FromMinutes(10), General, carols), Cancel));
        await WalkAsync(walking => walking.RecordAsync(Phone, null,
            Piece(Guid.NewGuid().ToString(), 0, [Side("4/5/6")], 400, TimeSpan.FromMinutes(10), carols), Cancel));

        List<StreetCoverage> streets = await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage);
        Assert.Equal(["1/2/3"], streets.Where(street => street.Campaign == General).Select(street => street.Street));
        Assert.Equal(["1/2/3", "4/5/6"], streets.Where(street => street.Campaign == carols).Select(street => street.Street).Order());
    }

    [Fact]
    public async Task AWalkCutShortStillCountsWhatWasSentAndARepeatedPieceIsStoredOnce()
    {
        string walk = Guid.NewGuid().ToString();
        WalkPiece first = Piece(walk, 0, [Side("1/2/3", letterboxes: 12)], 300, TimeSpan.FromMinutes(10));
        await WalkAsync(walking => walking.RecordAsync(Phone, null, first, Cancel));
        await WalkAsync(walking => walking.RecordAsync(Phone, null, first, Cancel));

        WalkRecord recorded = Assert.Single(await RecordsAsync(db => db.Walks));
        Assert.False(recorded.Ended);
        Assert.Equal(12, recorded.Letterboxes);
        Assert.Equal(300, recorded.Metres);
        Assert.Single(await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage));
    }

    [Fact]
    public async Task AnImplausibleWalkIsKeptButCoversNothing()
    {
        await WalkAsync(walking => walking.RecordAsync(Phone, null,
            Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3", metres: 5000)], 5000, TimeSpan.FromMinutes(5)), Cancel));

        WalkRecord recorded = Assert.Single(await RecordsAsync(db => db.Walks));
        Assert.NotEmpty(recorded.Doubtful);
        Assert.Empty(await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage));
    }

    [Fact]
    public async Task CoverageLeavesTheFeedOnceTheRestPeriodFromSettingsHasPassed()
    {
        OutreachSettings settings = OutreachDefaults.Settings();
        settings.RestDays = 7;
        await PlanAsync(planning => planning.SaveSettingsAsync(Staff, new SettingsSave { Settings = settings }, Cancel));
        await WalkAsync(walking => walking.RecordAsync(Phone, null,
            Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3")], 300, TimeSpan.FromMinutes(10)), Cancel));

        Clock.Advance(TimeSpan.FromDays(6));
        await KeepAsync();
        Assert.Single(await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage));

        Clock.Advance(TimeSpan.FromDays(2));
        await KeepAsync();
        Assert.Empty(await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage));
    }

    [Fact]
    public async Task ExpiringAnAreaOpensItsStreetsInThatCampaignOnly()
    {
        string carols = await CampaignAsync("Carols 2026");
        await WalkAsync(walking => walking.RecordAsync(Phone, null,
            Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3")], 300, TimeSpan.FromMinutes(10), General, carols), Cancel));

        Expired expired = await ReviewAsync(review => review.ExpireAsync(new ExpireCoverage
        {
            Campaign = General,
            Area = [new GeoPoint { Lat = -27.3, Lng = 152.9 }, new GeoPoint { Lat = -27.3, Lng = 153.0 }, new GeoPoint { Lat = -27.4, Lng = 153.0 }, new GeoPoint { Lat = -27.4, Lng = 152.9 }],
        }, Cancel));

        Assert.Equal(1, expired.Stretches);
        Assert.Equal([carols], (await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage)).Select(street => street.Campaign));
    }

    [Fact]
    public async Task UndoingAnExpiryPutsBackOnlyWhatThatExpiryOpened()
    {
        await WalkAsync(walking => walking.RecordAsync(Phone, null,
            Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3"), Side("4/5/6")], 600, TimeSpan.FromMinutes(20)), Cancel));
        Expired first = await ReviewAsync(review => review.ExpireAsync(new ExpireCoverage { Campaign = General, Stretches = [Side("1/2/3")] }, Cancel));
        Clock.Advance(TimeSpan.FromMinutes(1));
        await ReviewAsync(review => review.ExpireAsync(new ExpireCoverage { Campaign = General, Stretches = [Side("4/5/6")] }, Cancel));
        Assert.Empty(await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage));

        Expired undone = await ReviewAsync(review => review.UndoExpiryAsync(new ExpiryUndo { Campaign = General, At = first.At }, Cancel));

        Assert.Equal(1, undone.Stretches);
        Assert.Equal(["1/2/3"], (await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage)).Select(street => street.Street));
    }

    [Fact]
    public async Task ACampaignWithDaysToComeCantBeDeletedAndDeletingOneForgetsItsCoverage()
    {
        string carols = await CampaignAsync("Carols 2026");
        await WalkAsync(walking => walking.RecordAsync(Phone, null,
            Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3")], 300, TimeSpan.FromMinutes(10), carols), Cancel));
        string day = Guid.NewGuid().ToString();
        DateTime starts = Clock.GetUtcNow().UtcDateTime.AddDays(3);
        await PlanAsync(planning => planning.SaveDayAsync(new DaySave { Day = new OutreachDay { Id = day, Campaign = carols, StartsAt = starts, EndsAt = starts.AddHours(3) } }, Cancel));

        CampaignDeletion blocked = await WithAsync(provider => provider.GetRequiredService<OutreachPlanning>().CampaignDeletionAsync(new ThingRef { Id = carols }, Cancel));
        Assert.NotEmpty(blocked.WhyNot);

        await PlanAsync(planning => planning.DeleteDayAsync(new ThingRef { Id = day }, Cancel));
        await PlanAsync(planning => planning.DeleteCampaignAsync(new ThingRef { Id = carols }, Cancel));

        Assert.Empty(await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage));
        Assert.Empty(await RecordsAsync(db => db.Coverage));
        Assert.Empty(Assert.Single(await RecordsAsync(db => db.Walks)).Campaigns);
        await KeepAsync();
        Assert.Equal([General], (await FeedAsync<Campaign>(OutreachFeeds.Campaigns)).Select(campaign => campaign.Id));
    }
}
