using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public sealed record Audience
{
    private const string EveryoneText = "everyone";
    private const string SignedInText = "signed-in";
    private const string PermissionPrefix = "permission:";
    private const string RolePrefix = "role:";
    private const char ScopeMark = '@';

    public static readonly Audience Everyone = new(EveryoneText, null);
    public static readonly Audience SignedIn = new(SignedInText, null);

    private Audience(string text, string? node, IReadOnlyList<Guid>? roleIds = null, Scope? at = null)
    {
        Text = text;
        Node = node;
        RoleIds = roleIds ?? [];
        At = at;
    }

    public string Text { get; }

    public string? Node { get; }

    public IReadOnlyList<Guid> RoleIds { get; }

    public Scope? At { get; }

    public bool IsForRoles => RoleIds.Count > 0;

    public bool IsForSomeone => IsForRoles || At is not null;

    public static Audience HoldingAt(string node, Scope scope) =>
        PermissionNode.IsValid(node) && !scope.IsGlobal
            ? new Audience($"{PermissionPrefix}{node}{ScopeMark}{scope}", node, at: scope)
            : throw new ArgumentException($"'{node}' at '{scope}' is not a permission at one thing.", nameof(node));

    public static Audience Holding(string node) =>
        PermissionNode.IsValid(node) ? new Audience(PermissionPrefix + node, node) : throw new ArgumentException($"'{node}' is not a permission node.", nameof(node));

    public static Audience HoldingRole(Guid roleId) => HoldingAnyRole([roleId]);

    public static Audience HoldingAnyRole(IEnumerable<Guid> roleIds)
    {
        List<Guid> roles = roleIds.Distinct().ToList();
        return roles.Count == 0
            ? SignedIn
            : new Audience(RolePrefix + string.Join(',', roles.Select(role => role.ToString("D"))), null, roles);
    }

    public static bool TryParse(string text, [NotNullWhen(true)] out Audience? audience)
    {
        string node = text.StartsWith(PermissionPrefix, StringComparison.Ordinal) ? text[PermissionPrefix.Length..] : "";
        if (ScopedIn(node) is { } scoped)
        {
            audience = HoldingAt(scoped.Node, scoped.Scope);
            return true;
        }
        audience = text switch
        {
            EveryoneText or "" => Everyone,
            SignedInText => SignedIn,
            _ when PermissionNode.IsValid(node) => new Audience(text, node),
            _ when text.StartsWith(RolePrefix, StringComparison.Ordinal) && RolesIn(text[RolePrefix.Length..]) is { Count: > 0 } roles => HoldingAnyRole(roles),
            _ => null,
        };
        return audience is not null;
    }

    private static (string Node, Scope Scope)? ScopedIn(string text)
    {
        int mark = text.IndexOf(ScopeMark, StringComparison.Ordinal);
        if (mark <= 0)
            return null;
        string node = text[..mark];
        string[] scope = text[(mark + 1)..].Split(':', 2);
        return PermissionNode.IsValid(node) && scope.Length == 2 && scope[1].Length > 0
               && ScopeKind.TryParse(scope[0], out ScopeKind kind) && kind != ScopeKind.Global
            ? (node, Scope.Of(kind, scope[1]))
            : null;
    }

    private static List<Guid>? RolesIn(string list)
    {
        List<Guid> roles = [];
        foreach (string part in list.Split(','))
        {
            if (!Guid.TryParse(part, out Guid role))
                return null;
            roles.Add(role);
        }
        return roles;
    }

    public bool Includes(Viewer viewer) =>
        this == Everyone || (viewer.SignedIn && (IsForRoles
            ? RoleIds.Any(role => viewer.Holds(RoleMembership.NodeOf(role)))
            : Node is null || (At is { } scope ? viewer.HoldsAt?.Invoke(Node, scope) == true : viewer.Holds(Node))));

    public bool Equals(Audience? other) => other is not null && Text == other.Text;

    public override int GetHashCode() => Text.GetHashCode(StringComparison.Ordinal);
}

public sealed record Viewer(bool SignedIn, Func<string, bool> Holds, Func<string, Scope, bool>? HoldsAt = null)
{
    public static readonly Viewer Anonymous = new(false, _ => false);

    public static Viewer Of(GrantSet grants, bool signedIn = true) => new(
        signedIn,
        node => PermissionNode.IsValid(node) && grants.IsAllowed(new Permission(node, ScopeKind.Global, node)),
        (node, scope) => PermissionNode.IsValid(node) && grants.IsAllowed(new Permission(node, scope.Kind, node), scope.ObjectId));
}
