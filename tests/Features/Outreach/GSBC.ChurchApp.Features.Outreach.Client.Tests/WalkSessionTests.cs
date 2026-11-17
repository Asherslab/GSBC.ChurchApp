using GSBC.ChurchApp.Features.Outreach.Client.Phone;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Features.Outreach.Routing.Tests;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class WalkSessionTests
{
    private static readonly DateOnly Today = new(2026, 11, 14);

    [Fact]
    public async Task AWalkIsRecordedAsItGoesAndNoStreetIsSentTwice()
    {
        Phone phone = new();
        (PlannedWalk planned, WalkStart start) = await PlanAnHourAsync(phone);
        await phone.Walk.StartAsync(start);
        List<GpsFix> track = Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs, TrackStyle.InHand);

        foreach (GpsFix[] stretch in track.Chunk(track.Count / 4 + 1))
        {
            phone.Location.Walk(stretch);
            AdvanceTo(phone, stretch[^1].At);
            await phone.Walk.DrainAsync();
        }

        List<WalkPiece> pieces = phone.Outbox.Pieces();
        Assert.True(pieces.Count >= 3, "a walk sends pieces while it goes, not only at the end");
        Assert.Equal(Enumerable.Range(1, pieces.Count), pieces.Select(piece => piece.Seq));
        Assert.All(pieces, piece => Assert.Equal(pieces[0].WalkId, piece.WalkId));
        List<string> sent = [.. pieces.SelectMany(piece => piece.Covered).Select(Key)];
        Assert.Equal(sent.Count, sent.Distinct().Count());
        Assert.True(phone.Walk.Progress!.LetterboxesDone >= planned.Summary.Letterboxes * 0.95,
            $"{phone.Walk.Progress.LetterboxesDone} of {planned.Summary.Letterboxes} letterboxes marked from a track along the route");
        Assert.DoesNotContain(pieces, piece => piece.Ended);
    }

    [Fact]
    public async Task TheEndTakesBackWhatTheWalkerUntickedAndKeepsWhatTheyTicked()
    {
        Phone phone = new();
        (PlannedWalk planned, WalkStart start) = await PlanAnHourAsync(phone);
        await phone.Walk.StartAsync(start);
        List<Arc> firstHalf = [.. planned.Plan.Arcs.Take(planned.Plan.Arcs.Count / 2)];
        phone.Location.Walk(Tracks.Walk(planned.Planner.Network, firstHalf, TrackStyle.InHand));
        await phone.Walk.FinishAsync();

        IReadOnlyList<StepProgress> steps = phone.Walk.Progress!.Steps;
        StepProgress walked = steps.First(step => step.Step.Delivers && step.Share >= WalkSession.StepDoneShare);
        StepProgress missed = steps.Last(step => step.Step.Delivers && step.Share < WalkSession.StepDoneShare);
        HashSet<int> ticked = [.. steps.Where(step => step.Share >= WalkSession.StepDoneShare && step.Index != walked.Index).Select(step => step.Index), missed.Index];
        await phone.Walk.SaveAsync(ticked);

        WalkPiece last = phone.Outbox.Pieces()[^1];
        Assert.True(last.Ended);
        Assert.Contains(last.Uncovered, stretch => walked.Step.Sides.Any(side => Matches(stretch, side)));
        Assert.Contains(last.Covered, stretch => missed.Step.Sides.Any(side => Matches(stretch, side)));
        Assert.Equal(ticked.Sum(index => steps[index].Step.Letterboxes), phone.Walk.JustSaved!.Letterboxes);
        Assert.Null(phone.Walk.Current);
        Assert.False(phone.Location.Running);
    }

    [Fact]
    public async Task AWalkCarriesOnAfterTheAppIsClosedWithoutSendingAnythingAgain()
    {
        Phone phone = new();
        (PlannedWalk planned, WalkStart start) = await PlanAnHourAsync(phone);
        await phone.Walk.StartAsync(start);
        List<GpsFix> track = Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs, TrackStyle.InPocket);
        phone.Location.Walk(track.Take(track.Count / 2));
        await phone.Walk.DrainAsync();
        List<string> before = [.. phone.Outbox.Pieces().SelectMany(piece => piece.Covered).Select(Key)];

        Phone reopened = new(phone.Location, phone.Preferences);
        reopened.Location.Walk(track.Skip(track.Count / 2));
        WalkState? state = await reopened.Walk.LoadAsync();
        await reopened.Walk.DrainAsync();
        await reopened.Walk.FinishAsync();
        await reopened.Walk.SaveAsync(reopened.Walk.Progress!.Steps.Where(step => step.Share >= WalkSession.StepDoneShare).Select(step => step.Index).ToHashSet());

        Assert.NotNull(state);
        List<WalkPiece> after = reopened.Outbox.Pieces();
        Assert.All(after, piece => Assert.Equal(state.WalkId, piece.WalkId));
        Assert.True(after[0].Seq > phone.Outbox.Pieces()[^1].Seq, "numbering carries on, so the server never mistakes a new piece for a retried one");
        Assert.DoesNotContain(after.SelectMany(piece => piece.Covered).Select(Key), before.Contains);
        Assert.True(after[^1].Ended);
    }

    [Fact]
    public async Task SkippedThisBitStaysOpenForSomeoneElse()
    {
        Phone phone = new();
        (PlannedWalk planned, WalkStart start) = await PlanAnHourAsync(phone);
        await phone.Walk.StartAsync(start);
        phone.Location.Walk(Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs.Take(1), TrackStyle.InHand));
        await phone.Walk.DrainAsync();
        StepProgress now = phone.Walk.Progress!.Now!;

        await phone.Walk.SkipThisBitAsync();
        phone.Location.Walk(Tracks.Walk(planned.Planner.Network, planned.Plan.Arcs.Skip(1).Take(3), TrackStyle.InHand));
        await phone.Walk.FinishAsync();
        await phone.Walk.SaveAsync(new HashSet<int>());

        List<WalkPiece> pieces = phone.Outbox.Pieces();
        Assert.Contains(pieces.SelectMany(piece => piece.Skipped), stretch => now.Step.Sides.Any(side => Matches(stretch, side)));
        Assert.DoesNotContain(pieces.SelectMany(piece => piece.Covered), stretch => now.Step.Sides.Any(side => Matches(stretch, side) && stretch.From >= Thousandths(side.From) && stretch.To <= Thousandths(side.To)));
    }

    private static async Task<(PlannedWalk Planned, WalkStart Start)> PlanAnHourAsync(Phone phone)
    {
        OutreachNow now = await phone.Store.LoadAsync();
        OutreachRegion region = now.Regions[0];
        StreetGraph graph = (await phone.Regions.GraphAsync(region))!;
        PlannedWalk planned = WalkPlanning.Plan(now, region, graph,
            new WalkAsk(new WalkBudget(60), Fixture.Church, [OutreachCampaigns.GeneralId.ToString()], WalkMode.Letterbox), Today)!;
        return (planned, new WalkStart("A 1 hr walk", planned.Ask.Campaigns, WalkMode.Letterbox, region, planned.Summary));
    }

    private static void AdvanceTo(Phone phone, DateTimeOffset at) => ((StoppedClock)phone.Clock).Now = at.AddSeconds(1);

    private static string Key(Contracts.Stretch stretch) => $"{stretch.Street}|{stretch.Side}|{stretch.From}|{stretch.To}";

    private static int Thousandths(double fraction) => (int)Math.Round(fraction * OutreachRules.WholeStreet);

    private static bool Matches(Contracts.Stretch stretch, SavedSide side) =>
        stretch.Street == Fixture.AlbanyCreek().KeyOf(side.Street).ToString() && (int)stretch.Side == side.Side;
}
