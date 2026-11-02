namespace GSBC.ChurchApp.Platform.Permissions;

public sealed record Permission
{
    public Permission(string node, ScopeKind scopeKind, string description, bool isSensitive = false, string? area = null)
    {
        if (!PermissionNode.IsValid(node))
            throw new ArgumentException($"'{node}' is not a permission node: lowercase segments joined by dots.", nameof(node));

        Node = node;
        ScopeKind = scopeKind;
        Description = description;
        IsSensitive = isSensitive;
        Area = area ?? FeatureArea(node);
    }

    public string Node { get; }
    public ScopeKind ScopeKind { get; }
    public string Description { get; }
    public bool IsSensitive { get; }
    public string Area { get; }

    public override string ToString() => Node;

    private static string FeatureArea(string node)
    {
        string feature = node.Split(PermissionNode.Separator)[0];
        return char.ToUpperInvariant(feature[0]) + feature[1..];
    }
}
