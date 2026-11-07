using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessInsights(AccessDbContext db, PermissionCatalog catalog, IScopeHierarchy hierarchy, TimeProvider time)
{
    private readonly AccessExplanations _explanations = new(hierarchy, catalog);

    public async Task<PersonAbilities> ExplainAsync(Guid userId, CancellationToken cancellationToken)
    {
        List<Assignment> assignments = await ActiveAssignmentsOfAsync([userId], cancellationToken);
        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync(assignments.Select(assignment => assignment.RoleId), cancellationToken);
        return await _explanations.ExplainAsync(assignments, roles, cancellationToken);
    }

    public async Task<RoleEffect> PreviewAsync(RoleRecord? changed, Guid roleId, CancellationToken cancellationToken)
    {
        List<Guid> affectedRoles = [roleId, .. await db.RoleIncludes.AsNoTracking()
            .Where(include => include.IncludedRoleId == roleId).Select(include => include.RoleId).ToListAsync(cancellationToken)];
        List<Guid> userIds = await db.ActiveAssignments(Now).Where(assignment => affectedRoles.Contains(assignment.RoleId))
            .Select(assignment => assignment.UserId).Distinct().ToListAsync(cancellationToken);
        List<Assignment> assignments = await ActiveAssignmentsOfAsync(userIds, cancellationToken);

        IEnumerable<Guid> loadIds = assignments.Select(assignment => assignment.RoleId)
            .Concat(changed?.Includes.Select(include => include.IncludedRoleId) ?? []);
        Dictionary<Guid, RoleRecord> before = await db.LoadRolesWithIncludesAsync(loadIds, cancellationToken);
        return await _explanations.PreviewAsync(assignments, before, roleId, changed, cancellationToken);
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private Task<List<Assignment>> ActiveAssignmentsOfAsync(List<Guid> userIds, CancellationToken cancellationToken) =>
        db.ActiveAssignments(Now).Where(assignment => userIds.Contains(assignment.UserId)).ToListAsync(cancellationToken);
}
