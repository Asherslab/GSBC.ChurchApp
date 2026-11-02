using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Client;

internal static class PlacementMapping
{
    public static StoredPlacement ToStored(this Placement placement) =>
        new(placement.Id, placement.Surface, placement.Order, placement.Hidden, placement.Audience, placement.StartsAt, placement.EndsAt,
            placement.Values.GroupBy(value => value.Key).ToDictionary(group => group.Key, group => group.Last().Value), placement.Revision);
}
