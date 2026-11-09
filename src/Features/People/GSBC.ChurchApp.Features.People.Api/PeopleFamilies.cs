using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class PeopleFamilies(PeopleDbContext db, IPeopleAccounts accounts, IAccessEvaluator access, TimeProvider time) : IFamilies
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
}
