using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;

internal readonly record struct Placement(int Segment, Side Side, double Along, double Distance);

internal sealed class AddressPlacement
{
    public const double MaxSetbackMetres = 200;

    private readonly IReadOnlyList<Flat[]> _shapes;
    private readonly double[] _lengths;
    private readonly Dictionary<string, List<int>> _byStreet = [];

    public AddressPlacement(IReadOnlyList<StreetSegment> segments, IReadOnlyList<Flat[]> shapes)
    {
        _shapes = shapes;
        _lengths = shapes.Select(Length).ToArray();
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].Kind.IsPath() || segments[i].Name.Length == 0)
                continue;
            string key = StreetNames.Key(segments[i].Name);
            if (!_byStreet.TryGetValue(key, out List<int>? list))
                _byStreet[key] = list = [];
            list.Add(i);
        }
    }

    public Placement? Place(Flat point, string street)
    {
        if (!_byStreet.TryGetValue(StreetNames.Key(street), out List<int>? candidates))
            return null;
        Placement? best = null;
        foreach (int segment in candidates)
        {
            Flat[] shape = _shapes[segment];
            double walked = 0;
            for (int i = 1; i < shape.Length; i++)
            {
                Flat direction = shape[i] - shape[i - 1];
                double length = direction.Length;
                double t = length == 0 ? 0 : Math.Clamp(Flat.Dot(point - shape[i - 1], direction) / (length * length), 0, 1);
                double distance = Flat.Distance(point, shape[i - 1] + direction * t);
                if (best is null || distance < best.Value.Distance)
                {
                    Side side = Flat.Cross(direction, point - shape[i - 1]) > 0 ? Side.Left : Side.Right;
                    double along = _lengths[segment] == 0 ? 0 : (walked + t * length) / _lengths[segment];
                    best = new Placement(segment, side, along, distance);
                }
                walked += length;
            }
        }
        return best is { Distance: <= MaxSetbackMetres } ? best : null;
    }

    private static double Length(Flat[] shape)
    {
        double length = 0;
        for (int i = 1; i < shape.Length; i++)
            length += Flat.Distance(shape[i - 1], shape[i]);
        return length;
    }
}
