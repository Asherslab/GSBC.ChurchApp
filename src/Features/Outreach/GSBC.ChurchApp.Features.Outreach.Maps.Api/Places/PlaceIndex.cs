using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Places;

internal sealed class PlaceIndex(OutreachMapsDbContext db, IMapSources sources, TimeProvider time, IOptions<MapSourcesOptions> options)
{
    private static readonly TimeSpan KeptFor = TimeSpan.FromDays(30);
    private const int Found = 8;
    private const int ShortestSearch = 2;

    private static readonly IReadOnlyList<string> Kinds =
    [
        MapPlaceKinds.City, MapPlaceKinds.Town, MapPlaceKinds.Suburb, MapPlaceKinds.Village,
        MapPlaceKinds.Locality, MapPlaceKinds.Hamlet, MapPlaceKinds.Neighbourhood,
    ];

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    private GeoPoint Church => new(options.Value.ChurchLatitude, options.Value.ChurchLongitude);

    public async Task<bool> DueAsync(CancellationToken cancellationToken) =>
        await db.Places.MaxAsync(place => (DateTime?)place.IndexedAt, cancellationToken) is not { } indexed || Now - indexed > KeptFor;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OsmPlace> read = await sources.PlacesAsync(cancellationToken);
        DateTime now = Now;
        List<PlaceRecord> places = read.Where(place => Kinds.Contains(place.Kind) && place.Name.Trim().Length > 0)
            .DistinctBy(place => place.Id)
            .Select(place => new PlaceRecord
            {
                Id = place.Id, Name = place.Name.Trim(), Kind = place.Kind, Latitude = place.Point.Lat, Longitude = place.Point.Lon,
                Population = place.Population, IndexedAt = now,
            }).ToList();
        if (places.Count == 0)
            return;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Places.ExecuteDeleteAsync(cancellationToken);
        db.Places.AddRange(places);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<PlaceList> FindAsync(string text, CancellationToken cancellationToken)
    {
        string search = text.Trim();
        bool indexed = await db.Places.AnyAsync(cancellationToken);
        if (search.Length < ShortestSearch)
            return new PlaceList { Indexed = indexed };
        string escaped = search.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
        List<PlaceRecord> found = await db.Places.AsNoTracking()
            .Where(place => EF.Functions.ILike(place.Name, $"%{escaped}%"))
            .OrderBy(place => EF.Functions.ILike(place.Name, $"{escaped}%") ? 0 : 1)
            .ThenByDescending(place => place.Population)
            .ThenBy(place => place.Name)
            .Take(Found * 4)
            .ToListAsync(cancellationToken);
        return new PlaceList
        {
            Indexed = indexed,
            Places = found.OrderBy(place => PlaceWords.Rank(place, search)).ThenBy(place => place.Name.Length).Take(Found).Select(ToMessage).ToList(),
        };
    }

    public async Task<List<MapPlace>> WithinAsync(GeoBounds bounds, CancellationToken cancellationToken) =>
        (await db.Places.AsNoTracking()
            .Where(place => place.Latitude >= bounds.South && place.Latitude <= bounds.North && place.Longitude >= bounds.West && place.Longitude <= bounds.East
                            && place.Kind != MapPlaceKinds.Neighbourhood && place.Kind != MapPlaceKinds.Hamlet)
            .OrderByDescending(place => place.Population).Take(60).ToListAsync(cancellationToken))
        .Select(ToMessage).ToList();

    private MapPlace ToMessage(PlaceRecord place)
    {
        GeoPoint point = new(place.Latitude, place.Longitude);
        return new MapPlace
        {
            Name = place.Name,
            Kind = place.Kind,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            FromChurchKm = Math.Round(PlaceWords.KmBetween(Church, point)),
            Direction = PlaceWords.Direction(Church, point),
        };
    }
}
