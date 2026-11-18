using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public enum PlaceKind
{
    Zone,
    Address,
    NoJunkMail,
    Hostile,
}

public sealed record PlaceLine(string Key, PlaceKind Kind, string Title, string Why, DateTime? Since, object Item)
{
    public static IEnumerable<PlaceLine> Of(Exclusions exclusions, SkipLines skips) =>
        exclusions.Zones.Select(zone => new PlaceLine($"zone/{zone.Id}", PlaceKind.Zone, zone.Name.Length > 0 ? zone.Name : "An area", OutreachWords.Reason(zone.Reason), null, zone))
            .Concat(exclusions.Addresses.Select(address =>
                new PlaceLine($"address/{address.Id}", PlaceKind.Address, OutreachWords.Place(address.Address), OutreachWords.Reason(address.Reason), null, address)))
            .Concat(skips.Lines.Select(skip => new PlaceLine($"skip/{skip.Id}", skip.Hostile ? PlaceKind.Hostile : PlaceKind.NoJunkMail, OutreachWords.Place(skip.Address),
                skip.Hostile ? "Hostile" : "No junk mail", skip.At, skip)));

    public string Caption => Kind switch
    {
        PlaceKind.Zone => $"An area · {Why}",
        PlaceKind.Address => $"An address · {Why}",
        PlaceKind.NoJunkMail => "No junk mail",
        _ => "Hostile: wants nothing to do with outreach",
    };
}
