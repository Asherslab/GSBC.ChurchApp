using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

[Integration("Publishing replaces a day's stored routes and claims in one transaction, and deleting a day rewrites its walks and coverage")]
public sealed class DayPlanTests : OutreachHost
{
    [Fact]
    public async Task RepublishingKeepsEachRoutesPrintedCodeAndDropsTheClaimsOfRoutesTakenAway()
    {
        string day = await DayAsync();
        List<DayRoute> first = await PublishAsync(day, Route("", "1/2/3"), Route("", "4/5/6"));
        await WalkAsync(walking => walking.ClaimAsync(Phone, null, new ClaimSend { Id = Guid.NewGuid().ToString(), RouteId = first[1].Id, FirstName = "Sam" }, Cancel));

        List<DayRoute> second = await PublishAsync(day, Route(first[0].Id, "1/2/3", "7/8/9"), Route("", "10/11/12"));

        Assert.Equal(first[0].Code, second[0].Code);
        Assert.NotEqual(first[1].Id, second[1].Id);
        Assert.Equal([1, 2], second.Select(route => route.Number));
        Assert.Empty(await RecordsAsync(db => db.Claims));
        Assert.Equal(second.Select(route => route.Id).Order(), (await FeedAsync<DayRoute>(OutreachFeeds.Routes)).Select(route => route.Id).Order());
    }

    [Fact]
    public async Task DeletingADayKeepsWhatWasWalkedAsCasualWalks()
    {
        string day = await DayAsync();
        DayRoute route = (await PublishAsync(day, Route("", "1/2/3")))[0];
        WalkPiece piece = Piece(Guid.NewGuid().ToString(), 0, [Side("1/2/3")], 300, TimeSpan.FromMinutes(10));
        (piece.DayId, piece.RouteId) = (day, route.Id);
        await WalkAsync(walking => walking.RecordAsync(Phone, null, piece, Cancel));

        await PlanAsync(planning => planning.DeleteDayAsync(new ThingRef { Id = day }, Cancel));

        WalkRecord walk = Assert.Single(await RecordsAsync(db => db.Walks));
        Assert.Null(walk.DayId);
        Assert.Equal(10, walk.Letterboxes);
        CoverageRecord covered = Assert.Single(await RecordsAsync(db => db.Coverage));
        Assert.Null(covered.DayId);
        Assert.Single(await FeedAsync<StreetCoverage>(OutreachFeeds.Coverage));
    }

    private async Task<string> DayAsync()
    {
        string id = Guid.CreateVersion7().ToString();
        DateTime starts = Clock.GetUtcNow().UtcDateTime.AddMinutes(-30);
        await PlanAsync(planning => planning.SaveDayAsync(new DaySave
        {
            Day = new OutreachDay { Id = id, Campaign = General, StartsAt = starts, EndsAt = starts.AddHours(3), Published = true },
        }, Cancel));
        return id;
    }

    private async Task<List<DayRoute>> PublishAsync(string day, params DayRoute[] routes) =>
        await WithAsync(async provider => (await provider.GetRequiredService<OutreachPlanning>()
            .PublishRoutesAsync(new RoutesPublish { DayId = day, Routes = [.. routes] }, Cancel)).Routes);

    private static DayRoute Route(string id, params string[] streets) => new() { Id = id, Stretches = [.. streets.Select(street => Side(street))] };
}
