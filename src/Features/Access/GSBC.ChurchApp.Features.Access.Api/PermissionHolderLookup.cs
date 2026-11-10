using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class PermissionHolderLookup(
    AccessDbContext db, IScopeHierarchy hierarchy, IEnumerable<IScopeAttachments> attachments, HeldThroughRules throughRules, TimeProvider time)
    : IPermissionHolders
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

        IReadOnlyList<HeldThroughRule> rules = await throughRules.ResolveAsync(db, cancellationToken);
        ILookup<Guid, (Guid RoleId, string? ObjectId)> heldBy = people.ToLookup(assignment => assignment.Holder, assignment => (assignment.RoleId, assignment.ObjectId));
        people.AddRange(people.GroupBy(assignment => assignment.Holder)
            .SelectMany(holder => RoleGrants.HeldThrough(holder.Select(assignment => (assignment.RoleId, assignment.ObjectId)), person => heldBy[person], rules)
                .Select(through => (holder.Key, through.RoleId, through.ObjectId)))
            .ToList());

        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync(
            people.Concat(phones).Select(assignment => assignment.RoleId), cancellationToken);

        return await HoldersAsync(people, phones, roles, permission, objectId, hierarchy, cancellationToken, attachments);
    }

    public static async Task<PermissionHolders> HoldersAsync(
        IReadOnlyCollection<(Guid Holder, Guid RoleId, string? ObjectId)> people, IReadOnlyCollection<(Guid Holder, Guid RoleId, string? ObjectId)> phones,
        IReadOnlyDictionary<Guid, RoleRecord> roles, Permission permission, string? objectId, IScopeHierarchy hierarchy, CancellationToken cancellationToken,
        IEnumerable<IScopeAttachments>? attachments = null)
    {
        string? scope = permission.ScopeKind == ScopeKind.Global || string.IsNullOrEmpty(objectId) ? null : objectId;
        List<IScopeAttachments> attached = attachments?.ToList() ?? [];
        return new PermissionHolders(
            await AllowedAsync(people, roles, permission, scope, hierarchy, attached, cancellationToken),
            await AllowedAsync(phones, roles, permission, scope, hierarchy, attached, cancellationToken));
    }

    private static async Task<IReadOnlyList<Guid>> AllowedAsync(
        IReadOnlyCollection<(Guid Holder, Guid RoleId, string? ObjectId)> assignments, IReadOnlyDictionary<Guid, RoleRecord> roles, Permission permission,
        string? scope, IScopeHierarchy hierarchy, List<IScopeAttachments> attachments, CancellationToken cancellationToken)
    {
        List<Guid> allowed = [];
        foreach (IGrouping<Guid, (Guid Holder, Guid RoleId, string? ObjectId)> holder in assignments.GroupBy(assignment => assignment.Holder))
        {
            GrantSet grants = await RoleGrants.CompileAsync(
                holder.Select(assignment => (assignment.RoleId, assignment.ObjectId)), roles, hierarchy, cancellationToken, attachments);
            if (grants.IsAllowed(permission, scope))
                allowed.Add(holder.Key);
        }
        return allowed;
    }
}
