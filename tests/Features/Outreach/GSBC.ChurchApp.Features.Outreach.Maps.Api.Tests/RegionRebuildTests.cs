using Amazon.S3;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

[Integration("A region's files are content-addressed in the bucket and its feed record and version change only when a rebuild changed them; files older than the previous version are deleted")]
public sealed class RegionRebuildTests : IAsyncLifetime
{
    private readonly TestClock _clock = new();
    private readonly FakeSources _sources = new();
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        TestBuckets buckets = await TestBuckets.CreateAsync();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Database.ConnectionStringName}"] = _database.ConnectionString,
            ["Storage:Blobs:ServiceUrl"] = buckets.ServiceUrl,
            ["Storage:Blobs:AccessKeyId"] = buckets.AccessKey,
            ["Storage:Blobs:SecretAccessKey"] = buckets.SecretKey,
            ["Storage:Blobs:PublicBucket"] = buckets.Public,
            ["Storage:Blobs:PrivateBucket"] = buckets.Private,
            ["Storage:Blobs:PublicBaseUrl"] = $"{buckets.ServiceUrl}/{buckets.Public}",
        }).Build();
        _services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging()
            .AddSingleton<TimeProvider>(_clock)
            .AddBlobStore(configuration)
            .AddOutreachMapsData()
            .AddSyncData()
            .AddSingleton<IMapSources>(_sources)
            .AddScoped<AreaBuilder>()
            .BuildServiceProvider();
        await _services.GetRequiredService<IAmazonS3>().PutBucketAsync(buckets.Public);
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>().Database.MigrateAsync();
        OutreachMapsDbContext db = scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>();
        db.Areas.Add(MapAreas.New("test", "Test area", GeoArea.Around(Streets.Origin, 1500), _clock.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task ARebuildKeepsTheVersionUntilTheStreetsChangeAndOldFilesGo()
    {
        RegionRecord first = await BuildAsync();
        Assert.Equal(1, first.Version);
        Assert.NotNull(await FindAsync(first.StreetsKey));
        Assert.NotNull(await FindAsync(first.MapKey));
        OutreachRegion published = await PublishedAsync(first.Id);
        Assert.EndsWith(first.StreetsKey, published.Streets.Url);
        Assert.Equal(2, published.Letterboxes);

        Assert.Equal(1, (await BuildAsync()).Version);
        Assert.Equal(1, await FeedRowsAsync(first.Id));

        _sources.AddHome(new Flat(220, 15));
        RegionRecord second = await BuildAsync();
        _sources.AddHome(new Flat(240, -15));
        RegionRecord third = await BuildAsync();

        Assert.Equal((2, 3), (second.Version, third.Version));
        Assert.Equal(4, (await PublishedAsync(first.Id)).Letterboxes);
        Assert.Null(await FindAsync(first.StreetsKey));
        Assert.NotNull(await FindAsync(second.StreetsKey));
        Assert.NotNull(await FindAsync(third.StreetsKey));
        Assert.Equal(first.MapKey, third.MapKey);
        Assert.NotNull(await FindAsync(third.MapKey));
    }

    private async Task<RegionRecord> BuildAsync()
    {
        _clock.Advance(TimeSpan.FromHours(1));
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        OutreachMapsDbContext db = scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>();
        await scope.ServiceProvider.GetRequiredService<AreaBuilder>().BuildAsync(await db.Areas.SingleAsync(), TestContext.Current.CancellationToken);
        return await db.Regions.AsNoTracking().SingleAsync();
    }

    private async Task<BlobInfo?> FindAsync(string key)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IBlobStore>().FindAsync(BlobBucket.Public, key, TestContext.Current.CancellationToken);
    }

    private async Task<OutreachRegion> PublishedAsync(string regionId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        FeedChangeRecord row = await scope.ServiceProvider.GetRequiredService<SyncDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == OutreachMapsFeeds.Regions && change.RecordId == regionId)
            .OrderByDescending(change => change.Cursor).FirstAsync();
        return OutreachMapsContractsModel.Instance.Deserialize<OutreachRegion>(new MemoryStream(row.Payload));
    }

    private async Task<int> FeedRowsAsync(string regionId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SyncDbContext>().FeedChanges
            .CountAsync(change => change.Feed == OutreachMapsFeeds.Regions && change.RecordId == regionId);
    }

    private sealed class FakeSources : IMapSources
    {
        private readonly List<AddressPoint> _addresses = [];
        private uint _nextId = 1;

        public FakeSources()
        {
            AddHome(new Flat(60, 15));
            AddHome(new Flat(120, -15));
        }

        public void AddHome(Flat at) =>
            _addresses.Add(new AddressPoint(_nextId++, Streets.Projection.ToGeo(at), "Kingfisher Street", false, "", "Albany Creek", "Brisbane City"));

        public Task<OsmData> StreetsAsync(GeoBounds bounds, CancellationToken cancellationToken)
        {
            Streets streets = new();
            streets.Way(1, "residential", "Kingfisher Street", (0, 0), (300, 0));
            return Task.FromResult(streets.Osm);
        }

        public Task<IReadOnlyList<AddressPoint>> AddressesAsync(GeoBounds bounds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AddressPoint>>([.. _addresses]);

        public Task<LandUseMap> LandUseAsync(GeoBounds bounds, CancellationToken cancellationToken)
        {
            GeoPoint[] everywhere = [new(-28, 152), new(-28, 154), new(-27, 154), new(-27, 152)];
            Parcel residential = new([everywhere], new Dictionary<string, string> { [LandUseMap.BrisbaneZoneCode] = "LDR" });
            return Task.FromResult(new LandUseMap([], [residential]));
        }

        public Task<IRangeReader> TilesAsync(CancellationToken cancellationToken) => Task.FromResult<IRangeReader>(new BytesReader(TileArchives.World(6)));
    }
}
