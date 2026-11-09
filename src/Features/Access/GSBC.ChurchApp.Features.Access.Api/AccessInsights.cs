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

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private Task<List<Assignment>> ActiveAssignmentsOfAsync(List<Guid> userIds, CancellationToken cancellationToken) =>
        db.ActiveAssignments(Now).Where(assignment => userIds.Contains(assignment.UserId)).ToListAsync(cancellationToken);
}
