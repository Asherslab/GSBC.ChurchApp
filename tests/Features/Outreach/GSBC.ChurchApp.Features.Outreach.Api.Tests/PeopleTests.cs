using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

[Integration("A walker's history, their choice to be counted by name, and forgetting a deleted account across walks, claims and feeds")]
public sealed class PeopleTests : OutreachHost
{
    private static readonly Guid Walker = Guid.CreateVersion7();

    [Fact]
    public async Task AWalkerWhoOptsOutIsCountedAnonymouslyOnTheLeaderboard()
    {
        People.Names[Walker] = "Sam Smith";
        string campaign = Guid.CreateVersion7().ToString();
        await PlanAsync(planning => planning.SaveCampaignAsync(new CampaignSave { Campaign = new Campaign { Id = campaign, Name = "Carols", Leaderboard = true } }, Cancel));
        await WalkAsync(walking => walking.RecordAsync(Phone, Walker,
            Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3", letterboxes: 30)], 300, TimeSpan.FromMinutes(10), campaign), Cancel));
        await KeepAsync();
        Assert.Equal(["Sam S."], (await FeedAsync<Leaderboard>(OutreachFeeds.Leaderboards)).Single().Walkers.Select(walker => walker.Name));

        await WalkAsync(walking => walking.CountingAsync(Walker, new CountingSave { ByName = false }, Cancel));
        await KeepAsync();

        Leaderboard board = Assert.Single(await FeedAsync<Leaderboard>(OutreachFeeds.Leaderboards));
        Assert.Empty(board.Walkers);
        Assert.Equal(30, board.Everyone.Letterboxes);
    }

    [Fact]
    public async Task DeletingAnAccountKeepsTheCoverageAndForgetsWhoWalked()
    {
        await WalkAsync(walking => walking.RecordAsync(Phone, Walker,
            Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3")], 300, TimeSpan.FromMinutes(10)), Cancel));
        await WalkAsync(walking => walking.CountingAsync(Walker, new CountingSave { ByName = true }, Cancel));
        Assert.Equal(2, (await LiveRowsAsync(OutreachFeeds.Mine)).Count);

        await WithAsync(provider => provider.GetRequiredService<OutreachPersonData>().ForgetAsync(Walker, Cancel));

        Assert.Empty(await LiveRowsAsync(OutreachFeeds.Mine));
        Assert.Null(Assert.Single(await RecordsAsync(db => db.Walks)).UserId);
        Assert.Empty(await RecordsAsync(db => db.Walkers));
        Assert.Single(await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage));
    }

    [Fact]
    public async Task ARouteClaimIsForgottenWithItsAccount()
    {
        string campaign = General;
        string day = Guid.NewGuid().ToString();
        string route = Guid.NewGuid().ToString();
        DateTime starts = Clock.GetUtcNow().UtcDateTime.AddHours(1);
        await PlanAsync(planning => planning.SaveDayAsync(new DaySave
            { Day = new OutreachDay { Id = day, Campaign = campaign, StartsAt = starts, EndsAt = starts.AddHours(3), Published = true } }, Cancel));
        await PlanAsync(planning => planning.SaveRouteAsync(new RouteSave { Route = new DayRoute { Id = route, DayId = day, Stretches = [Side("1/2/3")] } }, Cancel));
        await WalkAsync(walking => walking.ClaimAsync(Phone, Walker, new ClaimSend { Id = Guid.NewGuid().ToString(), RouteId = route, FirstName = "Sam" }, Cancel));
        Assert.Single(await FeedAsync<Claim>(OutreachFeeds.Claims));

        await WithAsync(provider => provider.GetRequiredService<OutreachPersonData>().ForgetAsync(Walker, Cancel));

        Assert.Empty(await FeedAsync<Claim>(OutreachFeeds.Claims));
        Assert.Empty(await RecordsAsync<ClaimRecord>(db => db.Claims));
    }
}
