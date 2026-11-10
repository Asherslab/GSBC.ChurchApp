using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class MyFamilyFeed(PeopleDbContext db, FamilyViews views, IPeopleAccounts accounts, TimeProvider time)
{
    public async Task PublishAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken)
    {
        List<Guid> family = await db.Links.AsNoTracking().Where(link => people.Contains(link.ChildId) || people.Contains(link.ParentId))
            .Select(link => new[] { link.ChildId, link.ParentId }).ToListAsync(cancellationToken) is var links ? links.SelectMany(pair => pair).ToList() : [];
        List<Guid> everyone = people.Concat(family).Distinct().ToList();
        List<string> ids = everyone.Select(person => person.ToString()).ToList();
        Dictionary<string, FeedChangeRecord> published = (await db.FeedChanges.AsNoTracking()
                .Where(change => change.Feed == PeopleFeeds.Me && ids.Contains(change.RecordId))
                .GroupBy(change => change.RecordId)
                .Select(versions => versions.OrderByDescending(change => change.Cursor).First())
                .ToListAsync(cancellationToken))
            .ToDictionary(change => change.RecordId);
        long version = time.GetUtcNow().UtcDateTime.Ticks;
        IReadOnlyDictionary<Guid, PersonAccount> living = await accounts.GetAsync(everyone, cancellationToken);
        foreach (Guid person in everyone)
        {
            if (living.GetValueOrDefault(person) is not { Deleted: false })
            {
                if (published.GetValueOrDefault(person.ToString()) is { Deleted: false })
                    db.FeedChanges.Add(new FeedChangeRecord { Feed = PeopleFeeds.Me, RecordId = person.ToString(), ScopeId = person.ToString(), Version = version, Deleted = true });
                continue;
            }
            using MemoryStream stream = new();
            PeopleContractsModel.Instance.Serialize(stream, new MyPeople { Me = await views.FamilyAsync(person, Shown.Own, cancellationToken) });
            byte[] payload = stream.ToArray();
            if (published.GetValueOrDefault(person.ToString()) is { Deleted: false } before && before.Payload.AsSpan().SequenceEqual(payload))
                continue;
            db.FeedChanges.Add(new FeedChangeRecord { Feed = PeopleFeeds.Me, RecordId = person.ToString(), ScopeId = person.ToString(), Version = version, Payload = payload });
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
