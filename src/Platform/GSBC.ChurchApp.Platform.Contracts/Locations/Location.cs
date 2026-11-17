using System.Text.Json.Serialization;
using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Locations;

public enum LocationKind
{
    None = 0,
    AtChurch = 1,
    OffSite = 2,
}

[ProtoContract]
public sealed class Location
{
    [ProtoMember(1)] public LocationKind Kind { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Address { get; set; } = "";
    [ProtoMember(4)] public string Suburb { get; set; } = "";
    [ProtoMember(5)] public string Note { get; set; } = "";
    [ProtoMember(6)] public double? Latitude { get; set; }
    [ProtoMember(7)] public double? Longitude { get; set; }
    [ProtoMember(8)] public bool Home { get; set; }
    [ProtoMember(9)] public LocationMap? Map { get; set; }
    [ProtoMember(10)] public string Text { get; set; } = "";
    [ProtoMember(11)] public bool AddressHidden { get; set; }

    [JsonIgnore] public bool IsSet => Kind != LocationKind.None || Text.Length > 0;

    [JsonIgnore] public bool HasPin => Kind == LocationKind.OffSite && Latitude is not null && Longitude is not null;

    public static Location AtChurch(string note = "") => new() { Kind = LocationKind.AtChurch, Note = note };

    public static Location Written(string text) => new() { Text = text.Trim() };

    public Location SeenFromOutside() => Kind == LocationKind.OffSite && Home
        ? new Location { Kind = LocationKind.OffSite, Home = true, Suburb = Suburb, AddressHidden = true }
        : this;

    public Location Copy() => new()
    {
        Kind = Kind, Name = Name, Address = Address, Suburb = Suburb, Note = Note, Latitude = Latitude, Longitude = Longitude,
        Home = Home, Map = Map?.Copy(), Text = Text, AddressHidden = AddressHidden,
    };

    public bool SameAs(Location? other) => other is not null && Kind == other.Kind && Name == other.Name && Address == other.Address
        && Suburb == other.Suburb && Note == other.Note && Nullable.Equals(Latitude, other.Latitude) && Nullable.Equals(Longitude, other.Longitude) && Home == other.Home
        && Map?.Url == other.Map?.Url && Text == other.Text && AddressHidden == other.AddressHidden;
}

[ProtoContract]
public sealed class LocationMap
{
    [ProtoMember(1)] public string Url { get; set; } = "";
    [ProtoMember(2)] public double South { get; set; }
    [ProtoMember(3)] public double West { get; set; }
    [ProtoMember(4)] public double North { get; set; }
    [ProtoMember(5)] public double East { get; set; }
    [ProtoMember(6)] public long Bytes { get; set; }
    [ProtoMember(7)] public int MaxZoom { get; set; }
    [ProtoMember(8)] public string Attribution { get; set; } = "";
    [ProtoMember(9)] public string Key { get; set; } = "";
    [ProtoMember(10)] public int MinZoom { get; set; }

    public bool Contains(double latitude, double longitude) => latitude >= South && latitude <= North && longitude >= West && longitude <= East;

    public LocationMap Copy() => new()
    {
        Url = Url, South = South, West = West, North = North, East = East, Bytes = Bytes, MaxZoom = MaxZoom, Attribution = Attribution, Key = Key, MinZoom = MinZoom,
    };
}
