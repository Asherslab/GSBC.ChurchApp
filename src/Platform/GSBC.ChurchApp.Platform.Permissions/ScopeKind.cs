namespace GSBC.ChurchApp.Platform.Permissions;

public readonly record struct ScopeKind(string Name)
{
    public static readonly ScopeKind Global = new("global");
    public static readonly ScopeKind Ministry = new("ministry");

    public override string ToString() => Name;
}
