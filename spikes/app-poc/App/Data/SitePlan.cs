namespace Gsbc.AppPoc.Data;

public enum PlaceKind { Building, Room, Facility }

public sealed record Place(string Id, string Name, PlaceKind Kind, double X, double Y, string? Space = null);

public sealed record MapMark(Place Place, string Label, string? Colour = null, string? Badge = null, string? Detail = null, string? Href = null);

public interface IMapLayer
{
    string Id { get; }
    string Name { get; }
    IReadOnlyList<MapMark> Marks();
}

public sealed record MapViewBox(double X, double Y, double Width, double Height)
{
    public override string ToString() => FormattableString.Invariant($"{X} {Y} {Width} {Height}");

    public MapViewBox Around(Place place, double width)
    {
        var height = width * Height / Width;
        var left = Math.Clamp(place.X - width / 2, X, X + Width - width);
        var top = Math.Clamp(place.Y - height / 2, Y, Y + Height - height);
        return new(left, top, width, height);
    }
}

public sealed class SitePlan : IMapLayer
{
    public const string SvgPath = "data/site-map.svg";

    public static readonly MapViewBox ViewBox = new(80, 0, 1440, 1130);

    public static readonly IReadOnlyList<Place> Places =
    [
        new("auditorium", "Church Auditorium", PlaceKind.Building, 647, 631),
        new("library", "Library", PlaceKind.Building, 788, 386),
        new("primary-school", "Primary School", PlaceKind.Building, 653, 404),
        new("prep-centre", "Prep Centre", PlaceKind.Building, 963, 278, "prep-centre"),
        new("sports-court", "Sports Court", PlaceKind.Building, 1040, 739),
        new("toilets", "Toilets", PlaceKind.Facility, 799, 735),
        new("accessible-toilet", "Accessible toilet", PlaceKind.Facility, 712, 476),
        new("north-block", "North block", PlaceKind.Room, 796, 183, "north-block"),
        new("primary-school-north", "Primary School, north room", PlaceKind.Room, 655, 245, "primary-school-north"),
        new("primary-school-south", "Primary School, south room", PlaceKind.Room, 653, 334, "primary-school-south"),
        new("library-east", "Library, east wing", PlaceKind.Room, 845, 376, "library-east"),
        new("youth-building", "Youth building", PlaceKind.Room, 1078, 443, "youth-building"),
        new("south-block-upper", "South block, upper room", PlaceKind.Room, 824, 778, "south-block-upper"),
        new("south-block-lower", "South block, lower room", PlaceKind.Room, 822, 843, "south-block-lower"),
    ];

    public string Id => "places";

    public string Name => "Key places";

    public static Place? Find(string id) => Places.FirstOrDefault(p => p.Id == id);

    public static Place? ForSpace(string space) => Places.FirstOrDefault(p => p.Space == space);

    public IReadOnlyList<MapMark> Marks() =>
        Places.Where(p => p.Kind != PlaceKind.Room).Select(p => new MapMark(p, p.Name)).ToList();
}
