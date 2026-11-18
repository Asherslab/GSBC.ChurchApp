using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class AddressSearchTests
{
    private static readonly AddressRecord House = Address("12", "", "Smith Street", "Albany Creek");
    private static readonly AddressRecord Unit = Address("12", "3", "Smith Street", "Albany Creek");
    private static readonly AddressRecord Neighbour = Address("120", "", "Smith Street", "Albany Creek");
    private static readonly AddressRecord Elsewhere = Address("12", "", "Smith Road", "Arana Hills");
    private static readonly AddressRecord Mount = Address("4", "", "Mount Cotton Road", "Capalaba");

    [Theory]
    [InlineData("12 Smith St", true)]
    [InlineData("12 smith street albany", true)]
    [InlineData("12 Smith St, Albany Creek QLD", true)]
    [InlineData("12 smith", true)]
    [InlineData("12 Smith Rd", false)]
    [InlineData("12 Smith St Arana", false)]
    [InlineData("14 Smith St", false)]
    public void AStreetTypeMayBeShortenedAndTheSuburbLeftOff(string typed, bool found) =>
        Assert.Equal(found, AddressSearchText.Matches(AddressSearchText.Parse(typed), House));

    [Theory]
    [InlineData("3/12 Smith St")]
    [InlineData("U3/12 Smith Street")]
    [InlineData("Unit 3 / 12 Smith St")]
    public void AUnitIsWrittenAsUnitSlashNumber(string typed)
    {
        AddressQuery query = AddressSearchText.Parse(typed);

        Assert.True(AddressSearchText.Matches(query, Unit));
        Assert.False(AddressSearchText.Matches(query, House));
    }

    [Fact]
    public void TheHouseComesBeforeItsUnitsAndTheExactNumberFirst()
    {
        AddressQuery query = AddressSearchText.Parse("12 Smith");
        AddressRecord range = Address("12-14", "", "Smith Street", "Albany Creek");

        List<AddressRecord> ranked = [.. new[] { Unit, range, Elsewhere, House, Neighbour }.Where(address => AddressSearchText.Matches(query, address)).OrderBy(address => AddressSearchText.Rank(query, address))];

        Assert.Equal([Elsewhere, House, Unit, range], ranked);
    }

    [Fact]
    public void TheDatabaseIsAskedForTheKeyAsTypedAndAsSpelledOut()
    {
        AddressQuery query = AddressSearchText.Parse("4 Mt Cotton Rd");

        Assert.Contains("mount cotton road%", AddressSearchText.Patterns(query));
        Assert.True(AddressSearchText.Matches(query, Mount));
    }

    [Fact]
    public void ALabelReadsAsAnAddressIsWritten()
    {
        Assert.Equal("3/12 Smith Street, Albany Creek", AddressSearchText.Label(Unit));
        Assert.Equal("12 Smith Street, Albany Creek", AddressSearchText.Label(House));
    }

    private static AddressRecord Address(string number, string unit, string street, string locality) => new()
    {
        Number = number, Unit = unit, Street = street, Locality = locality, Key = AddressSearchText.Key(street, locality), RegionId = "church-c3",
    };
}
