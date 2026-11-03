using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Destinations;

public static class DestinationAudiences
{
    public static bool OpenableByAll(Audience place, string? placeScope, Audience viewers) =>
        place == Audience.Everyone
        || (place.Node is null && viewers != Audience.Everyone)
        || (place.Node is { } node && placeScope is null && viewers.Node == node);
}
