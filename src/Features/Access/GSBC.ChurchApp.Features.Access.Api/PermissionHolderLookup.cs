using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class PermissionHolderLookup(AccessDbContext db, IScopeHierarchy hierarchy, TimeProvider time) : IPermissionHolders
{
    public async ValueTask<PermissionHolders> HoldersAsync(Permission permission, string? objectId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<(Guid Holder, Guid RoleId, string? ObjectId)> people = (await db.ActiveAssignments(now)
                .Select(assignment => new { assignment.UserId, assignment.RoleId, assignment.ScopeObjectId })
                .ToListAsync(cancellationToken))
            .Select(assignment => (assignment.UserId, assignment.RoleId, assignment.ScopeObjectId)).ToList();
        List<(Guid Holder, Guid RoleId, string? ObjectId)> phones = (await db.PhoneAssignments.AsNoTracking()
                .Where(assignment => assignment.ExpiresAt == null || assignment.ExpiresAt > now)
                .Select(assignment => new { assignment.InstallId, assignment.RoleId, assignment.ScopeObjectId })
                .ToListAsync(cancellationToken))
            .Select(assignment => (assignment.InstallId, assignment.RoleId, assignment.ScopeObjectId)).ToList();

        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync(
            people.Concat(phones).Select(assignment => assignment.RoleId), cancellationToken);
        string? scope = permission.ScopeKind == ScopeKind.Global || string.IsNullOrEmpty(objectId) ? null : objectId;

        return new PermissionHolders(
            await AllowedAsync(people, roles, permission, scope, cancellationToken),
            await AllowedAsync(phones, roles, permission, scope, cancellationToken));
    }

    private async Task<IReadOnlyList<Guid>> AllowedAsync(
        List<(Guid Holder, Guid RoleId, string? ObjectId)> assignments, Dictionary<Guid, RoleRecord> roles, Permission permission, string? scope,
        CancellationToken cancellationToken)
    {
        List<Guid> allowed = [];
        foreach (IGrouping<Guid, (Guid Holder, Guid RoleId, string? ObjectId)> holder in assignments.GroupBy(assignment => assignment.Holder))
        {
            GrantSet grants = await RoleGrants.CompileAsync(
                holder.Select(assignment => (assignment.RoleId, assignment.ObjectId)), roles, hierarchy, cancellationToken);
            if (grants.IsAllowed(permission, scope))
                allowed.Add(holder.Key);
        }
        return allowed;
    }
}
