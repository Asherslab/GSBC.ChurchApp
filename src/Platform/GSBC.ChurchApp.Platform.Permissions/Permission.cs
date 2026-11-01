namespace GSBC.ChurchApp.Platform.Permissions;

public sealed record Permission
{
    public Permission(string node, ScopeKind scopeKind, string description)
    {
        if (!PermissionNode.IsValid(node))
            throw new ArgumentException($"'{node}' is not a permission node: lowercase segments joined by dots.", nameof(node));

        Node = node;
        ScopeKind = scopeKind;
        Description = description;
    }

    public string Node { get; }
    public ScopeKind ScopeKind { get; }
    public string Description { get; }

    public override string ToString() => Node;
}
