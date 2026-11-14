using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class LandUseTests
{
    [Theory]
    [InlineData("Shops", "shop")]
    [InlineData("Professional Offices", "office")]
    [InlineData("Educational - Primary School", "school")]
    [InlineData("Vacant Urban Land", "vacant land")]
    public void MoretonBayBusinessesAreSkippedWithWhatTheyAre(string landUse, string label) =>
        Assert.Equal(new LandUseVerdict(Delivery.Skip, label), LandUseRules.MoretonBay(landUse));

    [Theory]
    [InlineData("Single Unit Dwelling")]
    [InlineData("Residential CTS Properties")]
    [InlineData("Retirement Village")]
    public void MoretonBayHomesAreDelivered(string landUse) => Assert.Equal(LandUseVerdict.Home, LandUseRules.MoretonBay(landUse));

    [Fact]
    public void BrisbaneCentresAreSkippedAndMixedUseIsChecked()
    {
        Assert.Equal(Delivery.Skip, LandUseRules.Brisbane("NC", "Neighbourhood centre").Delivery);
        Assert.Equal(new LandUseVerdict(Delivery.Skip, "school"), LandUseRules.Brisbane("CF", "Community facilities - Education purposes"));
        Assert.Equal(LandUseVerdict.CheckFor("mixed use"), LandUseRules.Brisbane("MU", "Mixed use - Centre frame"));
        Assert.Equal(LandUseVerdict.Home, LandUseRules.Brisbane("LDR", "Low density residential"));
    }

    [Fact]
    public void AMoretonBayAddressJoinsItsLotByLotAndPlanBeforeByShape()
    {
        GeoPoint[] square = [new(-27.341, 152.961), new(-27.341, 152.962), new(-27.342, 152.962), new(-27.342, 152.961)];
        LandUseMap map = new(
            [
                new Parcel([], new Dictionary<string, string> { [LandUseMap.MoretonBayLotPlan] = "6RP211431", [LandUseMap.MoretonBayLandUse] = "Shops" }),
                new Parcel([square], new Dictionary<string, string> { [LandUseMap.MoretonBayLotPlan] = "1SP000001", [LandUseMap.MoretonBayLandUse] = "Single Unit Dwelling" }),
            ],
            []);

        LandUseVerdict byLot = map.Classify(Address("6RP211431", new GeoPoint(-27.3415, 152.9615)));
        LandUseVerdict byShape = map.Classify(Address("9SP999999", new GeoPoint(-27.3415, 152.9615)));

        Assert.Equal(Delivery.Skip, byLot.Delivery);
        Assert.Equal(LandUseVerdict.Home, byShape);
    }

    private static AddressPoint Address(string lotPlan, GeoPoint point) => new(1, point, "James Cash Court", false, lotPlan, "Albany Creek", "Moreton Bay City");
}
