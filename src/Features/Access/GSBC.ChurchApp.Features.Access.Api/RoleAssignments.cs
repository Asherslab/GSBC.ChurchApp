using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class RoleAssignments(AccessDbContext db, AssignmentRules rules, TimeProvider time) : IRoleAssignments
{
    public async ValueTask<AssignmentOutcome> GiveAsync(
        Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        RoleRecord role = await SystemRoleAsync(roleKey, cancellationToken);
        if (await rules.RefusalAsync(actorId, role, objectId, cancellationToken) is { } refusal)
            return AssignmentOutcome.Refused(refusal);

        await AddAsync(userId, role, objectId, expiresAt, AssignmentSource.Manual, actorId, cancellationToken);
        return AssignmentOutcome.Success;
    }

    public async ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        RoleRecord role = await SystemRoleAsync(roleKey, cancellationToken);
        if (await rules.RefusalAsync(actorId, role, objectId, cancellationToken) is { } refusal)
            return AssignmentOutcome.Refused(refusal);

        return await RemoveAsync(userId, role, objectId, cancellationToken);
    }

    public async ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
        await rules.RefusalAsync(actorId, await SystemRoleAsync(roleKey, cancellationToken), objectId, cancellationToken) is null;

    public async ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
        await AddAsync(userId, await SystemRoleAsync(roleKey, cancellationToken), objectId, expiresAt, AssignmentSource.Derived, null, cancellationToken);

    public async ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        AssignmentOutcome outcome = await RemoveAsync(userId, await SystemRoleAsync(roleKey, cancellationToken), objectId, cancellationToken);
        if (!outcome.Done)
            throw new InvalidOperationException(outcome.Refusal);
    }

    public async ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        RoleRecord role = await SystemRoleAsync(roleKey, cancellationToken);
        return await db.ActiveAssignments(Now)
            .Where(assignment => assignment.RoleId == role.Id && assignment.ScopeObjectId == objectId)
            .OrderBy(assignment => assignment.CreatedAt)
            .Select(assignment => new RoleHolder(assignment.UserId, assignment.ExpiresAt))
            .ToListAsync(cancellationToken);
    }

    public async ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => await db.AnnounceChangeAsync(cancellationToken);

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private async Task<RoleRecord> SystemRoleAsync(string roleKey, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking().Include(role => role.Assigners).SingleOrDefaultAsync(role => role.SystemKey == roleKey, cancellationToken)
        ?? throw new InvalidOperationException($"No system role '{roleKey}' is declared.");

    private async Task AddAsync(
        Guid userId, RoleRecord role, string? objectId, DateTime? expiresAt, AssignmentSource source, Guid? createdBy, CancellationToken cancellationToken)
    {
        ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind);
        if (AssignmentRules.ScopeProblem(kind, objectId) is { } problem)
            throw new ArgumentException(problem, nameof(objectId));

        Assignment? existing = await db.Assignments.SingleOrDefaultAsync(assignment =>
            assignment.UserId == userId && assignment.RoleId == role.Id && assignment.ScopeObjectId == objectId, cancellationToken);
        if (existing is null)
        {
            db.Assignments.Add(new Assignment
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                RoleId = role.Id,
                ScopeObjectId = objectId,
                Source = source,
                ExpiresAt = expiresAt,
                CreatedAt = Now,
                CreatedBy = createdBy,
            });
        }
        else
        {
            existing.ExpiresAt = expiresAt;
        }

        await db.SaveChangesAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
    }

    private async Task<AssignmentOutcome> RemoveAsync(Guid userId, RoleRecord role, string? objectId, CancellationToken cancellationToken)
    {
        List<Assignment> held = await db.Assignments
            .Where(assignment => assignment.UserId == userId && assignment.RoleId == role.Id && assignment.ScopeObjectId == objectId)
            .ToListAsync(cancellationToken);
        if (held.Count == 0)
            return AssignmentOutcome.Success;
        if (await rules.LastOwnerAsync(role, cancellationToken) is { } lastOwner)
            return AssignmentOutcome.Refused(lastOwner);

        db.Assignments.RemoveRange(held);
        await db.SaveChangesAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
        return AssignmentOutcome.Success;
    }
}
