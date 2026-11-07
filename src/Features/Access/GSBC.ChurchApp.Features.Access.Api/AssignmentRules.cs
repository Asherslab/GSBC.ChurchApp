using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AssignmentRules(
    AccessDbContext db, IAccessEvaluator evaluator, PermissionCatalog catalog, IScopeHierarchy hierarchy, ToolCallers tools, TimeProvider time)
{
    private readonly AssignmentPolicy _policy = new(hierarchy, catalog);

    public static string? ScopeProblem(ScopeKind kind, string? objectId) =>
        (kind == ScopeKind.Global, objectId is null) switch
        {
            (true, false) => "This role applies everywhere, so it isn't given for one place.",
            (false, true) => $"Choose which {kind.Name} this role is for.",
            _ => null,
        };

    public async Task<string?> RefusalAsync(Guid actorId, RoleRecord role, string? objectId, CancellationToken cancellationToken)
    {
        List<Guid> assignerIds = role.Assigners.Select(assigner => assigner.AssignerRoleId).ToList();
        Assigner actor = new(
            await evaluator.GrantsForAsync(actorId, cancellationToken),
            tools.ActsThroughATool(actorId),
            await db.ActiveAssignments(time.GetUtcNow().UtcDateTime)
                .Where(assignment => assignment.UserId == actorId && assignerIds.Contains(assignment.RoleId))
                .ToListAsync(cancellationToken));
        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync([role.Id, .. assignerIds], cancellationToken);
        return await _policy.RefusalAsync(actor, role, objectId, roles, cancellationToken);
    }

    public async Task<string?> LastOwnerAsync(RoleRecord role, IReadOnlyCollection<Assignment> removing, CancellationToken cancellationToken)
    {
        if (role.SystemKey != AccessService.OwnerKey)
            return null;

        DateTime now = time.GetUtcNow().UtcDateTime;
        List<Assignment> owners = await db.ActiveAssignments(now).Where(assignment => assignment.RoleId == role.Id).ToListAsync(cancellationToken);
        return AssignmentPolicy.LastOwner(role, removing, owners, now);
    }
}
