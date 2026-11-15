using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using Microsoft.Extensions.Caching.Memory;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed class RegionEstimates(IMapSources sources, IMemoryCache cache)
{
    public const long MapBytes = 1_100_000;
    public const long BytesPerAddress = 75;
    private const int AtOnce = 6;
    private static readonly TimeSpan Remembered = TimeSpan.FromHours(6);

    public static long Bytes(int addresses) => addresses == 0 ? 0 : MapBytes + addresses * BytesPerAddress;

    public async Task<List<CellEstimate>> EstimateAsync(MapGrid grid, IEnumerable<CellRef> cells, CancellationToken cancellationToken)
    {
        using SemaphoreSlim gate = new(AtOnce);
        return [.. await Task.WhenAll(cells.Select(async cell =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                int addresses = await CountAsync(RegionGrid.Core(grid, cell.Row, cell.Column), cancellationToken);
                return new CellEstimate { Row = cell.Row, Column = cell.Column, Addresses = addresses, Bytes = Bytes(addresses) };
            }
            finally
            {
                gate.Release();
            }
        }))];
    }

    private async Task<int> CountAsync(GeoBounds core, CancellationToken cancellationToken)
    {
        string key = string.Create(CultureInfo.InvariantCulture, $"outreach-maps-count:{core.South:F4},{core.West:F4},{core.North:F4},{core.East:F4}");
        if (cache.TryGetValue(key, out int counted))
            return counted;
        int count = await sources.CountAddressesAsync(core, cancellationToken);
        cache.Set(key, count, Remembered);
        return count;
    }
}
