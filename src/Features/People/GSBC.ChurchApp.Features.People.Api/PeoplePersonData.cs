using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class PeoplePersonData(PeopleDbContext db, Families families, IPeopleAccounts accounts, IRoleAssignments roles) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (Guid child in await LeftAloneAsync(userId, cancellationToken))
            await accounts.RemoveAsync(child, userId, cancellationToken);

        List<FamilyLink> links = await db.Links.Where(link => link.ParentId == userId || link.ChildId == userId).ToListAsync(cancellationToken);
        List<Guid> family = links.Select(link => link.ParentId == userId ? link.ChildId : link.ParentId).Distinct().ToList();
        db.Links.RemoveRange(links);
        await db.Asks.Where(ask => ask.ChildId == userId || ask.AskedBy == userId).ExecuteDeleteAsync(cancellationToken);
        await db.People.Where(person => person.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Safeguarding.Where(entry => entry.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await families.TouchAsync([userId, .. family], safeguarding: true, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await roles.ForgetScopeAsync(ScopeKind.Person, [userId.ToString()], cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> LeftAloneAsync(Guid userId, CancellationToken cancellationToken)
    {
        List<Guid> children = await db.Links.Where(link => link.ParentId == userId).Select(link => link.ChildId).ToListAsync(cancellationToken);
        if (children.Count == 0)
            return [];
        List<FamilyLink> links = await db.Links.AsNoTracking().Where(link => children.Contains(link.ChildId)).ToListAsync(cancellationToken);
        return FamilyRules.LeftAlone(userId, links, await accounts.GetAsync(children, cancellationToken));
    }
}
