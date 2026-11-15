using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class MapReachTests
{
    [Fact]
    public void SquaresSideBySideShareOneEdgeWithNoSeamBetweenThem()
    {
        MapReach reach = Reach(Square(0, 0), Square(0, 1));

        IReadOnlyList<MapPoint> edge = Assert.Single(reach.Edge());
        Assert.DoesNotContain(edge, point => point is { Lon: > 0.5 and < 1.5, Lat: > 0 and < 1 });
        Assert.Equal(edge[0], edge[^1]);
        Assert.Equal(1, reach.ShareOf(new MapFrame(0, 0, 1, 2)));
    }

    [Fact]
    public void AGapInsideTheSquaresIsBeyondTheMapsToo()
    {
        MapReach reach = Reach([.. from row in Enumerable.Range(0, 3) from column in Enumerable.Range(0, 3) where (row, column) != (1, 1) select Square(row, column)]);

        IReadOnlyList<IReadOnlyList<IReadOnlyList<MapPoint>>> beyond = reach.Beyond();

        Assert.Equal(2, beyond.Count);
        Assert.Equal(2, beyond[0].Count);
        Assert.Contains(beyond[1].Single(), point => point == new MapPoint(1, 1));
        Assert.Equal(0, reach.ShareOf(new MapFrame(1.2, 1.2, 1.8, 1.8)));
    }

    private static MapReach Reach(params MapFrame[] squares) => new(squares, [], []);

    private static MapFrame Square(int row, int column) => new(row, column, row + 1, column + 1);
}
