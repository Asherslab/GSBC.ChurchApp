using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

[Integration("What a door's outcome keeps in the database and which feeds it reaches")]
public sealed class DoorTests : OutreachHost
{
    private static AddressPoint Door(uint key) => new() { AddressKey = key, Region = "church-area", Street = "1/2/3", Along = 400, Point = new GeoPoint { Lat = -27.35, Lng = 152.97 } };

    [Fact]
    public async Task AHostileDoorIsSkippedForLetterboxesAndDoorsInEveryCampaign()
    {
        string carols = await CampaignAsync("Carols 2026");
        await WalkAsync(walking => walking.DoorAsync(Phone, null,
            new DoorOutcome { Id = Guid.NewGuid().ToString(), Campaign = carols, Address = Door(42), Outcome = DoorOutcomeKind.Hostile }, Cancel));

        Skip skip = Assert.Single(await FeedAsync<Skip>(OutreachFeeds.Skips));
        Assert.Equal(42u, skip.Address.AddressKey);
        Assert.True(skip.Letterbox);
        Assert.True(skip.Door);
    }

    [Fact]
    public async Task NoJunkMailSkipsTheLetterboxButNotTheDoor()
    {
        await WalkAsync(walking => walking.NoJunkMailAsync(Phone, null, new NoJunkMail { Id = Guid.NewGuid().ToString(), Address = Door(7) }, Cancel));

        Skip skip = Assert.Single(await FeedAsync<Skip>(OutreachFeeds.Skips));
        Assert.True(skip.Letterbox);
        Assert.False(skip.Door);
    }

    [Fact]
    public async Task AFollowUpIsKeptAndToldOnlyWithTheirConsent()
    {
        await WalkAsync(walking => walking.DoorAsync(Phone, null, new DoorOutcome
        {
            Id = Guid.NewGuid().ToString(), Address = Door(1), Outcome = DoorOutcomeKind.FollowUp, Consent = false, FirstName = "Sam", Contact = "0400 000 000",
        }, Cancel));
        Assert.Empty(await RecordsAsync(db => db.FollowUps));
        Assert.Empty(Told.Raised);

        await WalkAsync(walking => walking.DoorAsync(Phone, null, new DoorOutcome
        {
            Id = Guid.NewGuid().ToString(), Address = Door(2), Outcome = DoorOutcomeKind.FollowUp, Consent = true, FirstName = "Sam", Contact = "0400 000 000",
        }, Cancel));
        Assert.Equal("Sam", Assert.Single(await RecordsAsync(db => db.FollowUps)).FirstName);
        Assert.Equal(OutreachNotifications.FollowUpWanted, Assert.Single(Told.Raised).Kind);
        Assert.Single(await FeedAsync<FollowUp>(OutreachFeeds.FollowUps));
    }
}
