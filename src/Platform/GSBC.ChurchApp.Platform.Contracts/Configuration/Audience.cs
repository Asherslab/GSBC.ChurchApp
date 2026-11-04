using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public sealed record Audience
{
    private const string EveryoneText = "everyone";
    private const string SignedInText = "signed-in";
    private const string PermissionPrefix = "permission:";
    private const string RolePrefix = "role:";

    public static readonly Audience Everyone = new(EveryoneText, null);
    public static readonly Audience SignedIn = new(SignedInText, null);

    private Audience(string text, string? node, IReadOnlyList<Guid>? roleIds = null)
    {
        Text = text;
        Node = node;
        RoleIds = roleIds ?? [];
    }

    public string Text { get; }

    public string? Node { get; }

    public IReadOnlyList<Guid> RoleIds { get; }

    public bool IsForRoles => RoleIds.Count > 0;

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
            : Node is null || viewer.Holds(Node)));

    public bool Equals(Audience? other) => other is not null && Text == other.Text;

    public override int GetHashCode() => Text.GetHashCode(StringComparison.Ordinal);
}

public sealed record Viewer(bool SignedIn, Func<string, bool> Holds)
{
    public static readonly Viewer Anonymous = new(false, _ => false);
}
