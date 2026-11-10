namespace GSBC.ChurchApp.Platform.Permissions;

public readonly record struct ScopeKind(string Name)
{
    public static readonly ScopeKind Global = new("global");
    public static readonly ScopeKind Ministry = new("ministry");
    public static readonly ScopeKind Draft = new("draft");
    public static readonly ScopeKind SermonSeries = new("sermon-series");
    public static readonly ScopeKind Calendar = new("calendar");
    public static readonly ScopeKind RegularEvent = new("regular-event");
    public static readonly ScopeKind Person = new("person");
    public static readonly ScopeKind Chat = new("chat");

    public static readonly IReadOnlyList<ScopeKind> Known = [Global, Ministry, Draft, SermonSeries, Calendar, RegularEvent, Person, Chat];

    public static bool TryParse(string name, out ScopeKind kind)
    {
        kind = Known.FirstOrDefault(known => known.Name == name);
        return kind.Name is not null;
    }

    public override string ToString() => Name;
}
