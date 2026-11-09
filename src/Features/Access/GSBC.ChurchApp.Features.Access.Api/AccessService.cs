using Grpc.Core;
using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessService(
    AccessDbContext db, AccessChanges changes, IAccessEvaluator evaluator, AssignmentRules rules, AccessInsights insights, PermissionCatalog catalog,
    IPeopleIndex people, TimeProvider time)
    : IAccessService
{
    public const string OwnerKey = "owner";
    public const string RolesManagerKey = "roles-manager";
    private const int ShortestSearch = 2;
    private const int MostFound = 20;

    public async ValueTask<GrantList> GetMineAsync(Nothing request, CallContext context = default) =>
        GrantsFeed.Messages(await evaluator.GrantsForAsync(CallerId(context), context.CancellationToken));

    public ValueTask<Catalog> ListCatalogAsync(Nothing request, CallContext context = default) => ValueTask.FromResult(new Catalog
    {
        Permissions = catalog.ByNode.Values.OrderBy(permission => permission.Area).ThenBy(permission => permission.Node).Select(Info).ToList(),
        ScopeKinds = ScopeKind.Known.Select(kind => kind.Name).ToList(),
    });

    public async ValueTask<RoleNameList> ListRoleNamesAsync(Nothing request, CallContext context = default) => new()
    {
        Roles = await db.Roles.AsNoTracking()
            .Where(role => !role.Hidden)
            .OrderBy(role => role.Name)
            .Select(role => new RoleName { Id = role.Id.ToString(), Name = role.Name, Description = role.Description })
            .ToListAsync(context.CancellationToken),
    };

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
                .OrderBy(role => role.Position)
                .Select(role => ToInfo(role, roles, holders.GetValueOrDefault(role.Id)))
                .ToList(),
        };
    }

    public async ValueTask<AssignmentList> ListAssignmentsAsync(AssignmentQuery request, CallContext context = default)
    {
        IQueryable<Assignment> query = db.Assignments.AsNoTracking().Include(assignment => assignment.Role).Where(assignment => !assignment.Role!.Hidden);
        if (!string.IsNullOrEmpty(request.UserId))
            query = query.Where(assignment => assignment.UserId == ParseId(request.UserId));

        List<Assignment> assignments = await query.OrderBy(assignment => assignment.CreatedAt).ToListAsync(context.CancellationToken);
        IReadOnlyDictionary<Guid, string> names = await NamesAsync(assignments.Select(assignment => assignment.CreatedBy).OfType<Guid>(), context.CancellationToken);
        return new AssignmentList { Assignments = assignments.Select(assignment => ToInfo(assignment, names)).ToList() };
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
        if (role.Hidden)
            throw Invalid($"The app gives the {role.Name} role by itself; it isn't given by hand.");
        if (AssignmentRules.ScopeProblem(kind, scopeObjectId) is { } problem)
            throw Invalid(problem);
        if (await rules.UnknownPlaceAsync(kind, scopeObjectId, cancellationToken) is { } unknown)
            throw Invalid(unknown);
        if (request.ExpiresAt is { } expires && expires <= Now)
            throw Invalid("The end date has to be in the future.");
        if (await rules.RefusalAsync(CallerId(context), role, scopeObjectId, cancellationToken) is { } refusal)
            throw new RpcException(new Status(StatusCode.PermissionDenied, refusal));
        if ((await people.GetAsync([userId], cancellationToken)).SingleOrDefault() is not { Disabled: false })
            throw Invalid("That person's account isn't here or is turned off.");

        if (await db.ActiveAssignments(Now).Include(existing => existing.Role)
                .FirstOrDefaultAsync(existing => existing.UserId == userId && existing.RoleId == role.Id && existing.ScopeObjectId == scopeObjectId, cancellationToken)
            is { } held)
            return ToInfo(held, new Dictionary<Guid, string>());

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
        await changes.AnnounceAsync(cancellationToken);
        assignment.Role = role;
        return ToInfo(assignment, await NamesAsync([CallerId(context)], cancellationToken));
    }

    public async ValueTask<Done> UnassignAsync(AssignmentReference request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Assignment assignment = await db.Assignments.Include(existing => existing.Role).ThenInclude(role => role!.Assigners)
            .SingleOrDefaultAsync(existing => existing.Id == ParseId(request.Id), cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That role was already taken away."));

        if (await rules.RefusalAsync(CallerId(context), assignment.Role!, assignment.ScopeObjectId, cancellationToken) is { } refusal)
            throw new RpcException(new Status(StatusCode.PermissionDenied, refusal));
        if (await rules.LastOwnerAsync(assignment.Role!, [assignment], cancellationToken) is { } lastOwner)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, lastOwner));

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        await changes.AnnounceAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<PlacePeople> ListPlaceAsync(PlaceReference request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        (ScopeKind kind, string? objectId) = await PlaceAsync(request, cancellationToken);
        List<RoleRecord> roles = await RolesAtAsync(kind, cancellationToken);
        Guid callerId = CallerId(context);
        List<PlaceRole> placeRoles = [];
        foreach (RoleRecord role in roles)
            placeRoles.Add(new PlaceRole
            {
                Id = role.Id.ToString(),
                Name = role.Name,
                Description = role.Description,
                AbilityNodes = Abilities(role, roles.ToDictionary(each => each.Id)).Select(permission => permission.Node).ToList(),
                Refusal = await rules.RefusalAsync(callerId, role, objectId, cancellationToken),
            });
        GrantSet grants = await evaluator.GrantsForAsync(callerId, cancellationToken);
        if (!grants.IsAllowed(AccessPermissions.View) && placeRoles.All(role => role.Refusal is not null))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "You can't see who's on this. Ask someone who can add people to it."));

        List<Guid> roleIds = roles.Select(role => role.Id).ToList();
        List<Assignment> held = await db.ActiveAssignments(Now).Include(assignment => assignment.Role)
            .Where(assignment => roleIds.Contains(assignment.RoleId) && assignment.ScopeObjectId == objectId)
            .OrderBy(assignment => assignment.CreatedAt)
            .ToListAsync(cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await NamesAsync(
            held.Select(assignment => assignment.UserId).Concat(held.Select(assignment => assignment.CreatedBy).OfType<Guid>()), cancellationToken);

        return new PlacePeople
        {
            Roles = placeRoles,
            People = held.GroupBy(assignment => assignment.UserId)
                .Select(person => new PlaceHolder
                {
                    UserId = person.Key.ToString(),
                    Name = names.GetValueOrDefault(person.Key, "Someone"),
                    Assignments = person.Select(assignment => ToInfo(assignment, names)).ToList(),
                })
                .OrderBy(person => person.Name, StringComparer.CurrentCulture)
                .ToList(),
            Abilities = placeRoles.SelectMany(role => role.AbilityNodes).Distinct()
                .Select(node => catalog.ByNode.GetValueOrDefault(node)).OfType<Permission>().Select(Info).ToList(),
        };
    }

    public async ValueTask<FoundPeople> FindPeopleAsync(FindPeopleRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        (ScopeKind kind, string? objectId) = await PlaceAsync(request.Place, cancellationToken);
        Guid callerId = CallerId(context);
        bool mayGiveSomething = false;
        foreach (RoleRecord role in await RolesAtAsync(kind, cancellationToken))
            mayGiveSomething = mayGiveSomething || await rules.RefusalAsync(callerId, role, objectId, cancellationToken) is null;
        if (!mayGiveSomething)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "You can't add people to this."));

        string query = request.Query.Trim();
        if (query.Length < ShortestSearch)
            return new FoundPeople();
        return new FoundPeople
        {
            People = (await people.FindAsync(query, MostFound, cancellationToken))
                .Select(person => new FoundPerson { Id = person.Id.ToString(), Name = person.Name, MaskedEmail = MaskedEmail(person.Email) })
                .ToList(),
        };
    }

    public static string MaskedEmail(string email) =>
        email.IndexOf('@') is > 0 and var at ? $"{email[0]}…{email[at..]}" : "";

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private async Task<(ScopeKind Kind, string? ObjectId)> PlaceAsync(PlaceReference place, CancellationToken cancellationToken)
    {
        if (!ScopeKind.TryParse(place.ScopeKind, out ScopeKind kind))
            throw Invalid($"'{place.ScopeKind}' isn't a kind of place a role is given for.");
        string? objectId = string.IsNullOrWhiteSpace(place.ObjectId) ? null : place.ObjectId.Trim();
        if (AssignmentRules.ScopeProblem(kind, objectId) is { } problem)
            throw Invalid(problem);
        if (await rules.UnknownPlaceAsync(kind, objectId, cancellationToken) is { } unknown)
            throw Invalid(unknown);
        return (kind, objectId);
    }

    private Task<List<RoleRecord>> RolesAtAsync(ScopeKind kind, CancellationToken cancellationToken) =>
        db.Roles.AsNoTracking().Include(role => role.Patterns).Include(role => role.Includes).Include(role => role.Assigners)
            .Where(role => role.ScopeKind == kind.Name && !role.Hidden)
            .OrderBy(role => role.Position)
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken) =>
        (await people.GetAsync(ids.Distinct().ToList(), cancellationToken)).ToDictionary(person => person.Id, person => person.Name);

    private async Task<RoleRecord> LoadRoleAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Roles.Include(role => role.Patterns).Include(role => role.Includes).Include(role => role.Assigners)
            .SingleOrDefaultAsync(role => role.Id == id, cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, "That role no longer exists."));

    private static Guid CallerId(CallContext context) =>
        Caller.UserId(context.ServerCallContext!.GetHttpContext().User)
        ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't a valid id.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static PermissionInfo Info(Permission permission) => new()
    {
        Node = permission.Node,
        ScopeKind = permission.ScopeKind.Name,
        Description = permission.Description,
        Sensitive = permission.IsSensitive,
        Area = permission.Area,
    };

    private IEnumerable<Permission> Abilities(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles)
    {
        ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind);
        return RoleGrants.Abilities(RoleGrants.PatternsOf(role, roles), kind, null, catalog, RoleGrants.EverywhereOf(role, roles));
    }

    private RoleInfo ToInfo(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles, int holderCount) => new()
    {
        Id = role.Id.ToString(),
        Name = role.Name,
        Description = role.Description,
        ScopeKind = role.ScopeKind,
        IsSystem = role.IsSystem,
        Patterns = role.Patterns.Select(pattern => pattern.Everywhere ? $"{pattern.Pattern} (everywhere)" : pattern.Pattern).Order().ToList(),
        IncludedRoleIds = role.Includes.Select(include => include.IncludedRoleId.ToString()).ToList(),
        AbilityNodes = Abilities(role, roles).Select(permission => permission.Node).ToList(),
        AppliesToChildren = role.AppliesToChildren,
        AssignableByRoleIds = role.Assigners.Select(assigner => assigner.AssignerRoleId.ToString()).ToList(),
        HolderCount = holderCount,
        Feature = role.Feature,
        Hidden = role.Hidden,
        Position = role.Position,
    };

    private static AssignmentInfo ToInfo(Assignment assignment, IReadOnlyDictionary<Guid, string> names) => new()
    {
        Id = assignment.Id.ToString(),
        UserId = assignment.UserId.ToString(),
        RoleId = assignment.RoleId.ToString(),
        RoleName = assignment.Role?.Name ?? "",
        ScopeObjectId = assignment.ScopeObjectId,
        ExpiresAt = assignment.ExpiresAt,
        Source = assignment.Source.ToString(),
        CreatedAt = assignment.CreatedAt,
        CreatedBy = assignment.CreatedBy is { } by ? names.GetValueOrDefault(by) : null,
    };
}
