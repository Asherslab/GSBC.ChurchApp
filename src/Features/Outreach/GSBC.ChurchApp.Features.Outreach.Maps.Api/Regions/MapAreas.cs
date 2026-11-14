using System.Text;
using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Storage;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal static class MapAreas
{
    public const double LargestRadiusKm = 30;
    public const double SmallestRadiusKm = 1;

    public static MapAreaRecord New(string id, string name, GeoBounds bounds, DateTime now) => new()
    {
        Id = id,
        Name = name,
        South = bounds.South,
        West = bounds.West,
        North = bounds.North,
        East = bounds.East,
        State = MapAreaStates.Waiting,
        CreatedAt = now,
    };

    public static string Slug(string name, IReadOnlySet<string> taken)
    {
        StringBuilder slug = new();
        foreach (char letter in name.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(letter))
                slug.Append(letter);
            else if (slug.Length > 0 && slug[^1] != '-')
                slug.Append('-');
        }
        string stem = slug.ToString().Trim('-') is { Length: > 0 } text ? text : "area";
        string id = stem;
        for (int number = 2; taken.Contains(id); number++)
            id = $"{stem}-{number}";
        return id;
    }

    public static MapArea ToMessage(MapAreaRecord area, IEnumerable<RegionRecord> regions, IBlobStore blobs) => new()
    {
        Id = area.Id,
        Name = area.Name,
        Bounds = new MapBounds { South = area.South, West = area.West, North = area.North, East = area.East },
        State = area.State,
        Stage = area.Stage,
        Problem = area.Problem,
        StartedAt = area.StartedAt,
        BuiltAt = area.BuiltAt,
        Regions = regions.OrderBy(region => region.Id, StringComparer.Ordinal).Select(region => RegionFeed.ToMessage(region, blobs)).ToList(),
        Report = area.Report is null ? null : JsonSerializer.Deserialize<MapAreaReport>(area.Report),
    };
}
