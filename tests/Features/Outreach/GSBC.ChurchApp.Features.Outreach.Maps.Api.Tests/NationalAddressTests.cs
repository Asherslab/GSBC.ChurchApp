using GSBC.ChurchApp.Features.Outreach.Maps.Api.National;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

[Integration("Places off site are found in the national address file imported into Postgres: by address, street, suburb or building name, in any state; a dropped pin gets the nearest address; a newer release replaces the older one whole")]
public sealed class NationalAddressTests : IAsyncLifetime
{
    private MapsHost _host = null!;
    private readonly string _folder = Directory.CreateTempSubdirectory("gsbc-gnaf-tests").FullName;

    public async ValueTask InitializeAsync()
    {
        _host = await MapsHost.StartAsync();
        await ImportAsync("November", new GnafFiles()
            .Add(new GnafAddress("GAQLD1", "12", "Kenrick", "STREET", "Albany Creek", -27.3501, 152.9690))
            .Add(new GnafAddress("GAQLD2", "14", "Kenrick", "STREET", "Albany Creek", -27.3503, 152.9692))
            .Add(new GnafAddress("GAQLD3", "14", "Kenrick", "STREET", "Albany Creek", -27.3503, 152.9692, Unit: "2"))
            .Add(new GnafAddress("GAQLD4", "640", "South Pine", "ROAD", "Eatons Hill", -27.3390, 152.9600, Building: "Eatons Hill Shopping Village"))
            .Add(new GnafAddress("GAQLD5", "8", "Faheys", "ROAD", "Albany Creek", -27.3500, 152.9700, Suffix: "E"))
            .Add(new GnafAddress("GAQLD6", "9", "Gone", "STREET", "Albany Creek", -27.3600, 152.9700, Retired: "2020-01-01"))
            .Add(new GnafAddress("GANSW1", "3", "O'Connell", "STREET", "Tweed Heads", -28.1700, 153.5400)));
    }

    public async ValueTask DisposeAsync()
    {
        await _host.DisposeAsync();
        Directory.Delete(_folder, true);
    }

    [Fact]
    public async Task AnAddressIsFoundAsTypedWithItsStateAndPostcode()
    {
        FoundLocation found = Assert.Single(await FindAsync("12 kenrick st albany creek qld 4035"));

        Assert.Equal(FoundLocationKind.Address, found.Kind);
        Assert.Equal("12 Kenrick Street, Albany Creek QLD 4035", found.Address);
        Assert.Equal("Albany Creek", found.Suburb);
        Assert.Equal("north-west", Assert.Single(await FindAsync("640 south pine")).Direction);
    }

    [Fact]
    public async Task AddressesInOtherStatesAndWithApostrophesAreFound() =>
        Assert.Equal("3 O'Connell Street, Tweed Heads NSW 2485", Assert.Single(await FindAsync("3 oconnell st tweed")).Address);

    [Fact]
    public async Task AStreetWithoutANumberIsOneResultAndASuburbIsItsOwn()
    {
        List<FoundLocation> found = await FindAsync("kenrick");

        Assert.Equal("Kenrick Street, Albany Creek QLD 4035", Assert.Single(found).Address);
        Assert.Contains(await FindAsync("albany cr"), place => place is { Kind: FoundLocationKind.Suburb, Address: "Albany Creek QLD" });
        Assert.Equal("8 Faheys Road East, Albany Creek QLD 4035", Assert.Single(await FindAsync("8 faheys rd e")).Address);
    }

    [Fact]
    public async Task ABuildingIsFoundByItsName()
    {
        FoundLocation found = Assert.Single(await FindAsync("eatons hill shop"), place => place.Kind == FoundLocationKind.Named);

        Assert.Equal("Eatons Hill Shopping Village", found.Name);
        Assert.Equal("640 South Pine Road, Eatons Hill QLD 4035", found.Address);
    }

    [Fact]
    public async Task RetiredAddressesAreLeftOut() => Assert.Empty(await FindAsync("9 gone st"));

    [Fact]
    public async Task ADroppedPinGetsTheNearestAddress()
    {
        FoundLocations nearest = await _host.WithScopeAsync(services =>
            services.GetRequiredService<NationalAddresses>().NearestAsync(-27.35028, 152.96918, TestContext.Current.CancellationToken));

        Assert.Equal("14 Kenrick Street, Albany Creek QLD 4035", Assert.Single(nearest.Found).Address);
    }

    [Fact]
    public async Task ANewerReleaseReplacesTheOlderWhole()
    {
        await ImportAsync("February", new GnafFiles().Add(new GnafAddress("GAQLD7", "21", "Kenrick", "STREET", "Albany Creek", -27.3510, 152.9695)));

        Assert.Empty(await FindAsync("12 kenrick"));
        Assert.Single(await FindAsync("21 kenrick"));
        Assert.Equal(1, await _host.WithDbAsync(db => db.NationalReleases.CountAsync()));
        Assert.Equal(1, await _host.WithDbAsync(db => db.NationalAddresses.CountAsync()));
    }

    [Fact]
    public async Task AnUnfinishedImportIsForgottenAndNeverSearched()
    {
        await _host.WithDbAsync(async db =>
        {
            db.NationalReleases.Add(new Data.NationalReleaseRecord { Name = "Broken", Source = "test", StartedAt = _host.Clock.GetUtcNow().UtcDateTime });
            return await db.SaveChangesAsync();
        });

        Assert.Single(await FindAsync("12 kenrick"));
        await ImportAsync("February", new GnafFiles().Add(new GnafAddress("GAQLD7", "21", "Kenrick", "STREET", "Albany Creek", -27.3510, 152.9695)));
        Assert.Equal(["February"], await _host.WithDbAsync(db => db.NationalReleases.Select(release => release.Name).ToListAsync()));
    }

    private Task ImportAsync(string name, GnafFiles files) =>
        _host.WithScopeAsync(async services =>
        {
            await services.GetRequiredService<GnafImport>().ImportAsync(name, "test", files.Write(_folder), TestContext.Current.CancellationToken);
            return true;
        });

    private async Task<List<FoundLocation>> FindAsync(string text) =>
        (await _host.WithScopeAsync(services => services.GetRequiredService<NationalAddresses>().FindAsync(text, TestContext.Current.CancellationToken))).Found;
}
