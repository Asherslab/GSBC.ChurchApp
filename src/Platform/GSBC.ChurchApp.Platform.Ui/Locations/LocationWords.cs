using System.Globalization;
using GSBC.ChurchApp.Platform.Contracts.Locations;

namespace GSBC.ChurchApp.Platform.Ui.Locations;

public static class LocationWords
{
    public const string AtChurch = "At church";
    public const string SomewhereElse = "Somewhere else";

    public static string Headline(Location location) => location switch
    {
        { Kind: LocationKind.AtChurch } => AtChurch,
        { Kind: LocationKind.OffSite, AddressHidden: true } => location.Suburb.Length > 0 ? $"A home in {location.Suburb}" : "A home",
        { Kind: LocationKind.OffSite } when location.Name.Length > 0 => location.Name,
        { Kind: LocationKind.OffSite } when location.Address.Length > 0 => location.Address,
        { Kind: LocationKind.OffSite } => "A place on the map",
        _ => location.Text,
    };

    public static string? Address(Location location) =>
        location is { Kind: LocationKind.OffSite, AddressHidden: false } && location.Name.Length > 0 && location.Address.Length > 0 ? location.Address : null;

    public static string? Note(Location location) => location.AddressHidden || location.Note.Length == 0 ? null : location.Note;

    public const string HiddenHome = "Only its own people see the street address. Ask a leader if you need it.";

    public static string Found(FoundLocation found) => found.Kind switch
    {
        FoundLocationKind.Suburb => $"A suburb · {FromChurch(found)}",
        _ => found.Suburb.Length > 0 ? $"{found.Suburb} · {FromChurch(found)}" : FromChurch(found),
    };

    public static string FoundTitle(FoundLocation found) => found.Kind == FoundLocationKind.Named ? found.Name : found.Address;

    public static string FromChurch(FoundLocation found) => found.FromChurchKm < 0.5
        ? "near the church"
        : string.Join(' ', new[] { $"{Math.Max(1, Math.Round(found.FromChurchKm)).ToString(CultureInfo.InvariantCulture)} km", found.Direction, "of church" }.Where(part => part.Length > 0));

    public static string AppleMaps(Location location) => location.HasPin
        ? $"https://maps.apple.com/?ll={Point(location)}&q={Uri.EscapeDataString(Headline(location))}"
        : $"https://maps.apple.com/?q={Uri.EscapeDataString(Words(location))}";

    public static string GoogleMaps(Location location) =>
        $"https://www.google.com/maps/search/?api=1&query={(location.HasPin ? Point(location) : Uri.EscapeDataString(Words(location)))}";

    public static string PhoneMaps(Location location) => location.HasPin
        ? $"geo:{Point(location)}?q={Point(location)}({Uri.EscapeDataString(Headline(location))})"
        : $"geo:0,0?q={Uri.EscapeDataString(Words(location))}";

    public static string AppleDirections(Location location) =>
        $"https://maps.apple.com/?daddr={(location.HasPin ? Point(location) : Uri.EscapeDataString(Words(location)))}";

    public static string GoogleDirections(Location location) =>
        $"https://www.google.com/maps/dir/?api=1&destination={(location.HasPin ? Point(location) : Uri.EscapeDataString(Words(location)))}";

    public static bool Opens(Location location) =>
        location is { Kind: LocationKind.OffSite, AddressHidden: false } && (location.HasPin || location.Address.Length > 0);

    private static string Words(Location location) => string.Join(", ", new[] { location.Name, location.Address }.Where(part => part.Length > 0));

    private static string Point(Location location) =>
        $"{location.Latitude!.Value.ToString("0.######", CultureInfo.InvariantCulture)},{location.Longitude!.Value.ToString("0.######", CultureInfo.InvariantCulture)}";
}
