using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class RegionVersionTests
{
    [Fact]
    public void ARebuiltRegionWhoseStreetsOrMapChangedGetsANewVersion()
    {
        RegionRecord built = Region(version: 3, fingerprint: "streets-a", mapKey: "map-a.pmtiles");

        Assert.Equal(4, RegionVersions.Next(built, "streets-b", "map-a.pmtiles"));
        Assert.Equal(4, RegionVersions.Next(built, "streets-a", "map-b.pmtiles"));
    }

    [Fact]
    public void ARebuildThatChangedNothingKeepsTheVersionSoPhonesDontDownloadAgain() =>
        Assert.Equal(3, RegionVersions.Next(Region(3, "streets-a", "map-a.pmtiles"), "streets-a", "map-a.pmtiles"));

    [Fact]
    public void TheCurrentAndPreviousVersionsFilesAreKeptAndOlderOnesGo()
    {
        RegionRecord region = Region(5, "streets", "map.pmtiles");

        Assert.True(RegionVersions.Kept(File(5), region));
        Assert.True(RegionVersions.Kept(File(4), region));
        Assert.False(RegionVersions.Kept(File(3), region));
        Assert.False(RegionVersions.Kept(File(5), null));
    }

    private static RegionFileRecord File(long version) => new() { Key = "key", RegionId = "church-a1", Version = version };

    private static RegionRecord Region(long version, string fingerprint, string mapKey) => new()
    {
        Id = "church-a1",
        GridId = "church",
        Name = "Albany Creek",
        Version = version,
        StreetsKey = "streets.streets",
        StreetsFingerprint = fingerprint,
        MapKey = mapKey,
    };
}
