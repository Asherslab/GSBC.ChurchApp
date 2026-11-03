using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class HubGridTests
{
    [Fact]
    public void TheOldHubPacksAsItWasDrawnWithTheBulletinBesidePrayerAndGive()
    {
        IReadOnlyList<TilePlace> places = HubGrid.Pack(2, [new(2, 2), new(1, 2), new(1, 1), new(1, 1), new(2, 1)]);

        Assert.Equal(
            [new TilePlace(1, 1, 2, 2), new TilePlace(1, 3, 1, 2), new TilePlace(2, 3, 1, 1), new TilePlace(2, 4, 1, 1), new TilePlace(1, 5, 2, 1)],
            places);
    }

    [Fact]
    public void ALaterSmallTileFillsAGapLeftByAWideOne()
    {
        IReadOnlyList<TilePlace> places = HubGrid.Pack(3, [new(2, 1), new(3, 1), new(1, 1)]);

        Assert.Equal(new TilePlace(3, 1, 1, 1), places[2]);
        Assert.Equal(new TilePlace(1, 2, 3, 1), places[1]);
    }

    [Fact]
    public void ATileWiderThanTheGridTakesTheFullWidthAndOutOfRangeSizesAreHeldToTheGrid()
    {
        IReadOnlyList<TilePlace> places = HubGrid.Pack(2, [new(4, 1), new(0, 9)]);

        Assert.Equal(new TilePlace(1, 1, 2, 1), places[0]);
        Assert.Equal(new TilePlace(1, 2, 1, HubGrid.MostRows), places[1]);
    }

    [Fact]
    public void NoTwoTilesShareACell()
    {
        TileSpan[] tiles = [new(1, 2), new(2, 1), new(1, 1), new(3, 2), new(1, 1), new(2, 2), new(1, 3), new(1, 1)];

        IReadOnlyList<TilePlace> places = HubGrid.Pack(3, tiles);

        List<(int, int)> cells = places
            .SelectMany(place => Enumerable.Range(place.Column, place.Columns).SelectMany(x => Enumerable.Range(place.Row, place.Rows).Select(y => (x, y))))
            .ToList();
        Assert.Equal(cells.Count, cells.Distinct().Count());
        Assert.All(places, place => Assert.InRange(place.Column + place.Columns - 1, 1, 3));
    }
}
