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

    private Audience(string text, string? node, Guid? roleId = null)
    {
        Text = text;
        Node = node;
        RoleId = roleId;
    }

    public string Text { get; }

    public string? Node { get; }

    public Guid? RoleId { get; }

    public static Audience Holding(string node) =>
        PermissionNode.IsValid(node) ? new Audience(PermissionPrefix + node, node) : throw new ArgumentException($"'{node}' is not a permission node.", nameof(node));

    public static Audience HoldingRole(Guid roleId) => new(RolePrefix + roleId.ToString("D"), RoleMembership.NodeOf(roleId), roleId);

    public static bool TryParse(string text, [NotNullWhen(true)] out Audience? audience)
    {
        string node = text.StartsWith(PermissionPrefix, StringComparison.Ordinal) ? text[PermissionPrefix.Length..] : "";
        audience = text switch
        {
            EveryoneText or "" => Everyone,
            SignedInText => SignedIn,
            _ when PermissionNode.IsValid(node) => new Audience(text, node),
            _ when text.StartsWith(RolePrefix, StringComparison.Ordinal) && Guid.TryParse(text[RolePrefix.Length..], out Guid roleId) => HoldingRole(roleId),
            _ => null,
        };
        return audience is not null;
    }

    public bool Includes(Viewer viewer) =>
        this == Everyone || (viewer.SignedIn && (Node is null || viewer.Holds(Node)));
}

public sealed record Viewer(bool SignedIn, Func<string, bool> Holds)
{
    public static readonly Viewer Anonymous = new(false, _ => false);
}
