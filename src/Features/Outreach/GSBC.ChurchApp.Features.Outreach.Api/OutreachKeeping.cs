using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachKeeping(
    OutreachDbContext db,
    OutreachPublisher publisher,
    OutreachSettingsStore settingsStore,
    OutreachMinistries ministries,
    OutreachLeaderboards leaderboards,
    OutreachTelling telling,
    TimeProvider time)
{
    private static readonly TimeSpan OpenFollowUpsKeptFor = TimeSpan.FromDays(365);
    private static readonly TimeSpan ResolvedReportsKeptFor = TimeSpan.FromDays(90);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        OutreachSettings settings = await settingsStore.GetAsync(cancellationToken);
        await SettingsAsync(now, cancellationToken);
        List<CampaignRecord> endingSoon = await StagesAsync(now, cancellationToken);
        await CampaignsAsync(now, cancellationToken);
        await CoverageAsync(settings, now, cancellationToken);
        await DaysAsync(now, cancellationToken);
        await ForgetAsync(settings, now, cancellationToken);
        await leaderboards.PublishAllAsync(now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        foreach (CampaignRecord campaign in endingSoon)
            await telling.EndingSoonAsync(campaign, cancellationToken);
    }

    private async Task<List<CampaignRecord>> StagesAsync(DateTime now, CancellationToken cancellationToken)
    {
        DateOnly today = ChurchTime.DayOf(now);
        List<CampaignRecord> endingSoon = [];
        foreach (CampaignRecord campaign in await db.Campaigns.Where(campaign => !campaign.General && campaign.Stage != CampaignStage.Finished).ToListAsync(cancellationToken))
        {
            CampaignStage next = CampaignStages.Advance(campaign.Stage, campaign.StageSetOn, campaign.StartsOn, campaign.EndsOn, campaign.WrapUpDays, today);
            if (next == campaign.Stage)
                continue;
            campaign.Stage = next;
            campaign.ChangedAt = now;
            if (CampaignTelling.ShouldTell(campaign))
            {
                campaign.ToldEndingAt = now;
                endingSoon.Add(campaign);
            }
            publisher.Campaign(campaign, now);
            await publisher.DeskAsync("campaign", campaign.Id, now, cancellationToken);
        }
        return endingSoon;
    }

    private async Task SettingsAsync(DateTime now, CancellationToken cancellationToken)
    {
        byte[]? latest = await LatestAsync(OutreachFeeds.Settings, OutreachPublisher.SettingsId, cancellationToken);
        List<string> running = (await ministries.RunningAsync(cancellationToken)).Select(ministry => ministry.Id.ToString()).ToList();
        if (latest is not { Length: > 0 } || !OutreachPayloads.Decode<OutreachSettings>(latest).MinistryIds.SequenceEqual(running))
            await settingsStore.PublishAsync(now, cancellationToken);
    }

    private async Task CampaignsAsync(DateTime now, CancellationToken cancellationToken)
    {
        List<string> published = await db.FeedChanges.AsNoTracking().Where(change => change.Feed == OutreachFeeds.Campaigns)
            .Select(change => change.RecordId).Distinct().ToListAsync(cancellationToken);
        foreach (CampaignRecord campaign in await db.Campaigns.ToListAsync(cancellationToken))
            if (!published.Contains(campaign.Id.ToString()) || (db.Entry(campaign).State == EntityState.Unchanged && await OutOfDateAsync(campaign, cancellationToken)))
                publisher.Campaign(campaign, now);
    }

    private async Task<bool> OutOfDateAsync(CampaignRecord campaign, CancellationToken cancellationToken) =>
        await LatestAsync(OutreachFeeds.Campaigns, campaign.Id.ToString(), cancellationToken) is { } latest
        && !latest.AsSpan().SequenceEqual(OutreachPayloads.Encode(OutreachViews.Campaign(campaign)));

    private async Task CoverageAsync(OutreachSettings settings, DateTime now, CancellationToken cancellationToken)
    {
        List<string> stale = await db.FeedChanges.AsNoTracking()
            .Where(change => change.Feed == OutreachFeeds.Coverage && !change.Deleted && change.EndsAt <= now
                             && !db.FeedChanges.Any(newer => newer.Feed == change.Feed && newer.RecordId == change.RecordId && newer.Cursor > change.Cursor))
            .Select(change => change.RecordId).ToListAsync(cancellationToken);
        foreach (IGrouping<Guid, (Guid Campaign, string Street)> campaign in stale.Select(Split).Where(key => key.Campaign != Guid.Empty).GroupBy(key => key.Campaign))
            await publisher.StreetsAsync(campaign.Key, campaign.Select(key => key.Street), settings, now, cancellationToken);
        await db.Coverage.Where(coverage => coverage.At < now - CoverageRules.KeptFor).ExecuteDeleteAsync(cancellationToken);
    }

    private static (Guid Campaign, string Street) Split(string recordId)
    {
        int slash = recordId.IndexOf('/');
        return slash > 0 && Guid.TryParseExact(recordId[..slash], "N", out Guid campaign) ? (campaign, recordId[(slash + 1)..]) : (Guid.Empty, "");
    }

    private async Task DaysAsync(DateTime now, CancellationToken cancellationToken)
    {
        DateTime ahead = now + OutreachPublisher.DaysAhead;
        DateTime behind = now - OutreachPublisher.DaysKeptAfter;
        List<DayRecord> changing = await db.Days
            .Where(day => day.OnPhones != (day.Published && day.EndsAt > behind && day.StartsAt < ahead))
            .ToListAsync(cancellationToken);
        foreach (DayRecord day in changing)
            publisher.Day(day, await db.Routes.Where(route => route.DayId == day.Id).ToListAsync(cancellationToken), now);

        List<Guid> ended = await db.Days.Where(day => day.EndsAt <= now).Select(day => day.Id).ToListAsync(cancellationToken);
        foreach (ClaimRecord claim in await db.Claims.Where(claim => ended.Contains(claim.DayId)).ToListAsync(cancellationToken))
        {
            db.Claims.Remove(claim);
            publisher.ClaimGone(claim.Id, now);
        }
    }

    private async Task ForgetAsync(OutreachSettings settings, DateTime now, CancellationToken cancellationToken)
    {
        foreach (SkipRecord skip in await db.Skips.Where(skip => skip.Until != null && skip.Until <= now).ToListAsync(cancellationToken))
        {
            db.Skips.Remove(skip);
            publisher.SkipGone(skip.Id, now);
        }
        DateTime doorsSince = CoverageRules.RestStarts(now, settings.RestDays);
        await db.Doors.Where(door => door.At < doorsSince).ExecuteDeleteAsync(cancellationToken);

        DateTime doneBefore = now - TimeSpan.FromDays(settings.FollowUpsKeptDays);
        DateTime openBefore = now - OpenFollowUpsKeptFor;
        foreach (FollowUpRecord followUp in await db.FollowUps.Where(followUp => followUp.DoneAt < doneBefore || followUp.At < openBefore).ToListAsync(cancellationToken))
        {
            db.FollowUps.Remove(followUp);
            await publisher.FollowUpGoneAsync(followUp.Id, now, cancellationToken);
        }
        DateTime resolvedBefore = now - ResolvedReportsKeptFor;
        await db.Reports.Where(report => report.State != ReportState.New && report.ResolvedAt < resolvedBefore).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<byte[]?> LatestAsync(string feed, string recordId, CancellationToken cancellationToken) =>
        await db.FeedChanges.AsNoTracking().Where(change => change.Feed == feed && change.RecordId == recordId)
            .OrderByDescending(change => change.Cursor).Select(change => change.Payload).FirstOrDefaultAsync(cancellationToken);
}
