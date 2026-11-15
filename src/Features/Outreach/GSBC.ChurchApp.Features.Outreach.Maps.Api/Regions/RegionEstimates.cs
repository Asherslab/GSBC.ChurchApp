using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using Microsoft.Extensions.Caching.Memory;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal sealed class RegionEstimates(IMapSources sources, IMemoryCache cache)
{
    public const long MapBytes = 1_730_000;
    public const double BytesPerAddress = 44.5;
    private const int AtOnce = 6;
    private static readonly TimeSpan Remembered = TimeSpan.FromHours(6);

    public static long Bytes(int addressesInCore, int addressesInBounds) =>
        addressesInCore == 0 ? 0 : MapBytes + (long)(addressesInBounds * BytesPerAddress);

    public async Task<List<CellEstimate>> EstimateAsync(MapGrid grid, IEnumerable<CellRef> cells, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = new(AtOnce);
        return [.. await Task.WhenAll(cells.Select(async cell =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                RegionCell region = RegionGrid.CellOf("", grid, cell.Row, cell.Column);
                if (!Queensland.Contains(region.Core.Centre))
                    return new CellEstimate { Row = cell.Row, Column = cell.Column, OutsideQueensland = true };
                int inCore = await CountAsync(region.Core, cancellationToken);
                int inBounds = inCore == 0 ? 0 : await CountAsync(region.Bounds, cancellationToken);
                return new CellEstimate { Row = cell.Row, Column = cell.Column, Addresses = inCore, Bytes = Bytes(inCore, inBounds) };
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
