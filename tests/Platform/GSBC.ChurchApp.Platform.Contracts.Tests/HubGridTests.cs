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

    [Fact]
    public void DroppingASmallTileOnAnotherSwapsThemAndNothingElseMoves()
    {
        TileSpan[] home = [new(2, 2), new(1, 1), new(1, 2), new(1, 1), new(2, 1), new(2, 1), new(1, 1), new(1, 1)];
        const int give = 3;
        const int prayer = 1;
        IReadOnlyList<TilePlace> before = HubGrid.Pack(2, home);

        IReadOnlyList<int> order = HubGrid.Plan(2, home, give, column: 0, row: 2);

        IReadOnlyList<TilePlace> after = HubGrid.Pack(2, order.Select(index => home[index]).ToList());
        Assert.Equal(new TilePlace(1, 3, 1, 1), after[order.ToList().IndexOf(give)]);
        int[] moved = order.Where((index, at) => index != give && after[at] != before[index]).ToArray();
        Assert.Equal([prayer], moved);
    }

    [Fact]
    public void DraggingAcrossTheGridChangesTheOrderOncePerCellAndRestingChangesNothing()
    {
        TileSpan[] tiles = [new(2, 1), new(1, 1), new(1, 1), new(1, 1), new(1, 1)];
        List<int> order = [0, 1, 2, 3, 4];
        const int dragged = 4;
        (int Column, int Row)[] path = [(0, 2), (1, 1), (0, 1)];

        Assert.Equal(order, HubGrid.Plan(2, Spans(order), order.IndexOf(dragged), 1, 2).Select(at => order[at]));
        foreach ((int column, int row) in path)
        {
            List<int> next = HubGrid.Plan(2, Spans(order), order.IndexOf(dragged), column, row).Select(at => order[at]).ToList();
            Assert.NotEqual(order, next);
            TilePlace landed = HubGrid.Pack(2, Spans(next))[next.IndexOf(dragged)];
            Assert.Equal((column + 1, row + 1), (landed.Column, landed.Row));
            Assert.Equal(next, HubGrid.Plan(2, Spans(next), next.IndexOf(dragged), column, row).Select(at => next[at]));
            order = next;
        }

        List<TileSpan> Spans(List<int> indices) => indices.Select(index => tiles[index]).ToList();
    }
}
