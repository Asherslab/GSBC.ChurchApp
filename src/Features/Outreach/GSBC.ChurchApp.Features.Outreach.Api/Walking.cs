using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class Walking(
    OutreachDbContext db,
    OutreachPublisher publisher,
    OutreachSettingsStore settingsStore,
    OutreachMedia media,
    OutreachTelling telling,
    TimeProvider time)
{
    private const int MostCountedPerPiece = 500;
    private static readonly TimeSpan UndoWithin = TimeSpan.FromDays(1);

    public async Task RecordAsync(Guid install, Guid? user, WalkPiece piece, CancellationToken cancellationToken)
    {
        Guid walkId = IdOf(piece.WalkId, "A walk needs its id.");
        if (piece.Seq is < 0 or >= WalkRules.MostPiecesPerWalk)
            throw Invalid("That part of the walk is out of order.");
        DateTime now = Now;

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        WalkRecord? walk = await db.Walks.FirstOrDefaultAsync(found => found.Id == walkId, cancellationToken);
        if (walk is not null && walk.InstallId != install)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "That walk was recorded by another phone."));
        if (walk is not null && await db.WalkPieces.AnyAsync(found => found.WalkId == walkId && found.Seq == piece.Seq, cancellationToken))
            return;

        walk ??= await StartAsync(install, user, walkId, piece, now, cancellationToken);
        OutreachSettings settings = await settingsStore.GetAsync(cancellationToken);
        WalkVerdict verdict = WalkRules.Check(walk, piece, settings, now);
        db.WalkPieces.Add(new WalkPieceRecord { WalkId = walkId, Seq = piece.Seq, At = piece.At, Metres = piece.Metres, Counted = verdict.Counts });

        HashSet<string> streets = [];
        if (verdict.Counts)
            streets = await CountAsync(walk, piece, cancellationToken);
        else if (walk.Doubtful.Length == 0)
            walk.Doubtful = verdict.Why;

        walk.LastAt = piece.At > walk.LastAt ? Min(piece.At, now) : walk.LastAt;
        walk.Ended |= piece.Ended;
        walk.WithPartner |= piece.WithPartner;
        await db.SaveChangesAsync(cancellationToken);

        foreach (Guid campaign in walk.Campaigns)
            await publisher.StreetsAsync(campaign, streets, settings, now, cancellationToken);
        publisher.Walk(walk, walk.Doubtful.Length == 0, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<WalkRecord> StartAsync(Guid install, Guid? user, Guid walkId, WalkPiece piece, DateTime now, CancellationToken cancellationToken)
    {
        int startedToday = await db.Walks.CountAsync(found => found.InstallId == install && found.StartedAt > now - OutreachLimits.Day, cancellationToken);
        OutreachLimits.Check(startedToday, OutreachLimits.WalksPerDay, "walks");

        List<Guid> asked = piece.Campaigns.Select(text => Guid.TryParse(text, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();
        List<Guid> campaigns = await db.Campaigns.Where(campaign => asked.Contains(campaign.Id)).Select(campaign => campaign.Id).ToListAsync(cancellationToken);
        if (campaigns.Count == 0)
            campaigns.Add(OutreachCampaigns.GeneralId);

        DateTime startedAt = Min(piece.StartedAt == default ? piece.At : piece.StartedAt, now);
        Guid? dayId = Guid.TryParse(piece.DayId, out Guid day) ? day : await DayOnAsync(campaigns, startedAt, cancellationToken);
        WalkRecord walk = new()
        {
            Id = walkId,
            InstallId = install,
            UserId = user,
            Campaigns = campaigns,
            Mode = Enum.IsDefined(piece.Mode) ? piece.Mode : WalkMode.Letterbox,
            DayId = dayId,
            RouteId = Guid.TryParse(piece.RouteId, out Guid route) ? route : null,
            StartedAt = startedAt,
            LastAt = startedAt,
        };
        db.Walks.Add(walk);
        return walk;
    }

    private async Task<Guid?> DayOnAsync(List<Guid> campaigns, DateTime at, CancellationToken cancellationToken) =>
        await db.Days.Where(day => day.Published && campaigns.Contains(day.CampaignId) && day.StartsAt <= at && day.EndsAt > at)
            .OrderBy(day => day.StartsAt).Select(day => (Guid?)day.Id).FirstOrDefaultAsync(cancellationToken);

    private async Task<HashSet<string>> CountAsync(WalkRecord walk, WalkPiece piece, CancellationToken cancellationToken)
    {
        List<Stretch> covered = piece.Covered.Where(OutreachRules.IsValid).ToList();
        List<Stretch> reopened = piece.Uncovered.Concat(piece.Skipped).Where(OutreachRules.IsValid).ToList();
        walk.Metres += piece.Metres;
        walk.Letterboxes += covered.Sum(stretch => stretch.Letterboxes);
        walk.Doors += Math.Clamp(piece.Doors, 0, MostCountedPerPiece);
        walk.Conversations += Math.Clamp(piece.Conversations, 0, MostCountedPerPiece);

        await ReopenAsync(walk, reopened, cancellationToken);
        CoverageSource source = walk.RouteId is null ? CoverageSource.Casual : CoverageSource.DayRoute;
        foreach (Guid campaign in walk.Campaigns)
            foreach (Stretch stretch in covered)
                db.Coverage.Add(new CoverageRecord
                {
                    CampaignId = campaign,
                    Region = stretch.Region,
                    RegionVersion = stretch.RegionVersion,
                    Street = stretch.Street,
                    Side = stretch.Side,
                    From = stretch.From,
                    To = stretch.To,
                    MiddleLat = stretch.Middle.Lat,
                    MiddleLng = stretch.Middle.Lng,
                    Metres = stretch.Metres,
                    Letterboxes = stretch.Letterboxes,
                    At = Min(piece.At, Now),
                    Source = source,
                    WalkId = walk.Id,
                    DayId = walk.DayId,
                });
        return covered.Concat(reopened).Select(stretch => stretch.Street).ToHashSet();
    }

    private async Task ReopenAsync(WalkRecord walk, List<Stretch> reopened, CancellationToken cancellationToken)
    {
        if (reopened.Count == 0)
            return;
        List<string> streets = reopened.Select(stretch => stretch.Street).Distinct().ToList();
        List<CoverageRecord> ours = await db.Coverage.Where(coverage => coverage.WalkId == walk.Id && streets.Contains(coverage.Street)).ToListAsync(cancellationToken);
        foreach (CoverageRecord coverage in ours.Where(coverage => reopened.Any(stretch =>
                     stretch.Street == coverage.Street && stretch.Side == coverage.Side && stretch.From < coverage.To && stretch.To > coverage.From)))
        {
            walk.Letterboxes = Math.Max(0, walk.Letterboxes - (coverage.CampaignId == walk.Campaigns[0] ? coverage.Letterboxes : 0));
            db.Coverage.Remove(coverage);
        }
    }

    public async Task NoJunkMailAsync(Guid install, Guid? user, NoJunkMail mark, CancellationToken cancellationToken)
    {
        Guid id = IdOf(mark.Id, "A No junk mail mark needs its id.");
        if (await db.Skips.AnyAsync(skip => skip.Id == id, cancellationToken))
            return;
        CheckPlace(mark.Address);
        DateTime now = Now;
        int today = await db.Skips.CountAsync(skip => skip.InstallId == install && skip.At > now - OutreachLimits.Day, cancellationToken);
        OutreachLimits.Check(today, OutreachLimits.NoJunkMailPerDay, "No junk mail marks");

        OutreachSettings settings = await settingsStore.GetAsync(cancellationToken);
        DateTime until = now + TimeSpan.FromDays(settings.NoJunkMailKeptDays);
        if (await SameAddressAsync(SkipKind.NoJunkMail, mark.Address, cancellationToken) is { } existing)
        {
            existing.Until = until;
            publisher.Skip(existing, now);
        }
        else
        {
            SkipRecord skip = NewSkip(id, SkipKind.NoJunkMail, mark.Address, install, user, now, until);
            db.Skips.Add(skip);
            publisher.Skip(skip, now);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UndoNoJunkMailAsync(Guid install, MarkRef mark, CancellationToken cancellationToken)
    {
        DateTime now = Now;
        if (!Guid.TryParse(mark.Id, out Guid id)
            || await db.Skips.FirstOrDefaultAsync(found => found.Id == id && found.Kind == SkipKind.NoJunkMail, cancellationToken) is not { } skip
            || skip.InstallId != install || skip.At < now - UndoWithin)
            return;
        db.Skips.Remove(skip);
        publisher.SkipGone(skip.Id, now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DoorAsync(Guid install, Guid? user, DoorOutcome outcome, CancellationToken cancellationToken)
    {
        Guid id = IdOf(outcome.Id, "A door needs its id.");
        if (await db.Doors.AnyAsync(door => door.Id == id, cancellationToken) || await db.FollowUps.AnyAsync(followUp => followUp.Id == id, cancellationToken)
            || await db.Skips.AnyAsync(skip => skip.Id == id, cancellationToken))
            return;
        CheckPlace(outcome.Address);
        DateTime now = Now;
        int today = await db.Doors.CountAsync(door => door.InstallId == install && door.At > now - OutreachLimits.Day, cancellationToken)
                    + await db.FollowUps.CountAsync(followUp => followUp.InstallId == install && followUp.At > now - OutreachLimits.Day, cancellationToken);
        OutreachLimits.Check(today, OutreachLimits.DoorsPerDay, "doors");
        Guid campaign = Guid.TryParse(outcome.Campaign, out Guid chosen) && await db.Campaigns.AnyAsync(found => found.Id == chosen, cancellationToken)
            ? chosen
            : OutreachCampaigns.GeneralId;
        Guid? walk = Guid.TryParse(outcome.WalkId, out Guid walkId) ? walkId : null;

        switch (DoorRules.WhatToKeep(outcome))
        {
            case DoorKeeping.NoAnswer:
                db.Doors.Add(new DoorRecord
                {
                    Id = id, CampaignId = campaign, WalkId = walk, InstallId = install, AddressKey = outcome.Address.AddressKey, Street = outcome.Address.Street,
                    Side = outcome.Address.Side, Along = outcome.Address.Along, At = now,
                });
                break;
            case DoorKeeping.FollowUp:
                FollowUpRecord followUp = new()
                {
                    Id = id, CampaignId = campaign, AddressKey = outcome.Address.AddressKey, Region = outcome.Address.Region, Street = outcome.Address.Street,
                    Side = outcome.Address.Side, Along = outcome.Address.Along, Lat = outcome.Address.Point.Lat, Lng = outcome.Address.Point.Lng,
                    Label = OutreachRules.Clip(outcome.Address.Label, OutreachRules.LongestName),
                    FirstName = OutreachRules.Clip(outcome.FirstName, OutreachRules.LongestFirstName),
                    Contact = OutreachRules.Clip(outcome.Contact, OutreachRules.LongestContact),
                    Note = OutreachRules.Clip(outcome.Note, OutreachRules.LongestNote), At = now, InstallId = install, UserId = user,
                };
                db.FollowUps.Add(followUp);
                await publisher.FollowUpAsync(followUp, now, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                await telling.FollowUpAsync(followUp, cancellationToken);
                return;
            case DoorKeeping.Hostile:
                if (await SameAddressAsync(SkipKind.Hostile, outcome.Address, cancellationToken) is null)
                {
                    SkipRecord skip = NewSkip(id, SkipKind.Hostile, outcome.Address, install, user, now, null);
                    db.Skips.Add(skip);
                    publisher.Skip(skip, now);
                }
                break;
            case DoorKeeping.Nothing:
                return;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReportAsync(Guid install, Guid? user, ReportSend send, CancellationToken cancellationToken)
    {
        Guid id = IdOf(send.Id, "A report needs its id.");
        if (await db.Reports.AnyAsync(report => report.Id == id, cancellationToken))
            return;
        string note = OutreachRules.Clip(send.Note, OutreachRules.LongestNote);
        if (note.Length == 0)
            throw Invalid("Say what's wrong in a note, so staff know what to look at.");
        if (!Enum.IsDefined(send.Reason))
            throw Invalid("Choose why you're reporting it.");
        DateTime now = Now;
        int today = await db.Reports.CountAsync(report => report.InstallId == install && report.At > now - OutreachLimits.Day, cancellationToken);
        OutreachLimits.Check(today, OutreachLimits.ReportsPerDay, "reports");

        MediaRef? photo = await media.KeepAsync(id, send.Photo, cancellationToken);
        ReportRecord report = new()
        {
            Id = id, Reason = send.Reason, AddressKey = send.Place.AddressKey, Region = send.Place.Region, Street = send.Place.Street, Side = send.Place.Side,
            Along = send.Place.Along, Lat = send.Place.Point.Lat, Lng = send.Place.Point.Lng, Label = OutreachRules.Clip(send.Place.Label, OutreachRules.LongestName),
            Note = note, Photo = photo is null ? null : OutreachPayloads.Encode(photo), At = now, InstallId = install, UserId = user,
        };
        db.Reports.Add(report);
        await publisher.DeskAsync("report", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await telling.ReportedAsync(report, cancellationToken);
    }

    public async Task ClaimAsync(Guid install, Guid? user, ClaimSend send, CancellationToken cancellationToken)
    {
        Guid id = IdOf(send.Id, "A claim needs its id.");
        if (await db.Claims.AnyAsync(claim => claim.Id == id, cancellationToken))
            return;
        string firstName = OutreachRules.Clip(send.FirstName, OutreachRules.LongestFirstName);
        if (firstName.Length == 0)
            throw Invalid("Add your first name, so others see who has this route.");
        DateTime now = Now;
        if (!Guid.TryParse(send.RouteId, out Guid routeId)
            || await db.Routes.AsNoTracking().FirstOrDefaultAsync(found => found.Id == routeId, cancellationToken) is not { } route
            || await db.Days.AsNoTracking().FirstOrDefaultAsync(found => found.Id == route.DayId, cancellationToken) is not { Published: true } day
            || day.EndsAt <= now)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "That route isn't on offer any more."));
        int today = await db.Claims.CountAsync(claim => claim.InstallId == install && claim.At > now - OutreachLimits.Day, cancellationToken);
        OutreachLimits.Check(today, OutreachLimits.ClaimsPerDay, "claims");

        ClaimRecord claimed = new() { Id = id, RouteId = routeId, DayId = day.Id, InstallId = install, UserId = user, FirstName = firstName, At = now };
        db.Claims.Add(claimed);
        publisher.Claim(claimed, now);
        await publisher.DeskAsync("day", day.Id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnclaimAsync(Guid install, Guid? user, MarkRef mark, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(mark.Id, out Guid id)
            || await db.Claims.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } claim
            || (claim.InstallId != install && (user is null || claim.UserId != user)))
            return;
        DateTime now = Now;
        db.Claims.Remove(claim);
        publisher.ClaimGone(claim.Id, now);
        await publisher.DeskAsync("day", claim.DayId, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CountingAsync(Guid user, CountingSave save, CancellationToken cancellationToken)
    {
        DateTime now = Now;
        if (await db.Walkers.FirstOrDefaultAsync(walker => walker.UserId == user, cancellationToken) is { } existing)
            (existing.ByName, existing.ChangedAt) = (save.ByName, now);
        else
            db.Walkers.Add(new WalkerRecord { UserId = user, ByName = save.ByName, ChangedAt = now });
        publisher.Counting(user, save.ByName, now);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<SkipRecord?> SameAddressAsync(SkipKind kind, AddressPoint address, CancellationToken cancellationToken) =>
        address.AddressKey == 0
            ? null
            : await db.Skips.FirstOrDefaultAsync(skip => skip.Kind == kind && skip.AddressKey == address.AddressKey, cancellationToken);

    private static SkipRecord NewSkip(Guid id, SkipKind kind, AddressPoint address, Guid install, Guid? user, DateTime now, DateTime? until) => new()
    {
        Id = id, Kind = kind, AddressKey = address.AddressKey, Region = address.Region, Street = address.Street, Side = address.Side, Along = address.Along,
        Lat = address.Point.Lat, Lng = address.Point.Lng, Label = OutreachRules.Clip(address.Label, OutreachRules.LongestName), InstallId = install,
        UserId = user, At = now, Until = until,
    };

    private static void CheckPlace(AddressPoint address)
    {
        if (address.AddressKey == 0 && (address.Street.Length == 0 || address.Along is < 0 or > OutreachRules.WholeStreet))
            throw Invalid("That letterbox isn't on the map.");
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static Guid IdOf(string text, string message) =>
        Guid.TryParse(text, out Guid id) && id != Guid.Empty ? id : throw Invalid(message);

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
