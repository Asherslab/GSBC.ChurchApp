using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

[Integration("Building a region copies its addresses into the search index by binary COPY, rebuilding replaces them, switching the region off removes them, and regions built before the index are filled in by themselves")]
public sealed class AddressIndexTests : IAsyncLifetime
{
    private MapsHost _host = null!;
    private MapArea _area = null!;

    public async ValueTask InitializeAsync()
    {
        _host = await MapsHost.StartAsync();
        _host.Sources.AddStreet("Heron Street", (3000, 0), (3300, 0));
        _host.Sources.AddHome("Heron Street", new Flat(3100, 15), "7");
        _area = await _host.WithServiceAsync(maps => maps.AddAsync(new AddMapArea
        {
            Name = "Test area", CentreLatitude = Streets.Origin.Lat, CentreLongitude = Streets.Origin.Lon, RadiusKm = 1.5,
        }));
        await _host.BuildAsync(_area.Id);
    }

    public async ValueTask DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task ABuiltRegionsAddressesAreFoundByNumberAndStreet()
    {
        FoundAddress found = Assert.Single(await FindAsync("12 Kingfisher St"));

        Assert.Equal("12 Kingfisher Street, Albany Creek", found.Label);
        Assert.Equal(Assert.Single(await _host.RegionsAsync(_area.Id)).Id, found.RegionId);
    }

    [Fact]
    public async Task ARebuildReplacesTheRegionsAddresses()
    {
        _host.Sources.AddHome("Kingfisher Street", new Flat(200, 15), "21");
        await _host.WithServiceAsync(maps => maps.RebuildAsync(new MapAreaRef { Id = _area.Id }));
        await _host.BuildAsync(_area.Id);

        Assert.Single(await FindAsync("21 Kingfisher"));
        Assert.Equal(3, await _host.WithDbAsync(db => db.Addresses.CountAsync()));
    }

    [Fact]
    public async Task DeletingTheAreaTakesItsAddressesOutOfTheSearch()
    {
        await _host.WithServiceAsync(maps => maps.DeleteAsync(new MapAreaRef { Id = _area.Id }));

        Assert.Empty(await FindAsync("12 Kingfisher St"));
    }

    [Fact]
    public async Task ARegionBuiltBeforeTheIndexIsFilledIn()
    {
        await _host.WithDbAsync(db => db.Addresses.ExecuteDeleteAsync());

        Assert.True(await _host.WithAddressesAsync(index => index.FillMissingAsync(TestContext.Current.CancellationToken)));
        Assert.Single(await FindAsync("15 Kingfisher"));
        Assert.False(await _host.WithAddressesAsync(index => index.FillMissingAsync(TestContext.Current.CancellationToken)));
    }

    private Task<IReadOnlyList<FoundAddress>> FindAsync(string text) =>
        _host.WithAddressesAsync(index => index.FindAsync(text, TestContext.Current.CancellationToken));
}
