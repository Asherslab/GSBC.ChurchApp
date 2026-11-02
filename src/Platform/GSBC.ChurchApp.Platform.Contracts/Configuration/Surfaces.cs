using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public sealed record SurfaceArea(string Key, string Title, string Description, ScopeKind ScopeKind, Permission EditPermission, Permission PublishPermission)
{
    public Permission AudiencePermission(string node) => new(node, ScopeKind, "");
}

public sealed class Surface
{
    public const string FirstRelease = "1.0";

    public Surface(string key, SurfaceArea area, string title, string description, IReadOnlyList<Setting> settings,
        string introducedIn = FirstRelease, bool shownByDefault = true, int defaultOrder = 0, bool repeatable = false)
    {
        if (settings.Select(setting => setting.Key).Distinct().Count() != settings.Count)
            throw new ArgumentException($"Surface '{key}' declares a setting key twice.", nameof(settings));

        Key = key;
        Area = area;
        Title = title;
        Description = description;
        Settings = settings;
        IntroducedIn = introducedIn;
        ShownByDefault = shownByDefault && !repeatable;
        DefaultOrder = defaultOrder;
        Repeatable = repeatable;
    }

    public string Key { get; }
    public SurfaceArea Area { get; }
    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<Setting> Settings { get; }
    public string IntroducedIn { get; }
    public bool ShownByDefault { get; }
    public int DefaultOrder { get; }
    public bool Repeatable { get; }

    public bool NeedsNewerApp => IntroducedIn != FirstRelease;

    public SettingValues Defaults => new(Settings.ToDictionary(setting => setting.Key, setting => setting.DefaultText));

    public IReadOnlyList<string> ProblemsWith(IReadOnlyDictionary<string, string> values)
    {
        Dictionary<string, Setting> byKey = Settings.ToDictionary(setting => setting.Key);
        List<string> problems = values.Keys.Where(key => !byKey.ContainsKey(key)).Select(key => $"'{Title}' has no setting '{key}'.").ToList();
        problems.AddRange(Settings
            .Select(setting => setting.ProblemWith(values.GetValueOrDefault(setting.Key, setting.DefaultText)))
            .OfType<string>());
        return problems;
    }
}

public sealed class SettingValues(IReadOnlyDictionary<string, string> values)
{
    public static readonly SettingValues None = new(new Dictionary<string, string>());

    public IReadOnlyDictionary<string, string> Raw { get; } = values;

    public T Get<T>(Setting<T> setting) =>
        Raw.TryGetValue(setting.Key, out string? text) && setting.TryParse(text, out T value) ? value : setting.Default;
}

public sealed record SurfaceSet(IReadOnlyList<Surface> Surfaces);

public sealed class SurfaceCatalog(IEnumerable<SurfaceSet> sets)
{
    public IReadOnlyDictionary<string, Surface> ByKey { get; } =
        sets.SelectMany(set => set.Surfaces).ToDictionary(surface => surface.Key);

    public IReadOnlyDictionary<string, SurfaceArea> Areas =>
        ByKey.Values.Select(surface => surface.Area).DistinctBy(area => area.Key).ToDictionary(area => area.Key);

    public IReadOnlyList<Surface> In(SurfaceArea area) =>
        ByKey.Values.Where(surface => surface.Area.Key == area.Key).ToList();
}
