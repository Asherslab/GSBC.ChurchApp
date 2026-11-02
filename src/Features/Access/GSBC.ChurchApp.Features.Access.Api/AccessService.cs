using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessService(
    AccessDbContext db, IAccessEvaluator evaluator, AssignmentRules rules, AccessInsights insights, PermissionCatalog catalog, TimeProvider time)
    : IAccessService
{
    public const string OwnerKey = "owner";
    private const int MaximumNameLength = 60;

    public async ValueTask<GrantList> GetMineAsync(Nothing request, CallContext context = default)
    {
        GrantSet grants = await evaluator.GrantsForAsync(CallerId(context), context.CancellationToken);
        return new GrantList
        {
            Grants = grants.Grants.Select(grant => new GrantMessage
            {
                Pattern = grant.Pattern.Text,
                ScopeKind = grant.Scope.Kind.Name,
                ObjectId = grant.Scope.ObjectId,
            }).ToList(),
        };
    }

    public ValueTask<Catalog> ListCatalogAsync(Nothing request, CallContext context = default) => ValueTask.FromResult(new Catalog
    {
        Permissions = catalog.ByNode.Values.OrderBy(permission => permission.Area).ThenBy(permission => permission.Node).Select(permission => new PermissionInfo
        {
            Node = permission.Node,
            ScopeKind = permission.ScopeKind.Name,
            Description = permission.Description,
            Sensitive = permission.IsSensitive,
            Area = permission.Area,
        }).ToList(),
        ScopeKinds = ScopeKind.Known.Select(kind => kind.Name).ToList(),
    });

    public async ValueTask<RoleList> ListRolesAsync(Nothing request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Dictionary<Guid, RoleRecord> roles = await db.Roles.AsNoTracking()
            .Include(role => role.Patterns).Include(role => role.Includes).Include(role => role.Assigners)
            .ToDictionaryAsync(role => role.Id, cancellationToken);
        Dictionary<Guid, int> holders = await db.ActiveAssignments(Now)
            .GroupBy(assignment => assignment.RoleId)
            .Select(group => new { group.Key, Count = group.Select(assignment => assignment.UserId).Distinct().Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

        return new RoleList
        {
            Roles = roles.Values
                .OrderBy(role => role.SystemKey == null).ThenBy(role => role.Name)
                .Select(role => ToInfo(role, roles, holders.GetValueOrDefault(role.Id)))
                .ToList(),
        };
    }

    public async ValueTask<RoleInfo> SaveRoleAsync(RoleInfo request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        RoleRecord? role = string.IsNullOrEmpty(request.Id) ? null : await LoadRoleAsync(ParseId(request.Id), cancellationToken);
        RoleRecord draft = await DraftAsync(request, role, cancellationToken);

        Dictionary<Guid, RoleRecord> included = await db.LoadRolesWithIncludesAsync(draft.Includes.Select(include => include.IncludedRoleId), cancellationToken);
        ScopeKind.TryParse(draft.ScopeKind, out ScopeKind kind);
        GrantSet actor = await evaluator.GrantsForAsync(CallerId(context), cancellationToken);
        List<Permission> notHeld = RoleGrants.NotHeld(actor, RoleGrants.PatternsOf(draft, included), kind, null, catalog);
        if (notHeld.Count > 0)
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                $"A role can only include what you can do yourself. You can't: {string.Join(", ", notHeld.Select(permission => permission.Description))}."));

        if (role is null)
        {
            role = new RoleRecord { Id = draft.Id, Name = draft.Name, ScopeKind = draft.ScopeKind, CreatedAt = Now };
            db.Roles.Add(role);
        }
        role.Name = draft.Name;
        role.Description = draft.Description;
        role.ScopeKind = draft.ScopeKind;
        role.AppliesToChildren = draft.AppliesToChildren;
        role.Patterns.Clear();
        role.Patterns.AddRange(draft.Patterns);
        role.Includes.Clear();
        role.Includes.AddRange(draft.Includes);
        role.Assigners.Clear();
        role.Assigners.AddRange(draft.Assigners);

        await db.SaveChangesAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
        Dictionary<Guid, RoleRecord> saved = await db.LoadRolesWithIncludesAsync([role.Id], cancellationToken);
        return ToInfo(saved[role.Id], saved, await HolderCountAsync(role.Id, cancellationToken));
    }

    public async ValueTask<RoleEffect> PreviewRoleAsync(RolePreviewRequest request, CallContext context = default)
    {
        if (string.IsNullOrEmpty(request.Role.Id))
            return new RoleEffect();

        CancellationToken cancellationToken = context.CancellationToken;
        RoleRecord role = await LoadRoleAsync(ParseId(request.Role.Id), cancellationToken);
        RoleRecord? changed = request.Deleting ? null : await DraftAsync(request.Role, role, cancellationToken);
        return await insights.PreviewAsync(changed, role.Id, cancellationToken);
    }

    public async ValueTask<Done> DeleteRoleAsync(RoleReference request, CallContext context = default)
    {
        RoleRecord role = await LoadRoleAsync(ParseId(request.Id), context.CancellationToken);
        if (role.IsSystem)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Built-in roles come from the app and can't be deleted."));

        db.Roles.Remove(role);
        await db.SaveChangesAsync(context.CancellationToken);
        await db.AnnounceChangeAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<AssignmentList> ListAssignmentsAsync(AssignmentQuery request, CallContext context = default)
    {
        IQueryable<Assignment> query = db.Assignments.AsNoTracking().Include(assignment => assignment.Role);
        if (!string.IsNullOrEmpty(request.UserId))
            query = query.Where(assignment => assignment.UserId == ParseId(request.UserId));

        return new AssignmentList
        {
            Assignments = (await query.OrderBy(assignment => assignment.CreatedAt).ToListAsync(context.CancellationToken))
                .Select(ToInfo).ToList(),
        };
    }

    public async ValueTask<PersonAbilities> ExplainAsync(PersonReference request, CallContext context = default) =>
        await insights.ExplainAsync(ParseId(request.UserId), context.CancellationToken);

    public async ValueTask<AssignmentInfo> AssignAsync(AssignRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid userId = ParseId(request.UserId);
        RoleRecord role = await LoadRoleAsync(ParseId(request.RoleId), cancellationToken);
        ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind);

        string? scopeObjectId = string.IsNullOrWhiteSpace(request.ScopeObjectId) ? null : request.ScopeObjectId.Trim();
        if (AssignmentRules.ScopeProblem(kind, scopeObjectId) is { } problem)
            throw Invalid(problem);
        if (request.ExpiresAt is { } expires && expires <= Now)
            throw Invalid("The end date has to be in the future.");
        if (await rules.RefusalAsync(CallerId(context), role, scopeObjectId, cancellationToken) is { } refusal)
            throw new RpcException(new Status(StatusCode.PermissionDenied, refusal));

        Assignment assignment = new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            RoleId = role.Id,
            ScopeObjectId = scopeObjectId,
            Source = AssignmentSource.Manual,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = Now,
            CreatedBy = CallerId(context),
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
        assignment.Role = role;
        return ToInfo(assignment);
    }

    public async ValueTask<Done> UnassignAsync(AssignmentReference request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Assignment assignment = await db.Assignments.Include(existing => existing.Role).ThenInclude(role => role!.Assigners)
            .SingleOrDefaultAsync(existing => existing.Id == ParseId(request.Id), cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That role was already taken away."));

        if (await rules.RefusalAsync(CallerId(context), assignment.Role!, assignment.ScopeObjectId, cancellationToken) is { } refusal)
            throw new RpcException(new Status(StatusCode.PermissionDenied, refusal));
        if (await rules.LastOwnerAsync(assignment.Role!, cancellationToken) is { } lastOwner)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, lastOwner));

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
        return new Done();
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private async Task<RoleRecord> LoadRoleAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Roles.Include(role => role.Patterns).Include(role => role.Includes).Include(role => role.Assigners)
            .SingleOrDefaultAsync(role => role.Id == id, cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, "That role no longer exists."));

    private async Task<RoleRecord> DraftAsync(RoleInfo request, RoleRecord? existing, CancellationToken cancellationToken)
    {
        if (existing?.IsSystem == true)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Built-in roles come from the app. Make a copy to change it."));

        string name = request.Name.Trim();
        if (name.Length is 0 or > MaximumNameLength)
            throw Invalid("Give the role a name (up to 60 characters).");
        if (!ScopeKind.TryParse(request.ScopeKind, out ScopeKind kind))
            throw Invalid($"'{request.ScopeKind}' isn't a kind of place a role is given for.");
        if (existing is not null && existing.ScopeKind != kind.Name && await HolderCountAsync(existing.Id, cancellationToken) > 0)
            throw Invalid("People have this role, so where it applies can't change. Make a copy instead.");

        List<string> patterns = request.Patterns.Select(pattern => pattern.Trim()).Where(pattern => pattern.Length > 0).Distinct().ToList();
        List<string> invalid = patterns.Where(pattern => !PermissionPattern.TryParse(pattern, out _)).ToList();
        if (invalid.Count > 0)
            throw Invalid($"Not valid patterns: {string.Join(", ", invalid)}.");

        Guid id = existing?.Id ?? Guid.CreateVersion7();
        List<Guid> includes = request.IncludedRoleIds.Select(ParseId).Distinct().ToList();
        List<Guid> assigners = request.AssignableByRoleIds.Select(ParseId).Distinct().ToList();
        Dictionary<Guid, RoleRecord> others = await db.Roles.AsNoTracking().Include(other => other.Includes)
            .Where(other => includes.Contains(other.Id) || assigners.Contains(other.Id)).ToDictionaryAsync(other => other.Id, cancellationToken);
        if (includes.Any(other => other == id || !others.ContainsKey(other)))
            throw Invalid("A role can only include other roles that exist.");
        if (includes.Any(other => others[other].Includes.Count > 0))
            throw Invalid("Included roles can't include roles themselves (one level only).");
        if (assigners.Any(other => !others.ContainsKey(other)))
            throw Invalid("Only existing roles can be chosen to give this role.");

        return new RoleRecord
        {
            Id = id,
            Name = name,
            Description = request.Description.Trim(),
            ScopeKind = kind.Name,
            AppliesToChildren = request.AppliesToChildren && kind != ScopeKind.Global,
            Patterns = patterns.Select(pattern => new RolePattern { RoleId = id, Pattern = pattern }).ToList(),
            Includes = includes.Select(other => new RoleInclude { RoleId = id, IncludedRoleId = other }).ToList(),
            Assigners = assigners.Select(other => new RoleAssigner { RoleId = id, AssignerRoleId = other }).ToList(),
        };
    }

    private Task<int> HolderCountAsync(Guid roleId, CancellationToken cancellationToken) =>
        db.ActiveAssignments(Now).Where(assignment => assignment.RoleId == roleId).Select(assignment => assignment.UserId).Distinct().CountAsync(cancellationToken);

    private static Guid CallerId(CallContext context) =>
        Caller.UserId(context.ServerCallContext!.GetHttpContext().User)
        ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't a valid id.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private RoleInfo ToInfo(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles, int holderCount)
    {
        ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind);
        return new RoleInfo
        {
            Id = role.Id.ToString(),
            Name = role.Name,
            Description = role.Description,
            ScopeKind = role.ScopeKind,
            IsSystem = role.IsSystem,
            Patterns = role.Patterns.Select(pattern => pattern.Pattern).Order().ToList(),
            IncludedRoleIds = role.Includes.Select(include => include.IncludedRoleId.ToString()).ToList(),
            AbilityNodes = RoleGrants.Abilities(RoleGrants.PatternsOf(role, roles), kind, null, catalog).Select(permission => permission.Node).ToList(),
            AppliesToChildren = role.AppliesToChildren,
            AssignableByRoleIds = role.Assigners.Select(assigner => assigner.AssignerRoleId.ToString()).ToList(),
            HolderCount = holderCount,
        };
    }

    private static AssignmentInfo ToInfo(Assignment assignment) => new()
    {
        Id = assignment.Id.ToString(),
        UserId = assignment.UserId.ToString(),
        RoleId = assignment.RoleId.ToString(),
        RoleName = assignment.Role?.Name ?? "",
        ScopeObjectId = assignment.ScopeObjectId,
        ExpiresAt = assignment.ExpiresAt,
        Source = assignment.Source.ToString(),
        CreatedAt = assignment.CreatedAt,
    };
}
