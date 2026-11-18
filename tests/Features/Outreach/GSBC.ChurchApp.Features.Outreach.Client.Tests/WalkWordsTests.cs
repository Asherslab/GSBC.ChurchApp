using GSBC.ChurchApp.Features.Outreach.Client.Phone;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class WalkWordsTests
{
    [Fact]
    public void TwoStretchesOfOneSideOfAStreetAreToldApartByTheirEnd()
    {
        SavedStep southPart = Step("Whimbrel St", (-27.3500, 152.9800), (-27.3480, 152.9800));
        SavedStep sideStreet = Step("Fantail St", (-27.3480, 152.9800), (-27.3480, 152.9780));
        SavedStep northPart = Step("Whimbrel St", (-27.3470, 152.9800), (-27.3450, 152.9800));

        IReadOnlyList<string> sides = OutreachPhoneWords.SidesOf([southPart, sideStreet, northPart]);

        Assert.Equal(["Whimbrel St, west side, south end", "Fantail St, south side", "Whimbrel St, west side, north end"], sides);
    }

    private static SavedStep Step(string street, (double Lat, double Lon) from, (double Lat, double Lon) to) => new()
    {
        Street = street,
        Title = street,
        Delivers = true,
        Line = [[from.Lat, from.Lon], [to.Lat, to.Lon]],
    };
}
