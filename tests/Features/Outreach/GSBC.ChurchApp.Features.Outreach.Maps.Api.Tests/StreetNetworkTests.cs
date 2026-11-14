using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class StreetNetworkTests
{
    [Fact]
    public void AFootpathAlongsideARoadIsLeftOut()
    {
        Streets streets = new();
        streets.Way(1, "residential", "Kingfisher Street", (0, 0), (300, 0));
        streets.Way(2, "footway", "", (0, 8), (300, 8));

        Network network = StreetNetwork.Build(streets.Osm, Streets.Projection);

        Assert.DoesNotContain(network.Segments, segment => segment.Way == 2);
        Assert.Equal(1, network.PathsBesideRoads);
    }

    [Fact]
    public void AFootpathThroughAParkJoiningTwoStreetsIsKept()
    {
        Streets streets = new();
        streets.Way(1, "residential", "Kingfisher Street", (0, 0), (300, 0));
        streets.Way(2, "residential", "Flamingo Drive", (0, 200), (300, 200));
        streets.Way(3, "footway", "", (150, 0), (150, 200));

        Network network = StreetNetwork.Build(streets.Osm, Streets.Projection);

        Assert.Contains(network.Segments, segment => segment.Way == 3 && segment.Kind == RoadKind.Footway);
    }

    [Fact]
    public void AFootpathTaggedAsTheRoadsSidewalkIsLeftOutWhereverItIs()
    {
        Streets streets = new();
        streets.Way(1, "residential", "Kingfisher Street", (0, 0), (300, 0));
        streets.Way(2, new Dictionary<string, string> { ["highway"] = "footway", ["footway"] = "sidewalk" }, (0, 100), (300, 100));

        Network network = StreetNetwork.Build(streets.Osm, Streets.Projection);

        Assert.DoesNotContain(network.Segments, segment => segment.Way == 2);
    }

    [Fact]
    public void StreetsSplitWhereTheyMeetAndRunInTheWaysOwnDirection()
    {
        Streets streets = new();
        streets.Way(1, "residential", "Kingfisher Street", (0, 0), (100, 0), (200, 0));
        streets.Way(2, "residential", "Wren Court", (100, 0), (100, 150));

        Network network = StreetNetwork.Build(streets.Osm, Streets.Projection);

        List<StreetSegment> kingfisher = network.Segments.Where(segment => segment.Way == 1).ToList();
        Assert.Equal(2, kingfisher.Count);
        Assert.Equal(kingfisher[0].Nodes[^1], kingfisher[1].Nodes[0]);
    }
}
