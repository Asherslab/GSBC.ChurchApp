using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.People.Api.SafeMinistry;

internal sealed class SafeMinistrySync(
    PeopleDbContext db,
    SafeMinistryClient client,
    IOptions<SafeMinistryOptions> options,
    IPeopleIndex index,
    IPeopleAccounts accounts,
    Families families,
    TimeProvider time,
    ILogger<SafeMinistrySync> logger)
{
    private const string ListRecordId = "safe-ministry-check";
    private const long AdvisoryLockKey = 0x7065_6f70_6c65_736d;

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public bool Connected => options.Value.Connected;

    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        if (!Connected)
            return false;

        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
                return false;
            try
            {
                await SyncAsync(cancellationToken);
                return true;
            }
            finally
            {
                await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task<SmcSyncState> StateAsync(CancellationToken cancellationToken) =>
        await db.SmcSync.AsNoTracking().SingleOrDefaultAsync(state => state.Id == SmcSyncState.Only, cancellationToken) ?? new SmcSyncState();

    public async Task LinkByHandAsync(Guid personId, string smcId, Guid by, CancellationToken cancellationToken)
    {
        SmcPersonRecord record = await db.SmcPeople.SingleOrDefaultAsync(person => person.SmcId == smcId, cancellationToken)
            ?? throw new SmcLinkException("That person isn't in Safe Ministry Check any more. Sync again to see who is.");
        SafeguardingEntry? holder = await db.Safeguarding.SingleOrDefaultAsync(
            entry => entry.Source == SafeguardingSources.SafeMinistryCheck && entry.ExternalId == smcId, cancellationToken);
        if (holder is not null && holder.UserId != personId)
            throw new SmcLinkException("That Safe Ministry Check record is already linked to someone else. Unlink them first.");

        SafeguardingEntry entry = await db.Safeguarding.SingleOrDefaultAsync(found => found.UserId == personId, cancellationToken)
            ?? db.Safeguarding.Add(new SafeguardingEntry { UserId = personId, Source = SafeguardingSources.SafeMinistryCheck }).Entity;
        Link(entry, smcId, SmcLinkHow.ByHand, by);
        record.Why = null;
        record.LinkByHandOnly = false;

        if (Connected && await PersonOrNullAsync(smcId, cancellationToken) is { } row)
            Sync(entry, SmcValues.From(row, ChurchTime.Today(time)));
        await families.TouchAsync([personId], safeguarding: true, cancellationToken);
        TouchList();
        await db.SaveChangesAsync(cancellationToken);
        await RecountAsync(cancellationToken);
    }

    public async Task UnlinkAsync(Guid personId, CancellationToken cancellationToken)
    {
        SafeguardingEntry entry = await db.Safeguarding.SingleOrDefaultAsync(
                found => found.UserId == personId && found.Source == SafeguardingSources.SafeMinistryCheck, cancellationToken)
            ?? throw new SmcLinkException("They aren't linked to Safe Ministry Check.");
        if (await db.SmcPeople.SingleOrDefaultAsync(person => person.SmcId == entry.ExternalId, cancellationToken) is { } record)
        {
            record.LinkByHandOnly = true;
            record.Why = SmcWhy.ByHandOnly;
        }
        db.Safeguarding.Remove(entry);
        await families.TouchAsync([personId], safeguarding: true, cancellationToken);
        TouchList();
        await db.SaveChangesAsync(cancellationToken);
        await RecountAsync(cancellationToken);
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        SmcSyncState state = await db.SmcSync.SingleOrDefaultAsync(found => found.Id == SmcSyncState.Only, cancellationToken)
            ?? db.SmcSync.Add(new SmcSyncState()).Entity;
        state.LastRunAt = Now;
        IReadOnlyList<SmcRow> rows;
        try
        {
            rows = await client.EveryoneAsync(cancellationToken);
        }
        catch (SmcException problem)
        {
            logger.LogWarning("Syncing from Safe Ministry Check failed: {Problem}", problem.Message);
            state.Problem = problem.Message;
            TouchList();
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        rows = rows.Where(row => row.Id.Length > 0).DistinctBy(row => row.Id).ToList();
        await ApplyAsync(rows, cancellationToken);
        state.Problem = null;
        state.LastSucceededAt = Now;
        state.People = rows.Count;
        TouchList();
        await db.SaveChangesAsync(cancellationToken);
        await RecountAsync(cancellationToken);
    }

    private async Task ApplyAsync(IReadOnlyList<SmcRow> rows, CancellationToken cancellationToken)
    {
        Dictionary<string, SmcPersonRecord> directory = await db.SmcPeople.ToDictionaryAsync(person => person.SmcId, cancellationToken);
        List<SafeguardingEntry> entries = await db.Safeguarding.ToListAsync(cancellationToken);
        Dictionary<string, Guid> linked = entries.Where(entry => entry is { Source: SafeguardingSources.SafeMinistryCheck, ExternalId: not null })
            .ToDictionary(entry => entry.ExternalId!, entry => entry.UserId);

        SmcMatches matches = SmcMatching.Match(
            rows.Select(row => SmcCandidate.From(row, directory.GetValueOrDefault(row.Id)?.LinkByHandOnly ?? false)).ToList(),
            await AppPeopleAsync(cancellationToken),
            linked);

        foreach (SmcMatch match in matches.Found)
        {
            SafeguardingEntry entry = entries.SingleOrDefault(found => found.UserId == match.PersonId)
                ?? db.Safeguarding.Add(new SafeguardingEntry { UserId = match.PersonId, Source = SafeguardingSources.SafeMinistryCheck }).Entity;
            Link(entry, match.SmcId, match.How, by: null);
            entries.Add(entry);
            linked[match.SmcId] = match.PersonId;
        }

        Dictionary<string, SmcRow> byId = rows.ToDictionary(row => row.Id);
        List<Guid> changed = [];
        foreach (SafeguardingEntry entry in entries.Distinct().Where(entry => entry.Source == SafeguardingSources.SafeMinistryCheck))
        {
            SmcValues values = byId.TryGetValue(entry.ExternalId!, out SmcRow? row)
                ? SmcValues.From(row, ChurchTime.Today(time))
                : Gone(entry);
            if (Sync(entry, values))
                changed.Add(entry.UserId);
        }

        DateTime now = Now;
        foreach (SmcRow row in rows)
        {
            SmcPersonRecord record = directory.GetValueOrDefault(row.Id) ?? db.SmcPeople.Add(new SmcPersonRecord { SmcId = row.Id, Name = "" }).Entity;
            record.Name = row.Name;
            record.Email = row["email"] is { Length: > 0 } email ? email : null;
            record.BirthDate = row.Day("dob");
            record.Why = matches.Unmatched.GetValueOrDefault(row.Id);
            record.SeenAt = now;
        }
        db.SmcPeople.RemoveRange(directory.Values.Where(record => !byId.ContainsKey(record.SmcId) && !linked.ContainsKey(record.SmcId)));

        if (changed.Count > 0)
            await families.TouchAsync(changed, safeguarding: true, cancellationToken);
    }

    private bool Sync(SafeguardingEntry entry, SmcValues values)
    {
        values.ApplyTo(entry);
        bool changed = db.Entry(entry).State is EntityState.Added or EntityState.Modified;
        entry.SyncedAt = Now;
        if (changed)
            entry.UpdatedAt = Now;
        return changed;
    }

    private void Link(SafeguardingEntry entry, string smcId, string how, Guid? by)
    {
        entry.Source = SafeguardingSources.SafeMinistryCheck;
        entry.ExternalId = smcId;
        entry.LinkedHow = how;
        entry.LinkedAt = Now;
        entry.LinkedBy = by;
        entry.UpdatedBy = null;
    }

    private static SmcValues Gone(SafeguardingEntry entry) =>
        new(entry.CardNumber, entry.CardStatus, entry.CardExpires, entry.CardChecked, entry.TrainingProgress, entry.TrainingDue,
            entry.OtherTrainingName, entry.OtherTrainingDone, entry.SmcStatus, entry.SmcStatusWords, [SmcReasons.Gone]);

    private async Task<SmcRow?> PersonOrNullAsync(string smcId, CancellationToken cancellationToken)
    {
        try
        {
            return await client.PersonAsync(smcId, cancellationToken);
        }
        catch (SmcException problem)
        {
            logger.LogWarning("Reading one person from Safe Ministry Check failed: {Problem}", problem.Message);
            return null;
        }
    }

    private async Task<IReadOnlyList<AppPerson>> AppPeopleAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> ids = await index.EveryoneAsync(cancellationToken);
        IReadOnlyDictionary<Guid, PersonAccount> live = await accounts.GetAsync(ids, cancellationToken);
        Dictionary<Guid, DateOnly?> born = await db.People.AsNoTracking().Where(person => person.BirthDate != null)
            .ToDictionaryAsync(person => person.UserId, person => person.BirthDate, cancellationToken);
        return (await index.GetAsync(ids, cancellationToken))
            .Where(person => !person.Disabled && live.TryGetValue(person.Id, out PersonAccount? account) && !account.Deleted)
            .Select(person => new AppPerson(person.Id, person.Name, person.Email, born.GetValueOrDefault(person.Id)))
            .ToList();
    }

    private async Task RecountAsync(CancellationToken cancellationToken)
    {
        SmcSyncState state = await db.SmcSync.SingleOrDefaultAsync(found => found.Id == SmcSyncState.Only, cancellationToken)
            ?? db.SmcSync.Add(new SmcSyncState()).Entity;
        List<string> linked = await db.Safeguarding.Where(entry => entry.Source == SafeguardingSources.SafeMinistryCheck).Select(entry => entry.ExternalId!).ToListAsync(cancellationToken);
        state.Linked = linked.Count;
        state.ToMatch = await db.SmcPeople.CountAsync(person => person.Why != null && !linked.Contains(person.SmcId), cancellationToken);
        state.ToCheck = await db.Safeguarding.CountAsync(entry => entry.Source == SafeguardingSources.SafeMinistryCheck && entry.CheckReasons.Count > 0, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private void TouchList() => db.FeedChanges.Add(FeedChangeRecord.Marker(PeopleFeeds.Safeguarding, ListRecordId, Now));
}

internal sealed class SmcLinkException(string message) : Exception(message);
