using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class PermissionHolders(AccessDbContext db, IAccessEvaluator evaluator, PermissionCatalog catalog, TimeProvider time) : IPermissionHolders
{
    public async ValueTask<IReadOnlyList<Guid>> HoldersOfAsync(string node, CancellationToken cancellationToken)
    {
        if (!catalog.ByNode.TryGetValue(node, out Permission? permission))
            return [];

        List<Guid> users = await db.ActiveAssignments(time.GetUtcNow().UtcDateTime)
            .Select(assignment => assignment.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        List<Guid> holders = [];
        foreach (Guid user in users)
            if ((await evaluator.GrantsForAsync(user, cancellationToken)).IsAllowed(permission))
                holders.Add(user);
        return holders;
    }
}
