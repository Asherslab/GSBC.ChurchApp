namespace GSBC.ChurchApp.Platform.Permissions;

public readonly record struct Scope(ScopeKind Kind, string? ObjectId)
{
    public static readonly Scope Global = new(ScopeKind.Global, null);

    public static Scope Of(ScopeKind kind, string objectId) =>
        kind == ScopeKind.Global
            ? throw new ArgumentException("The global scope has no object.", nameof(kind))
            : new Scope(kind, objectId);

    public bool IsGlobal => Kind == ScopeKind.Global;

    public override string ToString() => IsGlobal ? Kind.Name : $"{Kind.Name}:{ObjectId}";
}
