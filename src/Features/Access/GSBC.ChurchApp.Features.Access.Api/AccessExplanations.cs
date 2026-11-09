using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class AccessExplanations(IScopeHierarchy hierarchy, PermissionCatalog catalog)
{
    private const string Nowhere = "~nowhere";

    public async Task<PersonAbilities> ExplainAsync(
        IReadOnlyCollection<Assignment> assignments, IReadOnlyDictionary<Guid, RoleRecord> roles, CancellationToken cancellationToken)
    {
        GrantSet grants = await CompileAsync(assignments, roles, cancellationToken);

        List<PlaceAbilities> places = [];
        List<Assignment> everywhere = assignments.Where(assignment => assignment.ScopeObjectId is null).ToList();
        List<string> everywhereNodes = catalog.ByNode.Values.Where(permission => IsAllowedEverywhere(grants, permission)).Select(permission => permission.Node).Order().ToList();
        if (everywhere.Count > 0 || everywhereNodes.Count > 0)
            places.Add(Place(ScopeKind.Global.Name, null, everywhere, everywhereNodes, roles));

        foreach (IGrouping<string, Assignment> place in assignments.Where(assignment => assignment.ScopeObjectId is not null).GroupBy(assignment => assignment.ScopeObjectId!))
        {
            string kindName = roles.GetValueOrDefault(place.First().RoleId)?.ScopeKind ?? "";
            List<string> nodes = catalog.ByNode.Values
                .Where(permission => permission.ScopeKind.Name == kindName && grants.IsAllowed(permission, place.Key) && !IsAllowedEverywhere(grants, permission))
                .Select(permission => permission.Node).Order().ToList();
            places.Add(Place(kindName, place.Key, [.. place], nodes, roles));
        }

        return new PersonAbilities { Places = places };
    }

    private static bool IsAllowedEverywhere(GrantSet grants, Permission permission) =>
        grants.IsAllowed(permission, permission.ScopeKind == ScopeKind.Global ? null : Nowhere);

    private Task<GrantSet> CompileAsync(IEnumerable<Assignment> assignments, IReadOnlyDictionary<Guid, RoleRecord> roles, CancellationToken cancellationToken) =>
        RoleGrants.CompileAsync(assignments.Select(assignment => (assignment.RoleId, assignment.ScopeObjectId)), roles, hierarchy, cancellationToken);

    private static PlaceAbilities Place(
        string kind, string? objectId, List<Assignment> because, List<string> nodes, IReadOnlyDictionary<Guid, RoleRecord> roles) => new()
    {
        ScopeKind = kind,
        ObjectId = objectId,
        IncludesTeams = because.Any(assignment => roles.GetValueOrDefault(assignment.RoleId)?.AppliesToChildren == true),
        Nodes = nodes,
        Because = because.Select(assignment => new PlaceReason
        {
            RoleName = roles.GetValueOrDefault(assignment.RoleId)?.Name ?? "",
            ExpiresAt = assignment.ExpiresAt,
        }).ToList(),
    };
}
