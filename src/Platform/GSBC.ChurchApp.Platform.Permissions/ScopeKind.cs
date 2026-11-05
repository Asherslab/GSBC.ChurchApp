namespace GSBC.ChurchApp.Platform.Permissions;

public readonly record struct ScopeKind(string Name)
{
    public static readonly ScopeKind Global = new("global");
    public static readonly ScopeKind Ministry = new("ministry");
    public static readonly ScopeKind Draft = new("draft");
    public static readonly ScopeKind SermonSeries = new("sermon-series");

    public static readonly IReadOnlyList<ScopeKind> Known = [Global, Ministry, Draft, SermonSeries];

    public static bool TryParse(string name, out ScopeKind kind)
    {
        kind = Known.FirstOrDefault(known => known.Name == name);
        return kind.Name is not null;
    }

    public override string ToString() => Name;
}
