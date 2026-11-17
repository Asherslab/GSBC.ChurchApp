using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Places;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.National;

internal sealed class NationalAddresses(OutreachMapsDbContext db, IOptions<MapSourcesOptions> options)
{
    private const int Found = 8;
    private const int Considered = 400;
    private const int ShortestWord = 2;
    private const int ForgottenAtOnce = 50_000;
    private static readonly double[] NearbySteps = [0.002, 0.01, 0.05];

    private GeoPoint Church => new(options.Value.ChurchLatitude, options.Value.ChurchLongitude);

    public Task<NationalReleaseRecord?> CurrentAsync(CancellationToken cancellationToken) =>
        db.NationalReleases.AsNoTracking().Where(release => release.FinishedAt != null).OrderByDescending(release => release.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<FoundLocations> FindAsync(string text, CancellationToken cancellationToken)
    {
        if (await CurrentAsync(cancellationToken) is not { } current)
            return new FoundLocations();
        AddressQuery query = AddressSearchText.Parse(text);
        if (query.Words is not [{ Length: >= ShortestWord }, ..])
            return new FoundLocations { Indexed = true };

        List<FoundLocation> found = [];
        if (query.Number is null)
        {
            found.AddRange((await LocalitiesAsync(current.Id, query, cancellationToken)).Select(Suburb));
            found.AddRange((await BuildingsAsync(current.Id, query, cancellationToken)).Select(Named));
        }
        List<NationalAddressRecord> addresses = await AddressesAsync(current.Id, query, cancellationToken);
        found.AddRange(query.Number is null ? Streets(addresses) : addresses.Take(Found).Select(Address));
        return new FoundLocations { Indexed = true, Found = [.. found.DistinctBy(place => (place.Name, place.Address, place.Suburb)).Take(Found)] };
    }

    public async Task<FoundLocations> NearestAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        if (await CurrentAsync(cancellationToken) is not { } current)
            return new FoundLocations();
        GeoPoint point = new(latitude, longitude);
        foreach (double step in NearbySteps)
        {
            List<NationalAddressRecord> near = await db.NationalAddresses.AsNoTracking()
                .Where(address => address.Release == current.Id
                                  && address.Latitude >= latitude - step && address.Latitude <= latitude + step
                                  && address.Longitude >= longitude - step && address.Longitude <= longitude + step)
                .Take(Considered).ToListAsync(cancellationToken);
            if (near.OrderBy(address => PlaceWords.KmBetween(point, new GeoPoint(address.Latitude, address.Longitude))).ThenBy(address => address.Unit.Length)
                    .FirstOrDefault() is { } nearest)
                return new FoundLocations { Indexed = true, Found = [Address(nearest)] };
        }
        return new FoundLocations { Indexed = true };
    }

    public async Task ForgetUnfinishedAsync(CancellationToken cancellationToken)
    {
        List<int> unfinished = await db.NationalReleases.Where(release => release.FinishedAt == null).Select(release => release.Id).ToListAsync(cancellationToken);
        foreach (int release in unfinished)
            await ForgetAsync(release, cancellationToken);
    }

    public async Task ForgetOlderThanAsync(int kept, CancellationToken cancellationToken)
    {
        List<int> older = await db.NationalReleases.Where(release => release.Id < kept).Select(release => release.Id).ToListAsync(cancellationToken);
        foreach (int release in older)
            await ForgetAsync(release, cancellationToken);
    }

    private async Task ForgetAsync(int release, CancellationToken cancellationToken)
    {
        while (await db.Database.ExecuteSqlAsync(
                   $"DELETE FROM outreach_maps.national_addresses WHERE ctid IN (SELECT ctid FROM outreach_maps.national_addresses WHERE release = {release} LIMIT {ForgottenAtOnce})",
                   cancellationToken) > 0)
        {
        }
        await db.NationalLocalities.Where(locality => locality.Release == release).ExecuteDeleteAsync(cancellationToken);
        await db.NationalReleases.Where(record => record.Id == release).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<List<NationalAddressRecord>> AddressesAsync(int release, AddressQuery query, CancellationToken cancellationToken)
    {
        List<NationalAddressRecord> found = await AddressCandidatesAsync(release, query, AddressSearchText.Patterns(query), cancellationToken);
        if (found.Count == 0)
            found = await AddressCandidatesAsync(release, query, AddressSearchText.BroadPatterns(query), cancellationToken);
        return [.. found.Where(address => AddressSearchText.Matches(query, address))
            .DistinctBy(address => address.Id)
            .OrderBy(address => AddressSearchText.Rank(query, address))
            .ThenBy(address => PlaceWords.KmBetween(Church, new GeoPoint(address.Latitude, address.Longitude)))];
    }

    private IEnumerable<FoundLocation> Streets(List<NationalAddressRecord> addresses) =>
        addresses.GroupBy(address => address.Key).Take(Found).Select(street =>
        {
            NationalAddressRecord[] numbered = [.. street.OrderBy(address => address.Latitude)];
            NationalAddressRecord middle = numbered[numbered.Length / 2];
            return Placed(new FoundLocation
            {
                Kind = FoundLocationKind.Address,
                Address = $"{middle.Street}, {middle.Locality} {middle.State} {middle.Postcode}".TrimEnd(),
                Suburb = middle.Locality,
            }, middle.Latitude, middle.Longitude);
        });

    private async Task<List<NationalAddressRecord>> AddressCandidatesAsync(int release, AddressQuery query, IReadOnlyList<string> patterns, CancellationToken cancellationToken)
    {
        List<NationalAddressRecord> found = [];
        foreach (string pattern in patterns)
        {
            IQueryable<NationalAddressRecord> matching = db.NationalAddresses.AsNoTracking()
                .Where(address => address.Release == release && EF.Functions.Like(address.Key, pattern));
            if (query.Number is { } number)
            {
                string range = $"{number}-%";
                matching = matching.Where(address => address.Number == number || EF.Functions.Like(address.Number, range));
            }
            else
                matching = matching.Where(address => address.Unit == "");
            if (query.Unit is { } unit)
                matching = matching.Where(address => address.Unit == unit);
            found.AddRange(await matching.OrderBy(address => address.Key).ThenBy(address => address.Number).Take(Considered).ToListAsync(cancellationToken));
        }
        return found;
    }

    private async Task<List<NationalLocalityRecord>> LocalitiesAsync(int release, AddressQuery query, CancellationToken cancellationToken)
    {
        string pattern = $"{string.Join(' ', query.Words)}%";
        List<NationalLocalityRecord> found = await db.NationalLocalities.AsNoTracking()
            .Where(locality => locality.Release == release && EF.Functions.Like(locality.Key, pattern))
            .Take(Considered).ToListAsync(cancellationToken);
        return [.. found.DistinctBy(locality => (locality.Name, locality.State))
            .OrderBy(locality => locality.Key.Length)
            .ThenBy(locality => PlaceWords.KmBetween(Church, new GeoPoint(locality.Latitude, locality.Longitude)))
            .Take(3)];
    }

    private async Task<List<NationalAddressRecord>> BuildingsAsync(int release, AddressQuery query, CancellationToken cancellationToken)
    {
        string pattern = $"{string.Join(' ', query.Words)}%";
        List<NationalAddressRecord> found = await db.NationalAddresses.AsNoTracking()
            .Where(address => address.Release == release && address.BuildingKey != "" && EF.Functions.Like(address.BuildingKey, pattern))
            .Take(Considered).ToListAsync(cancellationToken);
        return [.. found.DistinctBy(address => (address.Building, address.Street, address.Locality))
            .OrderBy(address => PlaceWords.KmBetween(Church, new GeoPoint(address.Latitude, address.Longitude)))
            .Take(3)];
    }

    private FoundLocation Address(NationalAddressRecord address) => Placed(new FoundLocation
    {
        Kind = FoundLocationKind.Address,
        Address = AddressLine(address),
        Suburb = address.Locality,
    }, address.Latitude, address.Longitude);

    private FoundLocation Named(NationalAddressRecord address) => Placed(new FoundLocation
    {
        Kind = FoundLocationKind.Named,
        Name = address.Building,
        Address = AddressLine(address),
        Suburb = address.Locality,
    }, address.Latitude, address.Longitude);

    private FoundLocation Suburb(NationalLocalityRecord locality) => Placed(new FoundLocation
    {
        Kind = FoundLocationKind.Suburb,
        Address = $"{locality.Name} {locality.State}",
        Suburb = locality.Name,
    }, locality.Latitude, locality.Longitude);

    private FoundLocation Placed(FoundLocation found, double latitude, double longitude)
    {
        GeoPoint point = new(latitude, longitude);
        found.Latitude = latitude;
        found.Longitude = longitude;
        found.FromChurchKm = Math.Round(PlaceWords.KmBetween(Church, point), 1);
        found.Direction = PlaceWords.Direction(Church, point);
        return found;
    }

    public static string AddressLine(NationalAddressRecord address) =>
        $"{AddressSearchText.Label(address)} {address.State} {address.Postcode}".TrimEnd();
}
