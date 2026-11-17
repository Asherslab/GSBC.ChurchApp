using GSBC.ChurchApp.Platform.Contracts.Locations;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventPlaces
{
    public const string Church = "church";

    public static Location? LocationOf(EventPlace? place) => place switch
    {
        null => null,
        { Location.IsSet: true } => place.Location,
        { PlaceId.Length: > 0 } => Location.AtChurch(),
        _ when place.Name.Length > 0 || place.Address.Length > 0 => new Location { Kind = LocationKind.OffSite, Name = place.Name, Address = place.Address },
        _ => null,
    };

    public static EventPlace? From(Location? location) => location switch
    {
        null or { IsSet: false } => null,
        { Kind: LocationKind.AtChurch } => new EventPlace { PlaceId = Church, Name = location.Note.Length > 0 ? $"Church, {location.Note}" : "Church", Location = location },
        { Kind: LocationKind.OffSite } => new EventPlace { Name = location.Name, Address = location.Address, Location = location },
        _ => new EventPlace { Name = location.Text, Location = location },
    };
}
