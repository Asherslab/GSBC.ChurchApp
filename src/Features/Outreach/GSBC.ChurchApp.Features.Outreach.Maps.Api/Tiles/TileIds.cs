using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;

internal static class TileIds
{
    public static ulong Of(int zoom, uint x, uint y)
    {
        ulong id = ((1UL << (2 * zoom)) - 1) / 3;
        uint size = 1u << zoom;
        for (uint step = size / 2; step > 0; step /= 2)
        {
            uint rx = (x & step) > 0 ? 1u : 0u;
            uint ry = (y & step) > 0 ? 1u : 0u;
            id += (ulong)step * step * ((3 * rx) ^ ry);
            if (ry != 0)
                continue;
            if (rx == 1)
            {
                x = size - 1 - x;
                y = size - 1 - y;
            }
            (x, y) = (y, x);
        }
        return id;
    }

    public static IEnumerable<ulong> Covering(GeoBounds bounds, int maxZoom)
    {
        for (int zoom = 0; zoom <= maxZoom; zoom++)
        {
            (uint minX, uint maxY) = TileOf(bounds.West, bounds.South, zoom);
            (uint maxX, uint minY) = TileOf(bounds.East, bounds.North, zoom);
            for (uint x = minX; x <= maxX; x++)
                for (uint y = minY; y <= maxY; y++)
                    yield return Of(zoom, x, y);
        }
    }

    private static (uint X, uint Y) TileOf(double longitude, double latitude, int zoom)
    {
        double size = 1 << zoom;
        double latitudeRadians = latitude * Math.PI / 180;
        double x = (longitude + 180) / 360 * size;
        double y = (1 - Math.Log(Math.Tan(latitudeRadians) + 1 / Math.Cos(latitudeRadians)) / Math.PI) / 2 * size;
        return (Clamp(x, size), Clamp(y, size));
    }

    private static uint Clamp(double value, double size) => (uint)Math.Clamp(Math.Floor(value), 0, size - 1);
}
