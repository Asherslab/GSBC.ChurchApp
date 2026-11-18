using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;

internal sealed class AddressIndex(OutreachMapsDbContext db, IMapSources sources) : IAddressSearch
{
    private const int Found = 12;
    private const int Considered = 2000;
    private const int ShortestWord = 2;

    public static IEnumerable<AddressRecord> Rows(IEnumerable<AddressPoint> addresses, IReadOnlyList<(string RegionId, GeoBounds Core)> regions) =>
        addresses.Where(address => address.Id != 0 && address.Street.Length > 0)
            .Select(address => (Address: address, Region: regions.FirstOrDefault(region => region.Core.Contains(address.Point)).RegionId))
            .Where(placed => placed.Region is not null)
            .DistinctBy(placed => placed.Address.Id)
            .Select(placed => new AddressRecord
            {
                Id = placed.Address.Id,
                RegionId = placed.Region,
                Number = AddressSearchText.Normal(placed.Address.Number),
                Unit = AddressSearchText.Normal(placed.Address.Unit),
                Street = placed.Address.Street,
                Locality = AreaStreets.Suburb(placed.Address.Locality),
                Key = AddressSearchText.Key(placed.Address.Street, AreaStreets.Suburb(placed.Address.Locality)),
                Latitude = placed.Address.Point.Lat,
                Longitude = placed.Address.Point.Lon,
            });

    public async Task ReplaceAsync(IReadOnlyCollection<string> regionIds, IEnumerable<AddressRecord> rows, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Addresses.Where(address => regionIds.Contains(address.RegionId)).ExecuteDeleteAsync(cancellationToken);
        NpgsqlConnection connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using (NpgsqlBinaryImporter import = await connection.BeginBinaryImportAsync(
                         $"COPY {OutreachMapsDbContext.SchemaName}.addresses (id, region_id, number, unit, street, locality, key, latitude, longitude) FROM STDIN (FORMAT BINARY)", cancellationToken))
        {
            foreach (AddressRecord row in rows)
            {
                await import.StartRowAsync(cancellationToken);
                await import.WriteAsync(row.Id, NpgsqlDbType.Bigint, cancellationToken);
                await import.WriteAsync(row.RegionId, NpgsqlDbType.Text, cancellationToken);
                await import.WriteAsync(row.Number, NpgsqlDbType.Text, cancellationToken);
                await import.WriteAsync(row.Unit, NpgsqlDbType.Text, cancellationToken);
                await import.WriteAsync(row.Street, NpgsqlDbType.Text, cancellationToken);
                await import.WriteAsync(row.Locality, NpgsqlDbType.Text, cancellationToken);
                await import.WriteAsync(row.Key, NpgsqlDbType.Text, cancellationToken);
                await import.WriteAsync(row.Latitude, NpgsqlDbType.Double, cancellationToken);
                await import.WriteAsync(row.Longitude, NpgsqlDbType.Double, cancellationToken);
            }
            await import.CompleteAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public Task RemoveAsync(IReadOnlyCollection<string> regionIds, CancellationToken cancellationToken) =>
        db.Addresses.Where(address => regionIds.Contains(address.RegionId)).ExecuteDeleteAsync(cancellationToken);

    public async Task<bool> FillMissingAsync(CancellationToken cancellationToken)
    {
        List<RegionRecord> missing = await db.Regions.AsNoTracking()
            .Where(region => !db.Addresses.Any(address => address.RegionId == region.Id))
            .Where(region => db.Squares.Any(square => square.Id == region.Id && !square.Pending))
            .OrderBy(region => region.Id).ToListAsync(cancellationToken);
        if (missing.Count == 0)
            return false;
        foreach (IGrouping<string, RegionRecord> area in missing.GroupBy(region => region.GridId))
        {
            List<(string RegionId, GeoBounds Core)> cores = [.. area.Select(region => (region.Id, new GeoBounds(region.CoreSouth, region.CoreWest, region.CoreNorth, region.CoreEast)))];
            IReadOnlyList<AddressPoint> addresses = await sources.AddressesAsync(RegionGrid.Union(cores.Select(core => core.Core)), cancellationToken);
            await ReplaceAsync([.. cores.Select(core => core.RegionId)], Rows(addresses, cores), cancellationToken);
        }
        return true;
    }

    public async Task<IReadOnlyList<FoundAddress>> FindAsync(string text, CancellationToken cancellationToken)
    {
        AddressQuery query = AddressSearchText.Parse(text);
        if (query.Words is not [{ Length: >= ShortestWord }, ..])
            return [];
        List<AddressRecord> found = await CandidatesAsync(query, AddressSearchText.Patterns(query), cancellationToken);
        if (found.Count == 0)
            found = await CandidatesAsync(query, AddressSearchText.BroadPatterns(query), cancellationToken);
        return [.. found.Where(address => AddressSearchText.Matches(query, address))
            .DistinctBy(address => address.Id)
            .OrderBy(address => AddressSearchText.Rank(query, address))
            .Take(Found)
            .Select(address => new FoundAddress((uint)address.Id, AddressSearchText.Label(address), address.RegionId, address.Latitude, address.Longitude))];
    }

    private async Task<List<AddressRecord>> CandidatesAsync(AddressQuery query, IReadOnlyList<string> patterns, CancellationToken cancellationToken)
    {
        List<AddressRecord> found = [];
        foreach (string pattern in patterns)
        {
            IQueryable<AddressRecord> matching = db.Addresses.AsNoTracking().Where(address => EF.Functions.Like(address.Key, pattern));
            if (query.Number is { } number)
            {
                string range = $"{number}-%";
                matching = matching.Where(address => address.Number == number || EF.Functions.Like(address.Number, range));
            }
            if (query.Unit is { } unit)
                matching = matching.Where(address => address.Unit == unit);
            found.AddRange(await matching.OrderBy(address => address.Key).ThenBy(address => address.Number).Take(Considered).ToListAsync(cancellationToken));
        }
        return found;
    }
}
