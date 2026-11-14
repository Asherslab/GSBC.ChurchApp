using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachReview(
    OutreachDbContext db,
    OutreachPublisher publisher,
    OutreachSettingsStore settingsStore,
    OutreachMinistries ministries,
    OutreachLeaderboards leaderboards,
    OutreachMedia media,
    OutreachTelling telling,
    IPeopleIndex people,
    TimeProvider time)
{
    private static readonly TimeSpan ResolvedReportsShownFor = TimeSpan.FromDays(90);
    private static readonly TimeSpan StillWalking = TimeSpan.FromMinutes(30);

    public async Task<Reports> ReportsAsync(string ministryId, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(ministryId, cancellationToken);
        DateTime since = Now - ResolvedReportsShownFor;
        List<ReportRecord> reports = await db.Reports.AsNoTracking().Where(report => report.State == ReportState.New || report.ResolvedAt > since)
            .OrderByDescending(report => report.At).ToListAsync(cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await NamesAsync(reports.Select(report => report.UserId), cancellationToken);
        return new Reports { Lines = reports.Select(report => OutreachViews.Report(report, ShortName(report.UserId, names))).ToList() };
    }

    public async Task ResolveReportAsync(Guid by, ReportResolve resolve, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(resolve.MinistryId, cancellationToken);
        if (resolve.Outcome == ReportState.New || !Enum.IsDefined(resolve.Outcome))
            throw Invalid("Choose what was done about the report.");
        if (!Guid.TryParse(resolve.Id, out Guid id) || await db.Reports.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } report)
            return;
        DateTime now = Now;
        if (resolve.Outcome == ReportState.Excluded)
        {
            ExclusionAddressRecord excluded = new()
            {
                Id = Guid.CreateVersion7(), AddressKey = report.AddressKey, Region = report.Region, Street = report.Street, Side = report.Side, Along = report.Along,
                Lat = report.Lat, Lng = report.Lng, Label = report.Label, Reason = Enum.IsDefined(resolve.ExcludeAs) ? resolve.ExcludeAs : ExclusionReason.Other,
                Note = report.Note, ShowMarker = resolve.ShowMarker, ChangedAt = now, ChangedBy = by,
            };
            db.ExclusionAddresses.Add(excluded);
            publisher.Address(excluded, now);
        }
        MediaRef? photo = report.Photo is { } bytes ? OutreachPayloads.Decode<MediaRef>(bytes) : null;
        (report.State, report.ResolvedAt, report.ResolvedBy, report.Photo) = (resolve.Outcome, now, by, null);
        await publisher.DeskAsync("report", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await media.ForgetAsync(id, photo, cancellationToken);
        await telling.SettleReportAsync(id, cancellationToken);
    }

    public async Task DeleteReportAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        if (!Guid.TryParse(thing.Id, out Guid id) || await db.Reports.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } report)
            return;
        await RemoveReportAsync(report, cancellationToken);
    }

    private async Task RemoveReportAsync(ReportRecord report, CancellationToken cancellationToken)
    {
        db.Reports.Remove(report);
        await publisher.DeskAsync("report", report.Id, Now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await media.ForgetAsync(report.Id, report.Photo is { } bytes ? OutreachPayloads.Decode<MediaRef>(bytes) : null, cancellationToken);
        await telling.SettleReportAsync(report.Id, cancellationToken);
    }

    public async Task<FollowUps> FollowUpsAsync(string ministryId, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(ministryId, cancellationToken);
        List<FollowUpRecord> followUps = await db.FollowUps.AsNoTracking().OrderBy(followUp => followUp.DoneAt != null).ThenByDescending(followUp => followUp.At)
            .ToListAsync(cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await NamesAsync(followUps.Select(followUp => followUp.UserId), cancellationToken);
        return new FollowUps { Lines = followUps.Select(followUp => OutreachViews.FollowUp(followUp, ShortName(followUp.UserId, names))).ToList() };
    }

    public async Task FollowUpDoneAsync(FollowUpDone done, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(done.MinistryId, cancellationToken);
        if (!Guid.TryParse(done.Id, out Guid id) || await db.FollowUps.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } followUp)
            return;
        DateTime now = Now;
        followUp.DoneAt = done.Done ? followUp.DoneAt ?? now : null;
        await publisher.FollowUpAsync(followUp, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (done.Done)
            await telling.SettleFollowUpAsync(id, cancellationToken);
    }

    public async Task DeleteFollowUpAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        if (!Guid.TryParse(thing.Id, out Guid id) || await db.FollowUps.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } followUp)
            return;
        db.FollowUps.Remove(followUp);
        await publisher.FollowUpGoneAsync(id, Now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await telling.SettleFollowUpAsync(id, cancellationToken);
    }

    public async Task<SkipLines> SkipsAsync(string ministryId, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(ministryId, cancellationToken);
        DateTime now = Now;
        return new SkipLines
        {
            Lines = (await db.Skips.AsNoTracking().Where(skip => skip.Until == null || skip.Until > now).OrderByDescending(skip => skip.At).ToListAsync(cancellationToken))
                .Select(OutreachViews.SkipLine).ToList(),
        };
    }

    public async Task ClearSkipAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        if (!Guid.TryParse(thing.Id, out Guid id) || await db.Skips.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } skip)
            return;
        DateTime now = Now;
        db.Skips.Remove(skip);
        publisher.SkipGone(id, now);
        await publisher.DeskAsync("skip", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Exclusions> ExclusionsAsync(string ministryId, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(ministryId, cancellationToken);
        return new Exclusions
        {
            Zones = (await db.ExclusionZones.AsNoTracking().OrderBy(zone => zone.Name).ToListAsync(cancellationToken)).Select(OutreachViews.Zone).ToList(),
            Addresses = (await db.ExclusionAddresses.AsNoTracking().OrderBy(address => address.Label).ToListAsync(cancellationToken))
                .Select(OutreachViews.ExcludedAddress).ToList(),
        };
    }

    public async Task SaveZoneAsync(Guid by, ZoneSave save, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(save.MinistryId, cancellationToken);
        if (!OutreachRules.IsAPolygon(save.Zone.Area))
            throw Invalid("Draw the place with at least three points.");
        DateTime now = Now;
        Guid id = Guid.TryParse(save.Zone.Id, out Guid given) && given != Guid.Empty ? given : Guid.CreateVersion7();
        ExclusionZoneRecord? zone = await db.ExclusionZones.FirstOrDefaultAsync(found => found.Id == id, cancellationToken);
        if (zone is null)
        {
            zone = new ExclusionZoneRecord { Id = id };
            db.ExclusionZones.Add(zone);
        }
        zone.Name = OutreachRules.Clip(save.Zone.Name, OutreachRules.LongestName);
        zone.AreaLats = save.Zone.Area.Select(point => point.Lat).ToList();
        zone.AreaLngs = save.Zone.Area.Select(point => point.Lng).ToList();
        zone.Reason = Enum.IsDefined(save.Zone.Reason) ? save.Zone.Reason : ExclusionReason.Other;
        zone.Note = OutreachRules.Clip(save.Zone.Note, OutreachRules.LongestNote);
        (zone.ChangedAt, zone.ChangedBy) = (now, by);
        publisher.Zone(zone, now);
        await publisher.DeskAsync("zone", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteZoneAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        if (!Guid.TryParse(thing.Id, out Guid id) || await db.ExclusionZones.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } zone)
            return;
        DateTime now = Now;
        db.ExclusionZones.Remove(zone);
        publisher.ZoneGone(id, now);
        await publisher.DeskAsync("zone", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAddressAsync(Guid by, AddressSave save, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(save.MinistryId, cancellationToken);
        AddressPoint place = save.Address.Address;
        if (place is { AddressKey: 0, Street.Length: 0 })
            throw Invalid("Choose the address on the map.");
        DateTime now = Now;
        Guid id = Guid.TryParse(save.Address.Id, out Guid given) && given != Guid.Empty ? given : Guid.CreateVersion7();
        ExclusionAddressRecord? address = await db.ExclusionAddresses.FirstOrDefaultAsync(found => found.Id == id, cancellationToken);
        if (address is null)
        {
            address = new ExclusionAddressRecord { Id = id };
            db.ExclusionAddresses.Add(address);
        }
        (address.AddressKey, address.Region, address.Street, address.Side, address.Along) = (place.AddressKey, place.Region, place.Street, place.Side, place.Along);
        (address.Lat, address.Lng) = (place.Point.Lat, place.Point.Lng);
        address.Label = OutreachRules.Clip(place.Label, OutreachRules.LongestName);
        address.Reason = Enum.IsDefined(save.Address.Reason) ? save.Address.Reason : ExclusionReason.Other;
        address.Note = OutreachRules.Clip(save.Address.Note, OutreachRules.LongestNote);
        address.Caller = OutreachRules.Clip(save.Address.Caller, OutreachRules.LongestContact);
        address.ShowMarker = save.Address.ShowMarker;
        (address.ChangedAt, address.ChangedBy) = (now, by);
        publisher.Address(address, now);
        await publisher.DeskAsync("address", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAddressAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        if (!Guid.TryParse(thing.Id, out Guid id) || await db.ExclusionAddresses.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } address)
            return;
        DateTime now = Now;
        db.ExclusionAddresses.Remove(address);
        publisher.AddressGone(id, now);
        await publisher.DeskAsync("address", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CoverageMap> CoverageAsync(CoverageQuery query, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(query.MinistryId, cancellationToken);
        Guid campaign = Guid.TryParse(query.Campaign, out Guid chosen) ? chosen : OutreachCampaigns.GeneralId;
        DateTime since = Now - CoverageRules.KeptFor;
        List<CoverageRecord> rows = await db.Coverage.AsNoTracking()
            .Where(coverage => coverage.CampaignId == campaign && coverage.ExpiredAt == null && coverage.At > since
                               && (query.Region.Length == 0 || coverage.Region == query.Region))
            .ToListAsync(cancellationToken);
        return new CoverageMap
        {
            Streets = rows.GroupBy(coverage => coverage.Street)
                .Select(street => new StreetCoverage
                {
                    Campaign = campaign.ToString(), Region = street.MaxBy(coverage => coverage.At)!.Region, Street = street.Key,
                    Stretches = CoverageRules.Merge(street),
                })
                .ToList(),
        };
    }

    public async Task<Expired> ExpireAsync(ExpireCoverage expire, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(expire.MinistryId, cancellationToken);
        if (!Guid.TryParse(expire.Campaign, out Guid campaign) || !await db.Campaigns.AnyAsync(found => found.Id == campaign, cancellationToken))
            throw Invalid("Choose the campaign whose streets are to be done again.");
        if (expire.Area.Count > 0 && !OutreachRules.IsAPolygon(expire.Area))
            throw Invalid("Draw the area with at least three points.");
        DateTime now = Now;
        List<string> named = expire.Stretches.Select(stretch => stretch.Street).Distinct().ToList();
        List<CoverageRecord> live = await db.Coverage
            .Where(coverage => coverage.CampaignId == campaign && coverage.ExpiredAt == null && (expire.Area.Count > 0 || named.Contains(coverage.Street)))
            .ToListAsync(cancellationToken);
        List<CoverageRecord> expiring = live.Where(coverage => CoverageExpiry.Matches(coverage, expire)).ToList();
        foreach (CoverageRecord coverage in expiring)
            coverage.ExpiredAt = now;
        await db.SaveChangesAsync(cancellationToken);
        await publisher.StreetsAsync(campaign, expiring.Select(coverage => coverage.Street), await settingsStore.GetAsync(cancellationToken), now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new Expired { Stretches = expiring.Count };
    }

    public async Task<Totals> TotalsAsync(TotalsQuery query, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(query.MinistryId, cancellationToken);
        DateTime from = DateTime.SpecifyKind(query.From, DateTimeKind.Utc);
        DateTime to = query.To == default ? DateTime.MaxValue : DateTime.SpecifyKind(query.To, DateTimeKind.Utc);
        Guid? campaign = Guid.TryParse(query.Campaign, out Guid chosen) ? chosen : null;
        List<WalkRecord> walks = await db.Walks.AsNoTracking()
            .Where(walk => walk.StartedAt >= from && walk.StartedAt < to && walk.Doubtful == "" && (campaign == null || walk.Campaigns.Contains(campaign.Value)))
            .ToListAsync(cancellationToken);
        Dictionary<Guid, string> campaignNames = await db.Campaigns.AsNoTracking().ToDictionaryAsync(found => found.Id, found => found.Name, cancellationToken);
        (List<WalkerTotal> named, WalkerTotal anonymous) = TotalsRules.Walkers(walks, await leaderboards.WalkersAsync(walks, cancellationToken));
        return new Totals
        {
            Rows = query.By == TotalsBy.Campaign ? TotalsRules.ByCampaign(walks, campaignNames) : TotalsRules.Rows(walks, query.By),
            Walkers = named,
            Anonymous = anonymous,
        };
    }

    public async Task<DayBoard> BoardAsync(BoardQuery query, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(query.MinistryId, cancellationToken);
        if (!Guid.TryParse(query.DayId, out Guid dayId) || await db.Days.AsNoTracking().FirstOrDefaultAsync(found => found.Id == dayId, cancellationToken) is not { } day)
            throw new RpcException(new Status(StatusCode.NotFound, "That outreach day isn't there any more."));
        List<DayRoute> routes = (await db.Routes.AsNoTracking().Where(route => route.DayId == dayId).OrderBy(route => route.Number).ToListAsync(cancellationToken))
            .Select(route => OutreachPayloads.Decode<DayRoute>(route.Payload)).ToList();
        List<ClaimRecord> claims = await db.Claims.AsNoTracking().Where(claim => claim.DayId == dayId).ToListAsync(cancellationToken);
        List<string> streets = routes.SelectMany(route => route.Stretches).Select(stretch => stretch.Street).Distinct().ToList();
        List<CoverageRecord> done = await db.Coverage.AsNoTracking()
            .Where(coverage => coverage.CampaignId == day.CampaignId && coverage.ExpiredAt == null && coverage.At >= day.StartsAt && streets.Contains(coverage.Street))
            .ToListAsync(cancellationToken);
        DateTime now = Now;
        return new DayBoard
        {
            Day = OutreachViews.Day(day),
            Routes = routes,
            Progress = routes.Select(route => Progress(route, claims, done.Select(row => new CoveredPiece(row.Street, row.Side, row.From, row.To)).ToList())).ToList(),
            Walking = await db.Walks.CountAsync(walk => walk.DayId == dayId && !walk.Ended && walk.LastAt > now - StillWalking, cancellationToken),
        };
    }

    private static RouteProgress Progress(DayRoute route, List<ClaimRecord> claims, List<CoveredPiece> done) =>
        DayProgress.Of(route, claims.Where(claim => claim.RouteId.ToString() == route.Id).OrderBy(claim => claim.At).Select(claim => claim.FirstName), done);

    private async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IEnumerable<Guid?> users, CancellationToken cancellationToken)
    {
        HashSet<Guid> known = users.Where(user => user is not null).Select(user => user!.Value).ToHashSet();
        return known.Count == 0 ? new Dictionary<Guid, string>() : await people.NamesAsync(known, cancellationToken);
    }

    private static string ShortName(Guid? user, IReadOnlyDictionary<Guid, string> names) =>
        user is { } id && names.TryGetValue(id, out string? name) ? PersonNames.Short(name) : "";

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}

internal static class CoverageExpiry
{
    public static bool Matches(CoverageRecord coverage, ExpireCoverage expire) =>
        (expire.Area.Count > 0 && OutreachRules.Contains(expire.Area, new GeoPoint { Lat = coverage.MiddleLat, Lng = coverage.MiddleLng }))
        || expire.Stretches.Any(stretch => stretch.Street == coverage.Street && stretch.Side == coverage.Side && stretch.From < coverage.To && stretch.To > coverage.From);
}
