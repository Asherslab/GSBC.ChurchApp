using GSBC.ChurchApp.Platform.Contracts.Locations;

namespace GSBC.ChurchApp.Platform.Ui.Locations;

public sealed record PlaceMapFile(string Id, string Url, string Title);

public sealed record PlaceMapChanges(IReadOnlyList<PlaceMapFile> Keep, IReadOnlyList<string> Remove);

public static class PlaceMapPlan
{
    public const string Prefix = "places/";

    public static string FileId(LocationMap map) => $"{Prefix}{map.Key}";

    public static PlaceMapChanges Plan(IEnumerable<Location> locations, Func<Location, bool> coveredByAKeptRegion, IEnumerable<string> keptIds)
    {
        List<PlaceMapFile> wanted = [.. locations
            .Where(location => location is { HasPin: true, AddressHidden: false, Map.Key.Length: > 0 } && !coveredByAKeptRegion(location))
            .Select(location => new PlaceMapFile(FileId(location.Map!), location.Map!.Url, $"{LocationWords.Headline(location)} map"))
            .DistinctBy(file => file.Id)];
        HashSet<string> kept = [.. keptIds.Where(id => id.StartsWith(Prefix, StringComparison.Ordinal))];
        HashSet<string> wantedIds = [.. wanted.Select(file => file.Id)];
        return new PlaceMapChanges([.. wanted.Where(file => !kept.Contains(file.Id))], [.. kept.Where(id => !wantedIds.Contains(id))]);
    }
}
