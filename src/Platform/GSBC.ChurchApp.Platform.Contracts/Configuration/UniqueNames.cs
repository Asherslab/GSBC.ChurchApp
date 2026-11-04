using System.Globalization;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public static class UniqueNames
{
    public static bool Same(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.InvariantCultureIgnoreCase);

    public static string? Taken(string name, IEnumerable<string> names) =>
        names.FirstOrDefault(other => Same(other, name));

    public static string Free(string name, IReadOnlyCollection<string> names)
    {
        string trimmed = name.Trim();
        if (Taken(trimmed, names) is null)
            return trimmed;
        int number = 2;
        while (Taken(string.Create(CultureInfo.InvariantCulture, $"{trimmed} {number}"), names) is not null)
            number++;
        return string.Create(CultureInfo.InvariantCulture, $"{trimmed} {number}");
    }

    public static IReadOnlyList<string> Clashes(Surface surface, IEnumerable<StoredPlacement> placements, IReadOnlyCollection<string> changed)
    {
        if (surface.UniqueBy is not { } setting)
            return [];
        List<string> names = placements.Where(placement => placement.Surface == surface.Key).Select(placement => NameOf(placement, setting)).ToList();
        return placements
            .Where(placement => placement.Surface == surface.Key && changed.Contains(placement.Id))
            .Select(placement => NameOf(placement, setting))
            .Where(name => names.Count(other => Same(other, name)) > 1)
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .ToList();
    }

    private static string NameOf(StoredPlacement placement, TextSetting setting) =>
        placement.Values.GetValueOrDefault(setting.Key, setting.DefaultText).Trim();
}
