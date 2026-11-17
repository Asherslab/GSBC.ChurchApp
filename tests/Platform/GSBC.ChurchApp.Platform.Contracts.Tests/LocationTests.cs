using GSBC.ChurchApp.Platform.Contracts.Locations;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class LocationTests
{
    private static Location Place(string key, double latitude = -28.17, bool home = false) => new()
    {
        Kind = LocationKind.OffSite, Name = "Camp", Address = "1 Camp Road", Suburb = "Tallebudgera", Note = "Gate code 1234",
        Latitude = latitude, Longitude = 153.4, Home = home, Map = new LocationMap { Key = key, Url = $"https://media.example/{key}" },
    };

    [Fact]
    public void AHomeSeenFromOutsideShowsOnlyItsSuburb()
    {
        Location seen = Place("a.pmtiles", home: true).SeenFromOutside();

        Assert.True(seen.AddressHidden);
        Assert.Equal("Tallebudgera", seen.Suburb);
        Assert.Equal(("", "", ""), (seen.Name, seen.Address, seen.Note));
        Assert.Null(seen.Latitude);
        Assert.Null(seen.Map);
        Location notHome = Place("b.pmtiles");
        Assert.Same(notHome, notHome.SeenFromOutside());
    }

    [Fact]
    public void ThePhoneKeepsAPlacesMapOnlyWhenNoKeptRegionCoversItAndNeverTwice()
    {
        Location covered = Place("covered.pmtiles", -27.35);
        Location far = Place("far.pmtiles");
        Location sameFile = Place("far.pmtiles", -28.171);

        PlaceMapChanges changes = PlaceMapPlan.Plan([covered, far, sameFile], location => location == covered,
            ["places/covered.pmtiles", "places/gone.pmtiles", "outreach/church-c3/map"]);

        Assert.Equal(["places/far.pmtiles"], changes.Keep.Select(file => file.Id));
        Assert.Equal(["places/covered.pmtiles", "places/gone.pmtiles"], changes.Remove.Order());
    }

    [Fact]
    public void AHiddenHomesMapIsNeverKept() =>
        Assert.Empty(PlaceMapPlan.Plan([Place("home.pmtiles", home: true).SeenFromOutside()], _ => false, []).Keep);
}
