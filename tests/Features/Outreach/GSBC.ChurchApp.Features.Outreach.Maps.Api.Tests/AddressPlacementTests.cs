using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class AddressPlacementTests
{
    [Fact]
    public void AnAddressLandsOnItsOwnStreetEvenWhenAnotherStreetIsCloser()
    {
        (AddressPlacement placement, List<StreetSegment> segments) = TwoParallelStreets();

        Placement? placed = placement.Place(new Flat(150, 70), "Flamingo Drive");

        Assert.NotNull(placed);
        Assert.Equal(2, segments[placed.Value.Segment].Way);
    }

    [Fact]
    public void AnAddressLandsOnTheSideOfTheStreetItIsOn()
    {
        (AddressPlacement placement, _) = TwoParallelStreets();

        Placement? north = placement.Place(new Flat(150, 20), "Kingfisher St");
        Placement? south = placement.Place(new Flat(150, -20), "KINGFISHER STREET");

        Assert.Equal(Side.Left, north?.Side);
        Assert.Equal(Side.Right, south?.Side);
        Assert.Equal(0.5, north!.Value.Along, 3);
    }

    [Fact]
    public void AnAddressFarFromItsStreetIsNotPlaced()
    {
        (AddressPlacement placement, _) = TwoParallelStreets();

        Assert.Null(placement.Place(new Flat(150, -AddressPlacement.MaxSetbackMetres - 50), "Kingfisher Street"));
        Assert.Null(placement.Place(new Flat(150, 20), "Pelican Parade"));
    }

    private static (AddressPlacement, List<StreetSegment>) TwoParallelStreets()
    {
        Streets streets = new();
        streets.Way(1, "residential", "Kingfisher Street", (0, 0), (300, 0));
        streets.Way(2, "residential", "Flamingo Drive", (0, 200), (300, 200));
        List<StreetSegment> segments = StreetNetwork.Build(streets.Osm, Streets.Projection).Segments;
        return (new AddressPlacement(segments, segments.Select(segment => StreetNetwork.Shape(segment, streets.Osm, Streets.Projection)).ToArray()), segments);
    }
}
