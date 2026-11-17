using System.Security.Cryptography;
using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachPlanning(
    OutreachDbContext db,
    OutreachPublisher publisher,
    OutreachSettingsStore settingsStore,
    OutreachMinistries ministries,
    OutreachLeaderboards leaderboards,
    OutreachTelling telling,
    TimeProvider time)
{
    private const string CodeLetters = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;
    private static readonly TimeSpan LongestDay = TimeSpan.FromHours(24);
    private static readonly TimeSpan DaysShownBack = TimeSpan.FromDays(90);

    public async Task<RunningMinistries> MinistriesAsync(CancellationToken cancellationToken) => new()
    {
        Ministries = (await ministries.RunningAsync(cancellationToken)).Select(ministry => new OutreachMinistry { Id = ministry.Id.ToString(), Name = ministry.Name })
            .ToList(),
    };

    public async Task<OutreachDesk> DeskAsync(string ministryId, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(ministryId, cancellationToken);
        DateTime since = Now - DaysShownBack;
        OutreachSettings settings = await settingsStore.GetAsync(cancellationToken);
        settings.MinistryIds = (await ministries.RunningAsync(cancellationToken)).Select(ministry => ministry.Id.ToString()).ToList();
        List<DayRecord> days = await db.Days.AsNoTracking().Where(day => day.EndsAt > since).OrderBy(day => day.StartsAt).ToListAsync(cancellationToken);
        List<Guid> dayIds = days.Select(day => day.Id).ToList();
        ILookup<Guid, RouteRecord> routes = (await db.Routes.AsNoTracking().Where(route => dayIds.Contains(route.DayId)).ToListAsync(cancellationToken)).ToLookup(route => route.DayId);
        Dictionary<Guid, int> claims = await db.Claims.AsNoTracking().Where(claim => dayIds.Contains(claim.DayId)).GroupBy(claim => claim.DayId)
            .Select(group => new { group.Key, Count = group.Count() }).ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
        return new OutreachDesk
        {
            Settings = settings,
            Campaigns = (await db.Campaigns.AsNoTracking().ToListAsync(cancellationToken))
                .OrderByDescending(campaign => campaign.General).ThenBy(campaign => campaign.StartsOn).ThenBy(campaign => campaign.Name)
                .Select(OutreachViews.Campaign).ToList(),
            Days = days.Select(OutreachViews.Day).ToList(),
            DayFacts = days.Select(day => new DayFacts
            {
                DayId = day.Id.ToString(),
                Routes = routes[day.Id].Count(),
                Letterboxes = routes[day.Id].Sum(route => OutreachPayloads.Decode<DayRoute>(route.Payload).Letterboxes),
                Claims = claims.GetValueOrDefault(day.Id),
                RoutesPublishedAt = day.RoutesPublishedAt,
                HasDraft = day.Draft is { Length: > 0 },
            }).ToList(),
            Areas = (await db.Areas.AsNoTracking().OrderBy(area => area.Name).ToListAsync(cancellationToken)).Select(OutreachViews.Area).ToList(),
            Ministries = (await MinistriesAsync(cancellationToken)).Ministries,
            Totals = await TotalsAsync(cancellationToken),
            OpenFollowUps = await db.FollowUps.CountAsync(followUp => followUp.DoneAt == null, cancellationToken),
            NewReports = await db.Reports.CountAsync(report => report.State == ReportState.New, cancellationToken),
        };
    }

    private async Task<List<CampaignTotals>> TotalsAsync(CancellationToken cancellationToken)
    {
        DateTime now = Now;
        DateOnly today = ChurchTime.DayOf(now);
        DateTime monthStarts = ChurchTime.ToUtc(new DateOnly(today.Year, today.Month, 1).ToDateTime(TimeOnly.MinValue));
        DateTime weekStarts = ChurchTime.ToUtc(today.AddDays(-(((int)today.DayOfWeek + 6) % 7)).ToDateTime(TimeOnly.MinValue));
        List<WalkRecord> walks = await db.Walks.AsNoTracking().Where(walk => walk.Doubtful == "").ToListAsync(cancellationToken);
        Dictionary<Guid, int> days = await db.Days.AsNoTracking().GroupBy(day => day.CampaignId).Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
        return
        [
            .. walks.SelectMany(walk => walk.Campaigns.Select(campaign => (campaign, walk))).GroupBy(pair => pair.campaign)
                .Select(group => new CampaignTotals
                {
                    Campaign = group.Key.ToString(),
                    LetterboxesThisMonth = group.Where(pair => pair.walk.StartedAt >= monthStarts).Sum(pair => pair.walk.Letterboxes),
                    LetterboxesThisWeek = group.Where(pair => pair.walk.StartedAt >= weekStarts).Sum(pair => pair.walk.Letterboxes),
                    Letterboxes = group.Sum(pair => pair.walk.Letterboxes),
                    Metres = group.Sum(pair => pair.walk.Metres),
                    Walks = group.Count(),
                    Days = days.GetValueOrDefault(group.Key),
                })
                .Concat(days.Keys.Where(campaign => walks.All(walk => !walk.Campaigns.Contains(campaign)))
                    .Select(campaign => new CampaignTotals { Campaign = campaign.ToString(), Days = days[campaign] })),
        ];
    }

    public async Task SaveSettingsAsync(Guid by, SettingsSave save, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(save.MinistryId, cancellationToken);
        if (SettingsRules.WhyNot(save.Settings) is { } why)
            throw Invalid(why);
        DateTime now = Now;
        await settingsStore.SaveAsync(save.Settings, by, now, cancellationToken);
        await publisher.DeskAsync("settings", Guid.Empty, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveCampaignAsync(CampaignSave save, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(save.MinistryId, cancellationToken);
        Campaign asked = save.Campaign;
        string name = OutreachRules.Clip(asked.Name, OutreachRules.LongestName);
        if (name.Length == 0)
            throw Invalid("Give the campaign a name.");
        DateOnly? starts = OutreachRules.DateOf(asked.StartsOn);
        DateOnly? ends = OutreachRules.DateOf(asked.EndsOn);
        if (starts is { } from && ends is { } to && to < from)
            throw Invalid("A campaign ends on or after the day it starts.");
        if (asked.Area.Count > 0 && !OutreachRules.IsAPolygon(asked.Area))
            throw Invalid("Draw the campaign's area with at least three points.");
        if (CampaignRules.WhyNotNumbers(asked) is { } numbers)
            throw Invalid(numbers);

        DateTime now = Now;
        DateOnly today = ChurchTime.DayOf(now);
        Guid id = Guid.TryParse(asked.Id, out Guid given) && given != Guid.Empty ? given : Guid.CreateVersion7();
        CampaignRecord? campaign = await db.Campaigns.FirstOrDefaultAsync(found => found.Id == id, cancellationToken);
        bool isNew = campaign is null;
        if (campaign is null)
        {
            campaign = new CampaignRecord { Id = id, CreatedAt = now, StageSetOn = today };
            db.Campaigns.Add(campaign);
        }
        int wrapUpDays = asked.WrapUpDays > 0 ? asked.WrapUpDays : CampaignStages.DefaultWrapUpDays;
        bool timingChanged = !campaign.General && (isNew || starts != campaign.StartsOn || ends != campaign.EndsOn || wrapUpDays != campaign.WrapUpDays);
        bool finished = campaign.Stage == CampaignStage.Finished;
        if (finished && timingChanged)
            throw Invalid("It's finished, so its dates stay as they were. Move it back to Ongoing to change them.");

        bool leaderboardWas = campaign.Leaderboard;
        campaign.Name = name;
        campaign.HandedOut = OutreachRules.Clip(asked.HandedOut, OutreachRules.LongestName);
        campaign.Leaderboard = asked.Leaderboard;
        if (!finished)
            campaign.BundleSize = asked.BundleSize;
        campaign.BundlesMade = asked.BundlesMade;
        if (!campaign.General)
        {
            (campaign.StartsOn, campaign.EndsOn, campaign.WrapUpDays) = (starts, ends, wrapUpDays);
            campaign.AreaLats = asked.Area.Select(point => point.Lat).ToList();
            campaign.AreaLngs = asked.Area.Select(point => point.Lng).ToList();
            campaign.Regions = asked.Regions.Select(region => OutreachRules.Clip(region, OutreachRules.LongestName)).Where(region => region.Length > 0).Distinct().ToList();
        }
        if (timingChanged)
            (campaign.Stage, campaign.StageSetOn) = (CampaignStages.Initial(starts, ends, wrapUpDays, today), today);
        campaign.ChangedAt = now;
        bool tell = Tell(campaign, now);
        publisher.Campaign(campaign, now);
        if (campaign.Leaderboard)
            await leaderboards.PublishAsync(campaign.Id, now, cancellationToken);
        else if (leaderboardWas)
            publisher.LeaderboardGone(campaign.Id, now);
        await publisher.DeskAsync("campaign", campaign.Id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (tell)
            await telling.EndingSoonAsync(campaign, cancellationToken);
    }

    public async Task MoveCampaignAsync(CampaignMove move, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(move.MinistryId, cancellationToken);
        if (!Enum.IsDefined(move.Stage))
            throw Invalid("Choose a stage.");
        CampaignRecord campaign = await CampaignAsync(move.Id, cancellationToken);
        if (campaign.General)
            throw Invalid("General outreach is always on, so it has no stages.");
        if (campaign.Stage == move.Stage)
            return;
        DateTime now = Now;
        (campaign.Stage, campaign.StageSetOn, campaign.ChangedAt) = (move.Stage, ChurchTime.DayOf(now), now);
        bool tell = Tell(campaign, now);
        publisher.Campaign(campaign, now);
        await publisher.DeskAsync("campaign", campaign.Id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (tell)
            await telling.EndingSoonAsync(campaign, cancellationToken);
    }

    private static bool Tell(CampaignRecord campaign, DateTime now)
    {
        if (!CampaignTelling.ShouldTell(campaign))
            return false;
        campaign.ToldEndingAt = now;
        return true;
    }

    public async Task<CampaignDeletion> CampaignDeletionAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        CampaignRecord campaign = await CampaignAsync(thing.Id, cancellationToken);
        DateTime now = Now;
        int upcoming = await db.Days.CountAsync(day => day.CampaignId == campaign.Id && day.EndsAt > now, cancellationToken);
        return new CampaignDeletion
        {
            Name = campaign.Name,
            CoveredStretches = await db.Coverage.CountAsync(coverage => coverage.CampaignId == campaign.Id && coverage.ExpiredAt == null, cancellationToken),
            PastDays = await db.Days.CountAsync(day => day.CampaignId == campaign.Id && day.EndsAt <= now, cancellationToken),
            UpcomingDays = upcoming,
            Walks = await db.Walks.CountAsync(walk => walk.Campaigns.Contains(campaign.Id), cancellationToken),
            WhyNot = CampaignRules.WhyNotDelete(campaign, upcoming) ?? "",
        };
    }

    public async Task DeleteCampaignAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        CampaignRecord campaign = await CampaignAsync(thing.Id, cancellationToken);
        DateTime now = Now;
        int upcoming = await db.Days.CountAsync(day => day.CampaignId == campaign.Id && day.EndsAt > now, cancellationToken);
        if (CampaignRules.WhyNotDelete(campaign, upcoming) is { } why)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, why));

        List<string> streets = await db.Coverage.Where(coverage => coverage.CampaignId == campaign.Id).Select(coverage => coverage.Street).Distinct()
            .ToListAsync(cancellationToken);
        await db.Coverage.Where(coverage => coverage.CampaignId == campaign.Id).ExecuteDeleteAsync(cancellationToken);
        await db.Doors.Where(door => door.CampaignId == campaign.Id).ExecuteDeleteAsync(cancellationToken);
        foreach (DayRecord day in await db.Days.Where(day => day.CampaignId == campaign.Id).ToListAsync(cancellationToken))
            await RemoveDayAsync(day, now, cancellationToken);
        foreach (WalkRecord walk in await db.Walks.Where(walk => walk.Campaigns.Contains(campaign.Id)).ToListAsync(cancellationToken))
            walk.Campaigns = walk.Campaigns.Where(id => id != campaign.Id).ToList();
        foreach (string street in streets)
            publisher.CoverageGone(campaign.Id, street, now);
        db.Campaigns.Remove(campaign);
        publisher.CampaignGone(campaign.Id, now);
        await publisher.DeskAsync("campaign", campaign.Id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveDayAsync(DaySave save, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(save.MinistryId, cancellationToken);
        OutreachDay asked = save.Day;
        if (!Guid.TryParse(asked.Campaign, out Guid campaignId) || await db.Campaigns.FirstOrDefaultAsync(found => found.Id == campaignId, cancellationToken) is not { } campaign)
            throw Invalid("Choose the campaign the day is for.");
        DateTime starts = DateTime.SpecifyKind(asked.StartsAt, DateTimeKind.Utc);
        DateTime ends = DateTime.SpecifyKind(asked.EndsAt, DateTimeKind.Utc);
        if (ends <= starts || ends - starts > LongestDay)
            throw Invalid("An outreach day ends after it starts, on the same day.");

        DateTime now = Now;
        Guid id = Guid.TryParse(asked.Id, out Guid given) && given != Guid.Empty ? given : Guid.CreateVersion7();
        DayRecord? day = await db.Days.FirstOrDefaultAsync(found => found.Id == id, cancellationToken);
        if (day is null)
        {
            day = new DayRecord { Id = id };
            db.Days.Add(day);
        }
        if (day.CampaignId != campaignId && CampaignRules.WhyNotPlanDay(campaign) is { } finished)
            throw Invalid(finished);
        day.CampaignId = campaignId;
        (day.StartsAt, day.EndsAt) = (starts, ends);
        (day.MeetingLat, day.MeetingLng) = (asked.MeetingPoint.Lat, asked.MeetingPoint.Lng);
        day.MeetingPlace = OutreachRules.Clip(asked.MeetingPlace, OutreachRules.LongestName);
        day.Note = OutreachRules.Clip(asked.Note, OutreachRules.LongestNote);
        day.Mode = Enum.IsDefined(asked.Mode) ? asked.Mode : WalkMode.Letterbox;
        day.Published = asked.Published;
        day.ChangedAt = now;
        publisher.Day(day, await db.Routes.Where(route => route.DayId == id).ToListAsync(cancellationToken), now);
        await publisher.DeskAsync("day", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDayAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        if (!Guid.TryParse(thing.Id, out Guid id) || await db.Days.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } day)
            return;
        DateTime now = Now;
        await RemoveDayAsync(day, now, cancellationToken);
        await publisher.DeskAsync("day", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveDayAsync(DayRecord day, DateTime now, CancellationToken cancellationToken)
    {
        List<RouteRecord> routes = await db.Routes.Where(route => route.DayId == day.Id).ToListAsync(cancellationToken);
        foreach (ClaimRecord claim in await db.Claims.Where(claim => claim.DayId == day.Id).ToListAsync(cancellationToken))
        {
            db.Claims.Remove(claim);
            publisher.ClaimGone(claim.Id, now);
        }
        await db.Walks.Where(walk => walk.DayId == day.Id).ExecuteUpdateAsync(set => set.SetProperty(walk => walk.DayId, (Guid?)null).SetProperty(walk => walk.RouteId, (Guid?)null), cancellationToken);
        await db.Coverage.Where(coverage => coverage.DayId == day.Id).ExecuteUpdateAsync(set => set.SetProperty(coverage => coverage.DayId, (Guid?)null), cancellationToken);
        publisher.DayGone(day.Id, routes.Select(route => route.Id), now);
        db.Routes.RemoveRange(routes);
        db.Days.Remove(day);
    }

    public async Task<DayDeletion> DayDeletionAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        DayRecord day = await DayAsync(thing.Id, cancellationToken);
        DateTime now = Now;
        return new DayDeletion
        {
            Routes = await db.Routes.CountAsync(route => route.DayId == day.Id, cancellationToken),
            Claims = await db.Claims.CountAsync(claim => claim.DayId == day.Id, cancellationToken),
            Walks = await db.Walks.CountAsync(walk => walk.DayId == day.Id, cancellationToken),
            OnNow = day.StartsAt <= now && now < day.EndsAt,
        };
    }

    public async Task<PlanningMarks> PlanningMarksAsync(string ministryId, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(ministryId, cancellationToken);
        return new PlanningMarks
        {
            Skips = (await db.Skips.AsNoTracking().ToListAsync(cancellationToken)).Select(OutreachViews.Skip).ToList(),
            Exclusions =
            [
                .. (await db.ExclusionZones.AsNoTracking().ToListAsync(cancellationToken)).Select(OutreachViews.Exclusion),
                .. (await db.ExclusionAddresses.AsNoTracking().ToListAsync(cancellationToken)).Select(OutreachViews.Exclusion),
            ],
        };
    }

    public async Task<DayPlan> PlanAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        DayRecord day = await DayAsync(thing.Id, cancellationToken);
        return new DayPlan
        {
            Day = OutreachViews.Day(day),
            Routes = (await db.Routes.AsNoTracking().Where(route => route.DayId == day.Id).OrderBy(route => route.Number).ToListAsync(cancellationToken))
                .Select(route => OutreachPayloads.Decode<DayRoute>(route.Payload)).ToList(),
            Draft = day.Draft is { Length: > 0 } draft ? OutreachPayloads.Decode<DayDraft>(draft).Upgraded() : null,
            PublishedAt = day.RoutesPublishedAt,
        };
    }

    public async Task SaveDraftAsync(DraftSave save, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(save.MinistryId, cancellationToken);
        DayRecord day = await DayAsync(save.DayId, cancellationToken);
        if (save.Draft is { } draft && PlanRules.WhyNot(draft) is { } why)
            throw Invalid(why);
        DateTime now = Now;
        if (save.Draft is { } kept)
            kept.ChangedAt = now;
        day.Draft = save.Draft is null ? null : OutreachPayloads.Encode(save.Draft);
        day.ChangedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Published> PublishRoutesAsync(RoutesPublish publish, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(publish.MinistryId, cancellationToken);
        DayRecord day = await DayAsync(publish.DayId, cancellationToken);
        if (PlanRules.WhyNot(publish.Routes) is { } why)
            throw Invalid(why);

        DateTime now = Now;
        List<RouteRecord> existing = await db.Routes.Where(route => route.DayId == day.Id).ToListAsync(cancellationToken);
        HashSet<Guid> asked = publish.Routes.Select(route => Guid.TryParse(route.Id, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToHashSet();
        foreach (RouteRecord gone in existing.Where(route => !asked.Contains(route.Id)).ToList())
        {
            foreach (ClaimRecord claim in await db.Claims.Where(claim => claim.RouteId == gone.Id).ToListAsync(cancellationToken))
            {
                db.Claims.Remove(claim);
                publisher.ClaimGone(claim.Id, now);
            }
            db.Routes.Remove(gone);
            existing.Remove(gone);
            publisher.RouteGone(gone.Id, now);
        }

        List<DayRoute> stored = [];
        int number = 0;
        foreach (DayRoute route in publish.Routes)
        {
            number++;
            Guid id = Guid.TryParse(route.Id, out Guid given) && given != Guid.Empty ? given : Guid.CreateVersion7();
            RouteRecord? record = existing.FirstOrDefault(found => found.Id == id);
            if (record is null)
            {
                if (await db.Routes.AnyAsync(found => found.Id == id, cancellationToken))
                    throw Invalid("That route belongs to another day.");
                record = new RouteRecord { Id = id, DayId = day.Id, Code = await NewCodeAsync(cancellationToken) };
                db.Routes.Add(record);
                existing.Add(record);
            }
            record.Number = number;
            DayRoute kept = Stored(route, id, day.Id, record);
            record.Payload = OutreachPayloads.Encode(kept);
            record.ChangedAt = now;
            stored.Add(kept);
        }

        day.Published = true;
        day.RoutesPublishedAt = now;
        day.ChangedAt = now;
        publisher.Day(day, existing, now);
        await publisher.DeskAsync("day", day.Id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new Published { Routes = stored };
    }

    private static DayRoute Stored(DayRoute asked, Guid id, Guid dayId, RouteRecord record) => new()
    {
        Id = id.ToString(), DayId = dayId.ToString(), Number = record.Number, Code = record.Code,
        Name = OutreachRules.Clip(asked.Name, OutreachRules.LongestName) is { Length: > 0 } name ? name : $"Route {record.Number}",
        Stretches = asked.Stretches, Start = asked.Start,
        Letterboxes = asked.Letterboxes > 0 ? asked.Letterboxes : asked.Stretches.Sum(stretch => stretch.Letterboxes),
        Metres = asked.Metres > 0 ? asked.Metres : asked.Stretches.Sum(stretch => stretch.Metres),
        Minutes = Math.Max(0, asked.Minutes),
        Colour = Math.Max(0, asked.Colour),
    };

    private async Task<DayRecord> DayAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid dayId) && await db.Days.FirstOrDefaultAsync(found => found.Id == dayId, cancellationToken) is { } day
            ? day
            : throw new RpcException(new Status(StatusCode.NotFound, "That outreach day isn't there any more."));

    public async Task<DayRoutes> RoutesAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        Guid dayId = Guid.TryParse(thing.Id, out Guid id) ? id : Guid.Empty;
        return new DayRoutes
        {
            Routes = (await db.Routes.AsNoTracking().Where(route => route.DayId == dayId).OrderBy(route => route.Number).ToListAsync(cancellationToken))
                .Select(route => OutreachPayloads.Decode<DayRoute>(route.Payload)).ToList(),
        };
    }

    public async Task SaveRouteAsync(RouteSave save, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(save.MinistryId, cancellationToken);
        DayRoute asked = save.Route;
        if (!Guid.TryParse(asked.DayId, out Guid dayId) || await db.Days.FirstOrDefaultAsync(found => found.Id == dayId, cancellationToken) is not { } day)
            throw Invalid("Choose the outreach day the route is for.");
        if (asked.Stretches.Count == 0 || asked.Stretches.Any(stretch => !OutreachRules.IsValid(stretch)))
            throw Invalid("A route needs its streets.");

        DateTime now = Now;
        Guid id = Guid.TryParse(asked.Id, out Guid given) && given != Guid.Empty ? given : Guid.CreateVersion7();
        List<RouteRecord> routes = await db.Routes.Where(route => route.DayId == dayId).ToListAsync(cancellationToken);
        RouteRecord? route = routes.FirstOrDefault(found => found.Id == id);
        if (route is null)
        {
            if (await db.Routes.AnyAsync(found => found.Id == id, cancellationToken))
                throw Invalid("That route belongs to another day.");
            route = new RouteRecord
            {
                Id = id,
                DayId = dayId,
                Code = await NewCodeAsync(cancellationToken),
                Number = asked.Number > 0 ? asked.Number : routes.Select(found => found.Number).DefaultIfEmpty().Max() + 1,
            };
            db.Routes.Add(route);
            routes.Add(route);
        }
        else if (asked.Number > 0)
            route.Number = asked.Number;

        DayRoute stored = Stored(asked, id, dayId, route);
        route.Payload = OutreachPayloads.Encode(stored);
        route.ChangedAt = now;
        publisher.Day(day, routes, now);
        await publisher.DeskAsync("day", dayId, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRouteAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        if (!Guid.TryParse(thing.Id, out Guid id) || await db.Routes.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } route)
            return;
        DateTime now = Now;
        foreach (ClaimRecord claim in await db.Claims.Where(claim => claim.RouteId == id).ToListAsync(cancellationToken))
        {
            db.Claims.Remove(claim);
            publisher.ClaimGone(claim.Id, now);
        }
        db.Routes.Remove(route);
        publisher.RouteGone(id, now);
        await publisher.DeskAsync("day", route.DayId, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAreaAsync(AreaSave save, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(save.MinistryId, cancellationToken);
        string name = OutreachRules.Clip(save.Area.Name, OutreachRules.LongestName);
        if (name.Length == 0)
            throw Invalid("Give the area a name.");
        if (!OutreachRules.IsAPolygon(save.Area.Area))
            throw Invalid("Draw the area with at least three points.");
        DateTime now = Now;
        Guid id = Guid.TryParse(save.Area.Id, out Guid given) && given != Guid.Empty ? given : Guid.CreateVersion7();
        AreaRecord? area = await db.Areas.FirstOrDefaultAsync(found => found.Id == id, cancellationToken);
        if (area is null)
        {
            area = new AreaRecord { Id = id };
            db.Areas.Add(area);
        }
        area.Name = name;
        area.AreaLats = save.Area.Area.Select(point => point.Lat).ToList();
        area.AreaLngs = save.Area.Area.Select(point => point.Lng).ToList();
        area.Note = OutreachRules.Clip(save.Area.Note, OutreachRules.LongestNote);
        area.ChangedAt = now;
        await publisher.DeskAsync("area", id, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAreaAsync(ThingRef thing, CancellationToken cancellationToken)
    {
        await ministries.CheckAsync(thing.MinistryId, cancellationToken);
        if (!Guid.TryParse(thing.Id, out Guid id) || await db.Areas.FirstOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } area)
            return;
        db.Areas.Remove(area);
        await publisher.DeskAsync("area", id, Now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> NewCodeAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            string code = RandomNumberGenerator.GetString(CodeLetters, CodeLength);
            if (!await db.Routes.AnyAsync(route => route.Code == code, cancellationToken))
                return code;
        }
    }

    private async Task<CampaignRecord> CampaignAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid campaignId) && await db.Campaigns.FirstOrDefaultAsync(found => found.Id == campaignId, cancellationToken) is { } campaign
            ? campaign
            : throw new RpcException(new Status(StatusCode.NotFound, "That campaign isn't there any more."));

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}

internal static class CampaignRules
{
    private const int LargestBundle = 1000;

    public static string? WhyNotNumbers(Campaign campaign) =>
        campaign.WrapUpDays is < 0 or > CampaignStages.LongestWrapUpDays ? $"Wrapping up starts between 1 and {CampaignStages.LongestWrapUpDays} days before it ends."
        : campaign.BundleSize is < 0 or > LargestBundle ? $"A bundle holds between 1 and {LargestBundle:N0} letterboxes."
        : campaign.BundlesMade is < 0 or > CampaignStages.MostBundlesMade ? $"Bundles made is between 0 and {CampaignStages.MostBundlesMade:N0}."
        : null;

    public static string? WhyNotPlanDay(CampaignRecord campaign) =>
        campaign.Stage == CampaignStage.Finished ? $"{campaign.Name} has finished. Move it back to Ongoing to plan a day for it." : null;

    public static string? WhyNotDelete(CampaignRecord campaign, int upcomingDays) =>
        campaign.General ? "General outreach is always there, so it can't be deleted."
        : upcomingDays > 0 ? $"It has {upcomingDays} outreach {(upcomingDays == 1 ? "day" : "days")} still to come. Delete or move {(upcomingDays == 1 ? "it" : "them")} first."
        : null;
}
