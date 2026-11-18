using GSBC.ChurchApp.Features.Outreach.Admin;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class MapAreaWordsTests
{
    [Fact]
    public void AnAreaBeingAddedCountsOnlyItsNewRegionsInTheDownloadAndNamesTheAreaThatHasTheOthers()
    {
        List<CellEstimate> cells =
        [
            new() { Row = 0, Column = 0, Addresses = 1_000, Bytes = 9_000_000, AlsoIn = ["Around the church"] },
            new() { Row = 0, Column = 1, Addresses = 1_000, Bytes = 2_000_000 },
        ];

        string estimate = MapAreaWords.AddEstimate(cells);

        Assert.Contains("1 is already in Around the church; the other one builds, about 2.0 MB", estimate);
        Assert.False(MapAreaWords.AllShared(cells));
        Assert.True(MapAreaWords.AllShared(cells[..1]));
    }

    [Fact]
    public void SwitchingOffARegionAnotherAreaHasSaysItStaysOnPhones()
    {
        AreaCell shared = new(new CellRef(), CellLook.SwitchingOff, "Nundah", null, new MapBounds(), ["Around the church"]);

        Assert.Equal("1 region stays on phones: it's also in Around the church.", MapAreaWords.ChangeSummary([shared], 0, null));
    }
}
