using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal static class RegionVersions
{
    private const int KeptVersions = 2;

    public static long Next(RegionRecord? current, string streetsFingerprint, string mapKey, long lastPublished = 0) => current switch
    {
        null => lastPublished + 1,
        _ when current.StreetsFingerprint == streetsFingerprint && current.MapKey == mapKey => current.Version,
        _ => current.Version + 1,
    };

    public static bool Kept(RegionFileRecord file, RegionRecord? region) => region is not null && file.Version > region.Version - KeptVersions;
}
