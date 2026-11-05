using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class RoleAssignments(AccessDbContext db, AccessChanges changes, AssignmentRules rules, TimeProvider time) : IRoleAssignments, IPhoneRoles
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
            .Select(assignment => new RoleHolder(assignment.UserId))
            .ToListAsync(cancellationToken);
    }

    public async ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => await changes.AnnounceAsync(cancellationToken);

    public async ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken)
    {
        List<Guid> roles = await db.Roles.Where(role => role.ScopeKind == kind.Name).Select(role => role.Id).ToListAsync(cancellationToken);
        List<Assignment> people = await db.Assignments.Where(assignment => roles.Contains(assignment.RoleId) && assignment.ScopeObjectId == fromObjectId).ToListAsync(cancellationToken);
        foreach (Assignment assignment in people)
        {
            if (await db.Assignments.AnyAsync(other => other.UserId == assignment.UserId && other.RoleId == assignment.RoleId && other.ScopeObjectId == toObjectId, cancellationToken))
                db.Assignments.Remove(assignment);
            else
                assignment.ScopeObjectId = toObjectId;
        }

        List<PhoneAssignment> phones = await db.PhoneAssignments.Where(assignment => roles.Contains(assignment.RoleId) && assignment.ScopeObjectId == fromObjectId).ToListAsync(cancellationToken);
        foreach (PhoneAssignment assignment in phones)
        {
            if (await db.PhoneAssignments.AnyAsync(other => other.InstallId == assignment.InstallId && other.RoleId == assignment.RoleId && other.ScopeObjectId == toObjectId, cancellationToken))
                db.PhoneAssignments.Remove(assignment);
            else
                assignment.ScopeObjectId = toObjectId;
        }

        await db.SaveChangesAsync(cancellationToken);
        if (people.Count + phones.Count > 0)
            await changes.AnnounceAsync(cancellationToken);
    }

    async ValueTask IPhoneRoles.GiveAsync(Guid installId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
    {
        RoleRecord role = await PhoneRoleAsync(roleKey, objectId, cancellationToken);
        PhoneAssignment? existing = await db.PhoneAssignments.SingleOrDefaultAsync(assignment =>
            assignment.InstallId == installId && assignment.RoleId == role.Id && assignment.ScopeObjectId == objectId, cancellationToken);
        if (existing is null)
        {
            db.PhoneAssignments.Add(new PhoneAssignment
            {
                Id = Guid.CreateVersion7(),
                InstallId = installId,
                RoleId = role.Id,
                ScopeObjectId = objectId,
                ExpiresAt = expiresAt,
                CreatedAt = Now,
            });
        }
        else
        {
            existing.ExpiresAt = expiresAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    async ValueTask IPhoneRoles.TakeAwayAsync(Guid installId, string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        RoleRecord role = await SystemRoleAsync(roleKey, cancellationToken);
        await db.PhoneAssignments
            .Where(assignment => assignment.InstallId == installId && assignment.RoleId == role.Id && assignment.ScopeObjectId == objectId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    async ValueTask<IReadOnlyList<HeldRole>> IPhoneRoles.HeldAsync(Guid installId, CancellationToken cancellationToken)
    {
        DateTime now = Now;
        return await db.PhoneAssignments.AsNoTracking()
            .Where(assignment => assignment.InstallId == installId && (assignment.ExpiresAt == null || assignment.ExpiresAt > now))
            .Join(db.Roles.Where(role => role.SystemKey != null), assignment => assignment.RoleId, role => role.Id,
                (assignment, role) => new HeldRole(role.SystemKey!, assignment.ScopeObjectId, assignment.ExpiresAt))
            .ToListAsync(cancellationToken);
    }

    async ValueTask IPhoneRoles.MoveToPersonAsync(Guid installId, Guid userId, CancellationToken cancellationToken)
    {
        DateTime now = Now;
        List<PhoneAssignment> held = await db.PhoneAssignments.Where(assignment => assignment.InstallId == installId).ToListAsync(cancellationToken);
        foreach (PhoneAssignment phone in held.Where(assignment => assignment.ExpiresAt == null || assignment.ExpiresAt > now))
        {
            Assignment? existing = await db.Assignments.SingleOrDefaultAsync(assignment =>
                assignment.UserId == userId && assignment.RoleId == phone.RoleId && assignment.ScopeObjectId == phone.ScopeObjectId, cancellationToken);
            if (existing is null)
            {
                db.Assignments.Add(new Assignment
                {
                    Id = Guid.CreateVersion7(),
                    UserId = userId,
                    RoleId = phone.RoleId,
                    ScopeObjectId = phone.ScopeObjectId,
                    Source = AssignmentSource.Derived,
                    ExpiresAt = phone.ExpiresAt,
                    CreatedAt = now,
                });
            }
            else if (existing.ExpiresAt is { } ends && (phone.ExpiresAt is null || phone.ExpiresAt > ends))
            {
                existing.ExpiresAt = phone.ExpiresAt;
            }
        }

        db.PhoneAssignments.RemoveRange(held);
        await db.SaveChangesAsync(cancellationToken);
        if (held.Count > 0)
            await changes.AnnounceAsync(cancellationToken);
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private async Task<RoleRecord> SystemRoleAsync(string roleKey, CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking().Include(role => role.Assigners).SingleOrDefaultAsync(role => role.SystemKey == roleKey, cancellationToken)
        ?? throw new InvalidOperationException($"No system role '{roleKey}' is declared.");

    private async Task<RoleRecord> PhoneRoleAsync(string roleKey, string? objectId, CancellationToken cancellationToken)
    {
        RoleRecord role = await SystemRoleAsync(roleKey, cancellationToken);
        if (!role.PhoneHoldable)
            throw new InvalidOperationException($"The role '{roleKey}' can't be given to a phone.");
        ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind);
        if (AssignmentRules.ScopeProblem(kind, objectId) is { } problem)
            throw new ArgumentException(problem, nameof(objectId));
        return role;
    }

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
        await changes.AnnounceAsync(cancellationToken);
    }

    private async Task<AssignmentOutcome> RemoveAsync(Guid userId, RoleRecord role, string? objectId, CancellationToken cancellationToken)
    {
        List<Assignment> held = await db.Assignments
            .Where(assignment => assignment.UserId == userId && assignment.RoleId == role.Id && assignment.ScopeObjectId == objectId)
            .ToListAsync(cancellationToken);
        if (held.Count == 0)
            return AssignmentOutcome.Success;
        if (await rules.LastOwnerAsync(role, held, cancellationToken) is { } lastOwner)
            return AssignmentOutcome.Refused(lastOwner);

        db.Assignments.RemoveRange(held);
        await db.SaveChangesAsync(cancellationToken);
        await changes.AnnounceAsync(cancellationToken);
        return AssignmentOutcome.Success;
    }
}
