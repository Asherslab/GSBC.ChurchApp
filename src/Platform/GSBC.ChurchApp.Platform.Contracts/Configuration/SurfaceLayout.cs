namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public sealed record StoredPlacement(
    string Id, string Surface, int Order, bool Hidden, string Audience, DateTime? StartsAt, DateTime? EndsAt,
    IReadOnlyDictionary<string, string> Values, long Revision);

public sealed record ArrangedSurface(Surface Surface, StoredPlacement? Placement)
{
    public int Order => Placement?.Order ?? Surface.DefaultOrder;

    public bool Hidden => Placement?.Hidden ?? !Surface.ShownByDefault;

    public Audience Audience =>
        Placement is not null && Audience.TryParse(Placement.Audience, out Audience? audience) ? audience : Audience.Everyone;

    public DateTime? StartsAt => Placement?.StartsAt;

    public DateTime? EndsAt => Placement?.EndsAt;

    public SettingValues Values => new(Surface.Settings.ToDictionary(
        setting => setting.Key,
        setting => Placement?.Values.GetValueOrDefault(setting.Key) ?? setting.DefaultText));

    public bool IsShownTo(Viewer viewer, DateTime now) =>
        !Hidden && (StartsAt is null || now >= StartsAt) && (EndsAt is null || now < EndsAt) && Audience.Includes(viewer);
}

public static class SurfaceLayout
{
    public static IReadOnlyList<ArrangedSurface> Arrange(IEnumerable<Surface> known, IEnumerable<StoredPlacement> stored)
    {
        Dictionary<string, Surface> byKey = known.ToDictionary(surface => surface.Key);
        List<ArrangedSurface> arranged = stored
            .Where(placement => byKey.ContainsKey(placement.Surface))
            .Select(placement => new ArrangedSurface(byKey[placement.Surface], placement))
            .ToList();

        HashSet<string> placed = arranged.Select(entry => entry.Surface.Key).ToHashSet();
        arranged.AddRange(byKey.Values
            .Where(surface => !surface.Repeatable && !placed.Contains(surface.Key))
            .Select(surface => new ArrangedSurface(surface, null)));

        return arranged
            .OrderBy(entry => entry.Order)
            .ThenBy(entry => entry.Surface.Key, StringComparer.Ordinal)
            .ThenBy(entry => entry.Placement?.Id, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<ArrangedSurface> ShownTo(
        IEnumerable<Surface> known, IEnumerable<StoredPlacement> stored, Viewer viewer, DateTime now) =>
        Arrange(known, stored).Where(entry => entry.IsShownTo(viewer, now)).ToList();
}
