using GSBC.ChurchApp.Features.Outreach.Client.Phone;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Features.Outreach.Routing.Tests;
using GSBC.ChurchApp.Platform.Client.Location;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class WalkCompanionTests
{
    private static readonly DateOnly Today = new(2026, 11, 14);

    [Fact]
    public async Task ATapOnTheLockScreenIsTheSameRecordAsTheWalkingScreensButton()
    {
        Phone phone = new();
        PlannedWalk planned = await StartAsync(phone);
        List<GpsFix> track = Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs.Take(1), TrackStyle.InHand);
        phone.Location.Walk(track);
        DateTimeOffset at = track[^1].At;

        phone.LockScreen.Tap(WalkTapKind.NoJunkMail, at);
        phone.LockScreen.Tap(WalkTapKind.Spoke, at);
        phone.LockScreen.Tap(WalkTapKind.Pause, at);
        await phone.Companion.CatchUpAsync();

        Assert.Single(phone.Outbox.Sent, write => write.Method.EndsWith("/NoJunkMail", StringComparison.Ordinal));
        Assert.Equal(1, phone.Walk.Current!.Conversations);
        Assert.True(phone.Walk.Current.Paused);
        Assert.False(phone.Location.Running);
        Assert.Equal(WalkPhase.Paused, phone.LockScreen.Shown[^1].State.Phase);
        Assert.StartsWith("Counted.", phone.LockScreen.Shown[^1].State.Said);

        int endedBefore = phone.LockScreen.Ended;
        phone.LockScreen.Tap(WalkTapKind.Resume, at);
        phone.LockScreen.Tap(WalkTapKind.Finish, at);
        await phone.Companion.CatchUpAsync();

        Assert.True(phone.Walk.Current.Finished);
        Assert.False(phone.Location.Running);
        Assert.Contains(phone.Pages.Opened, page => page.EndsWith(OutreachLinks.Walking, StringComparison.Ordinal));
        Assert.Equal(endedBefore + 1, phone.LockScreen.Ended);
    }

    [Fact]
    public async Task AConversationCanBeTakenBackUntilItHasBeenSent()
    {
        Phone phone = new();
        await StartAsync(phone);

        phone.LockScreen.Tap(WalkTapKind.Spoke, phone.Clock.GetUtcNow());
        phone.LockScreen.Tap(WalkTapKind.Undo, phone.Clock.GetUtcNow());
        await phone.Companion.CatchUpAsync();
        Assert.Equal(0, phone.Walk.Current!.Conversations);
        Assert.Equal("Taken back.", phone.LockScreen.Shown[^1].State.Said);

        await phone.Actions.SpokeAsync();
        ((StoppedClock)phone.Clock).Now += TimeSpan.FromMinutes(2);
        await phone.Walk.DrainAsync();
        await phone.Actions.UndoAsync();
        Assert.Equal(1, phone.Walk.Current.Conversations);
        Assert.Equal(1, phone.Outbox.Pieces().Sum(piece => piece.Conversations));
    }

    [Fact]
    public async Task NoJunkMailFromTheLockScreenMarksTheLetterboxWhereTheWalkerTapped()
    {
        Phone phone = new();
        PlannedWalk planned = await StartAsync(phone);
        WalkLeg leg = planned.Plan.Legs.First(candidate => candidate.Delivers);
        int through = planned.Plan.Arcs.ToList().IndexOf(leg.Arcs[^1]) + 1;
        List<GpsFix> first = Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs.Take(through), TrackStyle.InHand);
        Routing.GeoPoint middle = Tracks.Walk(planned.Planner.Network, leg.Arcs, TrackStyle.InHand) is { } along ? along[along.Count / 2].Point : first[^1].Point;
        GpsFix tapped = first.MinBy(fix => Metres(fix.Point, middle))!;
        List<GpsFix> rest = [.. Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs, TrackStyle.InHand).Where(fix => fix.At > first[^1].At)];
        List<GpsFix> later = [.. rest.Take(rest.FindIndex(fix => Metres(fix.Point, tapped.Point) > 150) + 1)];
        phone.Location.Walk(first);
        phone.Location.Walk(later);
        Assert.True(Metres(tapped.Point, later[^1].Point) > 150, "the walker has moved well on by the time the app catches up");

        phone.LockScreen.Tap(WalkTapKind.NoJunkMail, tapped.At);
        await phone.Companion.CatchUpAsync();

        (string _, byte[] body) = Assert.Single(phone.Outbox.Sent, write => write.Method.EndsWith("/NoJunkMail", StringComparison.Ordinal));
        NoJunkMail marked = OutreachContractsModel.Instance.Deserialize<NoJunkMail>(new MemoryStream(body));
        Assert.True(Metres(tapped.Point, new Routing.GeoPoint(marked.Address.Point.Lat, marked.Address.Point.Lng)) < 40,
            "the letterbox marked is the one beside the walker when they tapped, not where the phone is later");
    }

    [Fact]
    public async Task TheLiveActivityBuzzesOnlyAtATurn()
    {
        Phone phone = new();
        PlannedWalk planned = await StartAsync(phone);
        List<GpsFix> track = Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs, TrackStyle.InHand);

        foreach (GpsFix[] stretch in track.Chunk(10))
        {
            phone.Location.Walk(stretch);
            ((StoppedClock)phone.Clock).Now = stretch[^1].At.AddSeconds(1);
            await phone.Companion.CatchUpAsync();
        }

        List<(WalkActivityState State, string? Alert)> shown = phone.LockScreen.Shown;
        List<int> buzzed = [.. Enumerable.Range(1, shown.Count - 1).Where(i => shown[i].Alert is not null)];
        Assert.NotEmpty(buzzed);
        Assert.All(buzzed, i => Assert.NotEqual(shown[i - 1].State.Street, shown[i].State.Street));
        int streetChanges = Enumerable.Range(1, shown.Count - 1).Count(i => shown[i].State.Street != shown[i - 1].State.Street);
        Assert.True(buzzed.Count < streetChanges, $"{buzzed.Count} buzzes for {streetChanges} new streets: a straight run into the next street stays quiet");
    }

    [Fact]
    public async Task AWalkLeftForMoreThanAnHourDoesNotStartRecordingAgain()
    {
        Phone phone = new();
        PlannedWalk planned = await StartAsync(phone);
        List<GpsFix> track = Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs.Take(2), TrackStyle.InHand);
        phone.Location.Walk(track);
        await phone.Walk.DrainAsync();
        await phone.Location.StopAsync();

        Phone soon = new(phone.Location, phone.Preferences);
        ((StoppedClock)soon.Clock).Now = track[^1].At + TimeSpan.FromMinutes(20);
        Assert.False((await soon.Walk.LoadAsync())!.Paused);
        Assert.True(soon.Location.Running);
        await soon.Location.StopAsync();

        Phone later = new(phone.Location, phone.Preferences);
        ((StoppedClock)later.Clock).Now = track[^1].At + WalkSession.LeftFor + TimeSpan.FromMinutes(5);
        Assert.True((await later.Walk.LoadAsync())!.Paused);
        Assert.False(later.Location.Running);
    }

    private static async Task<PlannedWalk> StartAsync(Phone phone)
    {
        OutreachNow now = await phone.Store.LoadAsync();
        OutreachRegion region = now.Regions[0];
        StreetGraph graph = (await phone.Regions.GraphAsync(region))!;
        PlannedWalk planned = WalkPlanning.Plan(now, region, graph,
            new WalkAsk(new WalkBudget(60), Fixture.Church, [OutreachCampaigns.GeneralId.ToString()], WalkMode.Letterbox), Today)!;
        await phone.Companion.RunAsync();
        await phone.Walk.StartAsync(new WalkStart("A 1 hr walk", planned.Ask.Campaigns, WalkMode.Letterbox, region, planned.Summary));
        return planned;
    }

    private static double Metres(Routing.GeoPoint a, Routing.GeoPoint b)
    {
        double lat = (b.Lat - a.Lat) * Math.PI / 180, lon = (b.Lon - a.Lon) * Math.PI / 180;
        double h = Math.Pow(Math.Sin(lat / 2), 2) + Math.Cos(a.Lat * Math.PI / 180) * Math.Cos(b.Lat * Math.PI / 180) * Math.Pow(Math.Sin(lon / 2), 2);
        return 2 * 6_371_000 * Math.Asin(Math.Sqrt(h));
    }
}
