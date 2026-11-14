using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;

internal sealed record Parcel(IReadOnlyList<GeoPoint[]> Rings, IReadOnlyDictionary<string, string> Attributes)
{
    public string Attribute(string name) => Attributes.GetValueOrDefault(name, "");
}

internal sealed class PolygonIndex
{
    private const double CellDegrees = 0.002;
    private readonly Dictionary<(int, int), List<int>> _cells = [];
    private readonly List<(GeoBounds Box, Parcel Parcel)> _items = [];

    public PolygonIndex(IEnumerable<Parcel> parcels)
    {
        foreach (Parcel parcel in parcels.Where(parcel => parcel.Rings.Count > 0))
        {
            GeoBounds box = GeoBounds.Around(parcel.Rings.SelectMany(ring => ring));
            int index = _items.Count;
            _items.Add((box, parcel));
            for (int x = Cell(box.West); x <= Cell(box.East); x++)
                for (int y = Cell(box.South); y <= Cell(box.North); y++)
                {
                    if (!_cells.TryGetValue((x, y), out List<int>? items))
                        _cells[(x, y)] = items = [];
                    items.Add(index);
                }
        }
    }

    public Parcel? Find(GeoPoint point)
    {
        if (!_cells.TryGetValue((Cell(point.Lon), Cell(point.Lat)), out List<int>? items))
            return null;
        foreach (int index in items)
            if (_items[index].Box.Contains(point) && Inside(_items[index].Parcel.Rings, point))
                return _items[index].Parcel;
        return null;
    }

    private static bool Inside(IReadOnlyList<GeoPoint[]> rings, GeoPoint point)
    {
        bool inside = false;
        foreach (GeoPoint[] ring in rings)
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
                if (ring[i].Lat > point.Lat != ring[j].Lat > point.Lat
                    && point.Lon < (ring[j].Lon - ring[i].Lon) * (point.Lat - ring[i].Lat) / (ring[j].Lat - ring[i].Lat) + ring[i].Lon)
                    inside = !inside;
        return inside;
    }

    private static int Cell(double degrees) => (int)Math.Floor(degrees / CellDegrees);
}
