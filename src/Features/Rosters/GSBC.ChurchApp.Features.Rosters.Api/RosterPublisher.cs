using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RosterPublisher(RostersDbContext db, RosterViews views, IMinistryFacts ministries, IFamilies families, IPeopleIndex people, TimeProvider time)
{
    private const long PublishLockKey = 0x7273_7472_7075_626c;

    public async Task PublishAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<MinistryFacts> inUse = await ministries.UsingAsync(RosterParts.Key, cancellationToken);
        List<Guid> published = await db.Published.AsNoTracking().Where(row => row.MinistryId != null).Select(row => row.MinistryId!.Value).Distinct()
            .ToListAsync(cancellationToken);
        foreach (Guid ministry in inUse.Select(each => each.Id).Union(published))
            await PublishAsync(ministry, cancellationToken);

        List<Guid> away = await db.Away.AsNoTracking().Select(span => span.PersonId)
            .Union(db.Published.AsNoTracking().Where(row => row.Feed == RostersFeeds.Away).Select(row => row.ScopeId))
            .ToListAsync(cancellationToken);
        await PublishAwayAsync(away, cancellationToken);
    }

    public async Task PublishAsync(Guid ministryId, CancellationToken cancellationToken)
    {
        Dictionary<string, (Guid Scope, byte[] Payload)> wanted = [];
        if (await views.LoadAsync(ministryId, cancellationToken) is { } book && book.Ministry.Uses(RosterParts.Key))
            foreach ((Guid person, Recipient recipient) in book.Recipients())
                wanted[$"{person:N}-{ministryId:N}"] = (person, RostersPayloads.Encode(book.ViewFor(person, recipient)));

        if (await WriteAsync(RostersFeeds.Ministries, ministryId, wanted, row => row.MinistryId == ministryId, cancellationToken))
        {
            FeedChangeRecord marker = FeedChangeRecord.Marker(RostersFeeds.Changes, ministryId.ToString(), time.GetUtcNow().UtcDateTime);
            marker.ScopeId = ministryId.ToString();
            db.FeedChanges.Add(marker);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task PublishAwayAsync(IReadOnlyCollection<Guid> changed, CancellationToken cancellationToken)
    {
        if (changed.Count == 0)
            return;

        IReadOnlyDictionary<Guid, FamilyFacts> changedFacts = await families.FactsAsync(changed, cancellationToken);
        List<Guid> recipients = changed.Concat(changedFacts.Values.Where(fact => fact.Minor).SelectMany(fact => fact.Parents)).Distinct().ToList();
        IReadOnlyDictionary<Guid, FamilyFacts> facts = await families.FactsAsync(recipients, cancellationToken);
        Dictionary<Guid, List<Guid>> covers = recipients.ToDictionary(person => person,
            person => facts.GetValueOrDefault(person)?.Children.Prepend(person).Distinct().ToList() ?? [person]);
        List<Guid> everyone = covers.Values.SelectMany(each => each).Distinct().ToList();
        DateOnly today = ChurchTime.Today(time);
        ILookup<Guid, AwayRecord> spans = (await db.Away.AsNoTracking().Where(span => everyone.Contains(span.PersonId) && span.To >= today)
            .OrderBy(span => span.From).ToListAsync(cancellationToken)).ToLookup(span => span.PersonId);
        IReadOnlyDictionary<Guid, FamilyFacts> coveredFacts = await families.FactsAsync(everyone, cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(everyone, cancellationToken);

        Dictionary<string, (Guid Scope, byte[] Payload)> wanted = [];
        foreach ((Guid recipient, List<Guid> household) in covers)
        {
            List<Guid> shown = household.Where(person => person == recipient || coveredFacts.GetValueOrDefault(person) is { Minor: true }).ToList();
            if (!shown.Any(person => spans[person].Any()))
                continue;
            AwayList list = new()
            {
                People = shown.Select(person => new AwayOf
                {
                    PersonId = person.ToString(), Name = names.GetValueOrDefault(person) ?? "Someone",
                    Spans = spans[person].Select(span => new AwaySpan { Id = span.Id.ToString(), From = RosterBook.Wire(span.From), To = RosterBook.Wire(span.To), Note = span.Note })
                        .ToList(),
                }).ToList(),
            };
            wanted[$"{recipient:N}"] = (recipient, RostersPayloads.Encode(list));
        }

        await WriteAsync(RostersFeeds.Away, null, wanted, row => recipients.Contains(row.ScopeId), cancellationToken);
    }

    public async Task ForgetAsync(Guid person, CancellationToken cancellationToken) =>
        await WriteAsync(RostersFeeds.Away, null, [], row => row.ScopeId == person, cancellationToken, RostersFeeds.Ministries);

    private async Task<bool> WriteAsync(string feed, Guid? ministryId, Dictionary<string, (Guid Scope, byte[] Payload)> wanted,
        System.Linq.Expressions.Expression<Func<PublishedRecord, bool>> mine, CancellationToken cancellationToken, string? alsoFeed = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({PublishLockKey})", cancellationToken);

        List<PublishedRecord> existing = await db.Published.Where(row => row.Feed == feed || row.Feed == alsoFeed).Where(mine).ToListAsync(cancellationToken);
        long version = time.GetUtcNow().UtcDateTime.Ticks;
        foreach ((string recordId, (Guid scope, byte[] payload)) in wanted)
        {
            string hash = RostersPayloads.Hash(payload);
            PublishedRecord? row = existing.SingleOrDefault(each => each.Feed == feed && each.RecordId == recordId);
            if (row?.Hash == hash)
                continue;
            if (row is null)
                db.Published.Add(new PublishedRecord { Feed = feed, RecordId = recordId, ScopeId = scope, MinistryId = ministryId, Hash = hash });
            else
                row.Hash = hash;
            db.FeedChanges.Add(new FeedChangeRecord { Feed = feed, RecordId = recordId, ScopeId = scope.ToString(), Version = version, Payload = payload });
        }

        foreach (PublishedRecord gone in existing.Where(row => !(row.Feed == feed && wanted.ContainsKey(row.RecordId))))
        {
            db.Published.Remove(gone);
            db.FeedChanges.Add(new FeedChangeRecord { Feed = gone.Feed, RecordId = gone.RecordId, ScopeId = gone.ScopeId.ToString(), Version = version, Deleted = true });
        }

        bool changed = db.ChangeTracker.HasChanges();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return changed;
    }
}
