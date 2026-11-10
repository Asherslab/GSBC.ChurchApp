using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Api;

internal static class RoleGrants
{
    public const string AnyObject = "~any";

    public static IEnumerable<Grant> At(IEnumerable<string> patterns, ScopeKind kind, string? objectId) =>
        patterns
            .Select(text => PermissionPattern.TryParse(text, out PermissionPattern? pattern) ? pattern : null)
            .OfType<PermissionPattern>()
            .Select(pattern => new Grant(pattern, kind == ScopeKind.Global ? Scope.Global : Scope.Of(kind, objectId ?? AnyObject)));

    public static IEnumerable<Permission> Abilities(
        IEnumerable<string> rolePatterns, ScopeKind kind, string? objectId, PermissionCatalog catalog, IEnumerable<string>? everywhere = null)
    {
        GrantSet role = new([.. At(rolePatterns, kind, objectId), .. At(everywhere ?? [], ScopeKind.Global, null)]);
        string checkedObject = objectId ?? AnyObject;
        return catalog.ByNode.Values
            .Where(permission => role.IsAllowed(permission, permission.ScopeKind == ScopeKind.Global ? null : checkedObject))
            .OrderBy(permission => permission.Node, StringComparer.Ordinal);
    }

    public static List<Permission> NotHeld(
        GrantSet actor, IEnumerable<string> rolePatterns, ScopeKind kind, string? objectId, PermissionCatalog catalog, IEnumerable<string>? everywhere = null)
    {
        string checkedObject = objectId ?? AnyObject;
        return Abilities(rolePatterns, kind, objectId, catalog, everywhere)
            .Where(permission => !actor.IsAllowed(permission, permission.ScopeKind == ScopeKind.Global ? null : checkedObject))
            .ToList();
    }

    public static List<string> PatternsOf(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles) => Of(role, roles, everywhere: false);

    public static List<string> EverywhereOf(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles) => Of(role, roles, everywhere: true);

    private static List<string> Of(RoleRecord role, IReadOnlyDictionary<Guid, RoleRecord> roles, bool everywhere) =>
        role.Includes
            .Select(include => roles.GetValueOrDefault(include.IncludedRoleId))
            .OfType<RoleRecord>()
            .Prepend(role)
            .SelectMany(each => each.Patterns.Where(pattern => pattern.Everywhere == everywhere).Select(pattern => pattern.Pattern))
            .ToList();

    public static async Task<IReadOnlyList<string?>> ScopesAsync(
        RoleRecord role, ScopeKind kind, string? objectId, IScopeHierarchy hierarchy, CancellationToken cancellationToken) =>
        role.AppliesToChildren && kind != ScopeKind.Global && objectId is not null
            ? [objectId, .. await DescendantsAsync(kind, objectId, hierarchy, cancellationToken)]
            : [objectId];

    private static async Task<List<string>> DescendantsAsync(ScopeKind kind, string objectId, IScopeHierarchy hierarchy, CancellationToken cancellationToken)
    {
        List<string> found = [];
        Queue<string> waiting = new([objectId]);
        while (waiting.TryDequeue(out string? next))
            foreach (string child in await hierarchy.ChildrenOfAsync(kind, next, cancellationToken))
                if (child != objectId && !found.Contains(child))
                {
                    found.Add(child);
                    waiting.Enqueue(child);
                }
        return found;
    }

    public static IEnumerable<Grant> MembershipOf(RoleRecord role) =>
        role.Includes.Select(include => include.IncludedRoleId).Prepend(role.Id)
            .Select(roleId => new Grant(PermissionPattern.Parse(RoleMembership.NodeOf(roleId)), Scope.Global));

    public static async Task<GrantSet> CompileAsync(
        IEnumerable<(Guid RoleId, string? ObjectId)> assignments, IReadOnlyDictionary<Guid, RoleRecord> roles, IScopeHierarchy hierarchy,
        CancellationToken cancellationToken, IEnumerable<IScopeAttachments>? attachments = null)
    {
        List<IScopeAttachments> attached = attachments?.ToList() ?? [];
        List<Grant> grants = [];
        foreach ((Guid roleId, string? objectId) in assignments)
        {
            if (!roles.TryGetValue(roleId, out RoleRecord? role) || !ScopeKind.TryParse(role.ScopeKind, out ScopeKind kind))
                continue;
            grants.AddRange(MembershipOf(role));
            List<string> patterns = PatternsOf(role, roles);
            foreach (string? scope in await ScopesAsync(role, kind, objectId, hierarchy, cancellationToken))
            {
                grants.AddRange(At(patterns, kind, scope));
                if (scope is not null)
                    foreach (IScopeAttachments attachment in attached.Where(each => each.AttachedTo == kind))
                    foreach (Scope other in await attachment.AttachedAsync(scope, cancellationToken))
                        grants.AddRange(At(patterns, other.Kind, other.ObjectId));
            }
            grants.AddRange(At(EverywhereOf(role, roles), ScopeKind.Global, null));
        }
        return new GrantSet(grants);
    }

    public static IEnumerable<(Guid RoleId, string? ObjectId)> HeldThrough(
        IEnumerable<(Guid RoleId, string? ObjectId)> assignments, Func<Guid, IEnumerable<(Guid RoleId, string? ObjectId)>> heldBy,
        IReadOnlyList<HeldThroughRule> rules)
    {
        List<(Guid RoleId, string? ObjectId)> mine = assignments.ToList();
        foreach (HeldThroughRule rule in rules)
        foreach ((Guid _, string? personId) in mine.Where(assignment => assignment.RoleId == rule.PersonRoleId))
        {
            if (!Guid.TryParse(personId, out Guid person))
                continue;
            foreach ((Guid _, string? objectId) in heldBy(person).Where(theirs => theirs.RoleId == rule.HeldRoleId))
                yield return (rule.GivesRoleId, objectId);
        }
    }
}

internal sealed record HeldThroughRule(Guid PersonRoleId, Guid HeldRoleId, Guid GivesRoleId);
