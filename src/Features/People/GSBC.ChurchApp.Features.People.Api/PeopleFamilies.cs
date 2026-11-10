using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class PeopleFamilies(PeopleDbContext db, Families families, IPeopleAccounts accounts, IAccessEvaluator access, TimeProvider time) : IFamilies
{
    public async ValueTask<PersonReach> ReachAsync(Guid personId, CancellationToken cancellationToken)
    {
        PersonRecord? record = await db.People.AsNoTracking().SingleOrDefaultAsync(person => person.UserId == personId, cancellationToken);
        List<Guid> parents = await db.Links.AsNoTracking().Where(link => link.ChildId == personId).Select(link => link.ParentId).ToListAsync(cancellationToken);
        bool minor = Ages.IsMinor(record?.BirthDate, ChurchTime.Today(time));
        bool canSignIn = (await accounts.GetAsync([personId], cancellationToken)).GetValueOrDefault(personId)?.CanSignIn ?? false;
        string reach = Reach.Effective(record?.Reach, minor, canSignIn, parents.Count > 0);
        return new PersonReach(personId, minor, Reach.ReachesThemselves(reach), Reach.ReachesParents(reach) ? parents : []);
    }

    public async ValueTask<bool> MayActForAsync(Guid actorId, Guid personId, CancellationToken cancellationToken) =>
        actorId == personId || (await access.GrantsForAsync(actorId, cancellationToken)).IsAllowed(PeoplePermissions.ActFor, personId.ToString());

    public async ValueTask<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken)
    {
        List<Guid> ids = people.Distinct().ToList();
        Dictionary<Guid, DateOnly?> born = await db.People.AsNoTracking().Where(person => ids.Contains(person.UserId))
            .ToDictionaryAsync(person => person.UserId, person => person.BirthDate, cancellationToken);
        List<FamilyLink> links = await db.Links.AsNoTracking().Where(link => ids.Contains(link.ChildId) || ids.Contains(link.ParentId)).ToListAsync(cancellationToken);
        IReadOnlyDictionary<Guid, PersonAccount> accountsById = await accounts.GetAsync(ids, cancellationToken);
        DateOnly today = ChurchTime.Today(time);
        return ids.ToDictionary(id => id, id => new FamilyFacts(
            id,
            born.GetValueOrDefault(id) is { } day ? Ages.YearsOn(day, today) : null,
            accountsById.GetValueOrDefault(id)?.CanSignIn ?? false,
            links.Where(link => link.ChildId == id).Select(link => link.ParentId).ToList(),
            links.Where(link => link.ParentId == id).Select(link => link.ChildId).ToList()));
    }

    public async ValueTask LinkAsync(Guid parentId, Guid childId, Guid linkedBy, CancellationToken cancellationToken) =>
        await families.LinkAsync(parentId, childId, LinkHow.Office, linkedBy, cancellationToken);
}
