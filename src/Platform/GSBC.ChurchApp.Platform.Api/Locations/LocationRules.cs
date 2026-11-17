using GSBC.ChurchApp.Platform.Contracts.Locations;

namespace GSBC.ChurchApp.Platform.Api.Locations;

public static class LocationRules
{
    public const int LongestName = 80;
    public const int LongestAddress = 160;
    public const int LongestNote = 160;

    public static Location? Cleaned(Location? asked)
    {
        if (asked is null)
            return null;
        Location cleaned = asked.Kind switch
        {
            LocationKind.AtChurch => Location.AtChurch(Clip(asked.Note, LongestNote)),
            LocationKind.OffSite => new Location
            {
                Kind = LocationKind.OffSite,
                Name = Clip(asked.Name, LongestName),
                Address = Clip(asked.Address, LongestAddress),
                Suburb = Clip(asked.Suburb, LongestName),
                Note = Clip(asked.Note, LongestNote),
                Latitude = OnEarth(asked.Latitude, 90) && OnEarth(asked.Longitude, 180) ? asked.Latitude : null,
                Longitude = OnEarth(asked.Latitude, 90) && OnEarth(asked.Longitude, 180) ? asked.Longitude : null,
                Home = asked.Home,
                Map = asked.Map is { Url.Length: > 0 } map ? map.Copy() : null,
            },
            _ => Location.Written(Clip(asked.Text, LongestAddress)),
        };
        return cleaned.IsSet ? cleaned : null;
    }

    public static string Text(Location? location) => location switch
    {
        null => "",
        { Kind: LocationKind.AtChurch } => location.Note.Length > 0 ? $"Church, {location.Note}" : "Church",
        { Kind: LocationKind.OffSite } => string.Join(", ", new[] { location.Name, location.Address }.Where(part => part.Length > 0)),
        _ => location.Text,
    };

    private static bool OnEarth(double? value, double limit) => value is { } number && double.IsFinite(number) && Math.Abs(number) <= limit;

    private static string Clip(string text, int longest)
    {
        string trimmed = text.Trim();
        return trimmed.Length <= longest ? trimmed : trimmed[..longest].TrimEnd();
    }
}
