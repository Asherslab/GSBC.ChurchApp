using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

[Integration("What resolving a walker's report adds to the places outreach skips")]
public sealed class ReportTests : OutreachHost
{
    private static readonly AddressPoint Place = new() { AddressKey = 9, Region = "church-area", Street = "1/2/3", Along = 400, Label = "Sample St", Point = new GeoPoint { Lat = -27.35, Lng = 152.97 } };

    [Fact]
    public async Task NeverDeliveringThereSkipsTheAddressOrTheAreaDrawnRoundIt()
    {
        string single = Guid.NewGuid().ToString(), wide = Guid.NewGuid().ToString();
        await WalkAsync(walking => walking.ReportAsync(Phone, null, new ReportSend { Id = single, Reason = ReportReason.NotAHome, Place = Place, Note = "A shop" }, Cancel));
        await WalkAsync(walking => walking.ReportAsync(Phone, null, new ReportSend { Id = wide, Reason = ReportReason.NotAHome, Place = Place, Note = "A car yard" }, Cancel));

        await ReviewAsync(review => review.ResolveReportAsync(Staff, new ReportResolve { Id = single, Outcome = ReportState.Excluded }, Cancel));
        await ReviewAsync(review => review.ResolveReportAsync(Staff, new ReportResolve
        {
            Id = wide, Outcome = ReportState.Excluded,
            Area = [new GeoPoint { Lat = -27.34, Lng = 152.96 }, new GeoPoint { Lat = -27.34, Lng = 152.98 }, new GeoPoint { Lat = -27.36, Lng = 152.98 }],
        }, Cancel));

        Exclusions exclusions = await ReviewAsync(review => review.ExclusionsAsync("", Cancel));
        Assert.Equal(9u, Assert.Single(exclusions.Addresses).Address.AddressKey);
        Assert.Equal(3, Assert.Single(exclusions.Zones).Area.Count);
    }
}
