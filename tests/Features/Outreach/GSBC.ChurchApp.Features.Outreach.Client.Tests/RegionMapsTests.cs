using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class RegionMapsTests
{
    [Fact]
    public async Task AMapBeingKeptIsReadFromTheMediaHostUntilTheCopyOnThePhoneIsWhole()
    {
        Phone phone = new();
        OutreachRegion region = (await phone.Store.LoadAsync()).Regions[0];
        await phone.Regions.KeepAsync(region, false);

        MapSource source = phone.Regions.SourceFor(await phone.Regions.OnPhoneAsync(region));

        Assert.Equal(MapSource.FromKeptFile, source.Kind);
        Assert.Equal(region.Map.Url, source.Fallback);
    }
}
