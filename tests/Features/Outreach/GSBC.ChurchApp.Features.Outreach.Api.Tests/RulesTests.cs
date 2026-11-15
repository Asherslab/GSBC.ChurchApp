using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

public sealed class RulesTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc);

    private static CoverageRecord Covered(int from, int to, int daysAgo, StreetSide side = StreetSide.Left) =>
        new() { Street = "1/2/3", Side = side, From = from, To = to, At = Now.AddDays(-daysAgo) };

    [Fact]
    public void TheNewestWalkOfAPieceOfStreetIsTheOneKept()
    {
        List<CoveredStretch> merged = CoverageRules.Merge([Covered(0, 1000, 10), Covered(200, 600, 2), Covered(0, 1000, 3, StreetSide.Right)]);

        Assert.Equal(
            [(StreetSide.Left, 0, 200, 10), (StreetSide.Left, 200, 600, 2), (StreetSide.Left, 600, 1000, 10), (StreetSide.Right, 0, 1000, 3)],
            merged.Select(stretch => (stretch.Side, stretch.From, stretch.To, (Now - stretch.At).Days)));
    }

    [Fact]
    public void AWalkFasterThanTheSettingsAllowIsDoubtful()
    {
        OutreachSettings settings = OutreachDefaults.Settings();
        WalkRecord walk = new() { StartedAt = Now.AddMinutes(-30) };

        Assert.True(WalkRules.Check(walk, new WalkPiece { At = Now, StartedAt = walk.StartedAt, Metres = 2000 }, settings, Now).Counts);
        Assert.False(WalkRules.Check(walk, new WalkPiece { At = Now, StartedAt = walk.StartedAt, Metres = 6000 }, settings, Now).Counts);
    }

    [Fact]
    public void MarkingMoreStreetDoneThanWasWalkedIsDoubtful()
    {
        WalkRecord walk = new() { StartedAt = Now.AddMinutes(-30) };
        WalkPiece piece = new() { At = Now, StartedAt = walk.StartedAt, Metres = 200, Covered = [new Stretch { Metres = 2000 }] };

        Assert.False(WalkRules.Check(walk, piece, OutreachDefaults.Settings(), Now).Counts);
    }

    [Theory]
    [InlineData(DoorOutcomeKind.NoAnswer, false, "", DoorKeeping.NoAnswer)]
    [InlineData(DoorOutcomeKind.Spoke, false, "", DoorKeeping.Nothing)]
    [InlineData(DoorOutcomeKind.FollowUp, false, "0400 000 000", DoorKeeping.Nothing)]
    [InlineData(DoorOutcomeKind.FollowUp, true, "", DoorKeeping.Nothing)]
    [InlineData(DoorOutcomeKind.FollowUp, true, "0400 000 000", DoorKeeping.FollowUp)]
    [InlineData(DoorOutcomeKind.Hostile, false, "", DoorKeeping.Hostile)]
    internal void ADoorKeepsOnlyWhatItsOutcomeAndConsentAllow(DoorOutcomeKind outcome, bool consent, string contact, DoorKeeping kept) =>
        Assert.Equal(kept, DoorRules.WhatToKeep(new DoorOutcome { Outcome = outcome, Consent = consent, Contact = contact }));

    [Fact]
    public void WalkersWhoOptedOutAreTotalledAnonymously()
    {
        Guid named = Guid.NewGuid();
        Guid hidden = Guid.NewGuid();
        List<WalkRecord> walks =
        [
            new() { UserId = named, Letterboxes = 10, StartedAt = Now }, new() { UserId = hidden, Letterboxes = 20, StartedAt = Now },
            new() { Letterboxes = 5, StartedAt = Now },
        ];
        Dictionary<Guid, Walker> walkers = new()
        {
            [named] = new Walker(named, "Ruth Jones", true), [hidden] = new Walker(hidden, "Sam Smith", false),
        };

        (List<WalkerTotal> listed, WalkerTotal anonymous) = TotalsRules.Walkers(walks, walkers);

        Assert.Equal(["Ruth J."], listed.Select(total => total.Name));
        Assert.Equal(25, anonymous.Letterboxes);
    }
}
