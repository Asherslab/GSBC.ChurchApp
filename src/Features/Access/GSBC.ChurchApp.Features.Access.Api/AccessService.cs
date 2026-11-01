using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessService(
    AccessDbContext db, IAccessEvaluator evaluator, PermissionCatalog catalog, TimeProvider time) : IAccessService
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

    public async ValueTask<Catalog> ListCatalogAsync(Nothing request, CallContext context = default) => new()
    {
        Permissions = await db.Catalog.AsNoTracking().OrderBy(entry => entry.Node).Select(entry => new PermissionInfo
        {
            Node = entry.Node,
            ScopeKind = entry.ScopeKind,
            Description = entry.Description,
            Sensitive = entry.Sensitive,
        }).ToListAsync(context.CancellationToken),
        ScopeKinds = ScopeKind.Known.Select(kind => kind.Name).ToList(),
    };

    public async ValueTask<RoleList> ListRolesAsync(Nothing request, CallContext context = default) => new()
    {
        Roles = (await db.Roles.AsNoTracking().Include(role => role.Patterns).Include(role => role.Includes)
                .OrderBy(role => role.SystemKey == null).ThenBy(role => role.Name)
                .ToListAsync(context.CancellationToken))
            .Select(ToInfo).ToList(),
    };

    public async ValueTask<RoleInfo> SaveRoleAsync(RoleInfo request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        string name = request.Name.Trim();
        if (name.Length is 0 or > MaximumNameLength)
            throw Invalid("Give the role a name (up to 60 characters).");
        if (!ScopeKind.TryParse(request.ScopeKind, out ScopeKind kind))
            throw Invalid($"'{request.ScopeKind}' isn't a scope kind.");

        List<string> patterns = request.Patterns.Select(pattern => pattern.Trim()).Where(pattern => pattern.Length > 0).Distinct().ToList();
        List<string> invalid = patterns.Where(pattern => !PermissionPattern.TryParse(pattern, out _)).ToList();
        if (invalid.Count > 0)
            throw Invalid($"Not valid patterns: {string.Join(", ", invalid)}.");

        RoleRecord? role = null;
        if (!string.IsNullOrEmpty(request.Id))
        {
            role = await db.Roles.Include(existing => existing.Patterns).Include(existing => existing.Includes)
                .SingleOrDefaultAsync(existing => existing.Id == ParseId(request.Id), cancellationToken)
                ?? throw new RpcException(new Status(StatusCode.NotFound, "That role no longer exists."));
            if (role.IsSystem)
                throw new RpcException(new Status(StatusCode.FailedPrecondition, "System roles come from the app and can't be changed."));
        }

        List<Guid> includes = request.IncludedRoleIds.Select(ParseId).Distinct().ToList();
        Dictionary<Guid, RoleRecord> included = await db.Roles.AsNoTracking().Include(other => other.Patterns).Include(other => other.Includes)
            .Where(other => includes.Contains(other.Id)).ToDictionaryAsync(other => other.Id, cancellationToken);
        if (included.Count != includes.Count || (role is not null && includes.Contains(role.Id)))
            throw Invalid("A role can only include other roles that exist.");
        if (included.Values.Any(other => other.Includes.Count > 0))
            throw Invalid("Included roles can't include roles themselves (one level only).");

        GrantSet actor = await evaluator.GrantsForAsync(CallerId(context), cancellationToken);
        List<string> notHeld = RoleGrants.NotHeld(actor,
            patterns.Concat(included.Values.SelectMany(other => other.Patterns.Select(pattern => pattern.Pattern))), kind, null, catalog);
        if (notHeld.Count > 0)
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                $"You can only build roles from permissions you hold yourself. Not yours: {string.Join(", ", notHeld)}."));

        if (role is null)
        {
            role = new RoleRecord { Id = Guid.CreateVersion7(), Name = name, ScopeKind = kind.Name, CreatedAt = Now };
            db.Roles.Add(role);
        }
        role.Name = name;
        role.Description = request.Description.Trim();
        role.ScopeKind = kind.Name;
        role.Patterns.Clear();
        role.Patterns.AddRange(patterns.Select(pattern => new RolePattern { RoleId = role.Id, Pattern = pattern }));
        role.Includes.Clear();
        role.Includes.AddRange(includes.Select(id => new RoleInclude { RoleId = role.Id, IncludedRoleId = id }));

        await db.SaveChangesAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
        return ToInfo(role);
    }

    public async ValueTask<Done> DeleteRoleAsync(RoleReference request, CallContext context = default)
    {
        RoleRecord role = await db.Roles.SingleOrDefaultAsync(existing => existing.Id == ParseId(request.Id), context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That role no longer exists."));
        if (role.IsSystem)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "System roles come from the app and can't be deleted."));

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

    public async ValueTask<AssignmentInfo> AssignAsync(AssignRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid userId = ParseId(request.UserId);
        RoleRecord role = await db.Roles.AsNoTracking().Include(existing => existing.Patterns).Include(existing => existing.Includes)
            .SingleOrDefaultAsync(existing => existing.Id == ParseId(request.RoleId), cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That role no longer exists."));
        ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind);

        string? scopeObjectId = string.IsNullOrWhiteSpace(request.ScopeObjectId) ? null : request.ScopeObjectId.Trim();
        if (kind == ScopeKind.Global && scopeObjectId is not null)
            throw Invalid("This role applies everywhere, so it doesn't take a scope.");
        if (kind != ScopeKind.Global && scopeObjectId is null)
            throw Invalid($"Choose which {kind.Name} this role is for.");
        if (request.ExpiresAt is { } expires && expires <= Now)
            throw Invalid("The end date has to be in the future.");

        Dictionary<Guid, RoleRecord> roles = await db.LoadRolesWithIncludesAsync([role.Id], cancellationToken);
        GrantSet actor = await evaluator.GrantsForAsync(CallerId(context), cancellationToken);
        List<string> notHeld = RoleGrants.NotHeld(actor, AccessEvaluator.PatternsOf(roles[role.Id], roles), kind, scopeObjectId, catalog);
        if (notHeld.Count > 0)
            throw new RpcException(new Status(StatusCode.PermissionDenied,
                $"You can only give roles whose permissions you hold yourself. Not yours: {string.Join(", ", notHeld)}."));

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
        Assignment assignment = await db.Assignments.Include(existing => existing.Role)
            .SingleOrDefaultAsync(existing => existing.Id == ParseId(request.Id), cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That role was already taken away."));

        ClaimsPrincipal caller = context.ServerCallContext!.GetHttpContext().User;
        if (!await evaluator.IsAllowedAsync(caller, AccessPermissions.EditAssignmentsNode, assignment.ScopeObjectId, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Not permitted."));

        if (assignment.Role!.SystemKey == OwnerKey
            && await db.Assignments.CountAsync(other => other.RoleId == assignment.RoleId, cancellationToken) <= 1)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "This is the last Owner. Make someone else an Owner first."));

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        await db.AnnounceChangeAsync(cancellationToken);
        return new Done();
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private static Guid CallerId(CallContext context) =>
        Caller.UserId(context.ServerCallContext!.GetHttpContext().User)
        ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't a valid id.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RoleInfo ToInfo(RoleRecord role) => new()
    {
        Id = role.Id.ToString(),
        Name = role.Name,
        Description = role.Description,
        ScopeKind = role.ScopeKind,
        IsSystem = role.IsSystem,
        Patterns = role.Patterns.Select(pattern => pattern.Pattern).Order().ToList(),
        IncludedRoleIds = role.Includes.Select(include => include.IncludedRoleId.ToString()).ToList(),
    };

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
