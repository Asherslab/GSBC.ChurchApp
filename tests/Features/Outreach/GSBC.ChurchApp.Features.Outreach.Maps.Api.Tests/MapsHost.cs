using Amazon.S3;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Places;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

internal sealed class MapsHost : IAsyncDisposable
{
    private readonly TestDatabase _database;
    private readonly ServiceProvider _services;

    private MapsHost(TestDatabase database, ServiceProvider services, TestClock clock, FakeSources sources, FakeUses uses)
    {
        _database = database;
        _services = services;
        Clock = clock;
        Sources = sources;
        Uses = uses;
    }

    public TestClock Clock { get; }
    public FakeSources Sources { get; }
    public FakeUses Uses { get; }

    public static async Task<MapsHost> StartAsync()
    {
        TestDatabase database = await TestDatabase.CreateAsync();
        TestBuckets buckets = await TestBuckets.CreateAsync();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Database.ConnectionStringName}"] = database.ConnectionString,
            ["Storage:Blobs:ServiceUrl"] = buckets.ServiceUrl,
            ["Storage:Blobs:AccessKeyId"] = buckets.AccessKey,
            ["Storage:Blobs:SecretAccessKey"] = buckets.SecretKey,
            ["Storage:Blobs:PublicBucket"] = buckets.Public,
            ["Storage:Blobs:PrivateBucket"] = buckets.Private,
            ["Storage:Blobs:PublicBaseUrl"] = $"{buckets.ServiceUrl}/{buckets.Public}",
        }).Build();
        TestClock clock = new();
        FakeSources sources = new();
        FakeUses uses = new();
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging()
            .AddMemoryCache()
            .AddOptions()
            .AddSingleton<TimeProvider>(clock)
            .AddBlobStore(configuration)
            .AddOutreachMapsData()
            .AddSyncData()
            .AddSingleton<IMapSources>(sources)
            .AddSingleton<IRegionUses>(uses)
            .AddScoped<AreaBuilder>()
            .AddScoped<PlaceIndex>()
            .AddScoped<AddressIndex>()
            .AddScoped<RegionEstimates>()
            .AddSingleton<AreaBuildRunner>()
            .AddScoped<OutreachMapsService>()
            .BuildServiceProvider();
        await services.GetRequiredService<IAmazonS3>().PutBucketAsync(buckets.Public);
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>().Database.MigrateAsync();
        return new MapsHost(database, services, clock, sources, uses);
    }

    public async Task<T> WithServiceAsync<T>(Func<IOutreachMapsService, ValueTask<T>> call)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<OutreachMapsService>());
    }

    public async Task<T> WithAddressesAsync<T>(Func<AddressIndex, Task<T>> call)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await call(scope.ServiceProvider.GetRequiredService<AddressIndex>());
    }

    public async Task<T> WithDbAsync<T>(Func<OutreachMapsDbContext, Task<T>> read)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await read(scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>());
    }

    public async Task BuildAsync(string areaId)
    {
        Clock.Advance(TimeSpan.FromHours(1));
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        OutreachMapsDbContext db = scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>();
        await scope.ServiceProvider.GetRequiredService<AreaBuilder>().BuildAsync(await db.Areas.SingleAsync(area => area.Id == areaId), TestContext.Current.CancellationToken);
    }

    public Task<List<RegionRecord>> RegionsAsync(string areaId) =>
        WithDbAsync(db => db.Regions.AsNoTracking().Where(region => region.AreaId == areaId).OrderBy(region => region.Id).ToListAsync());

    public async Task<BlobInfo?> FindAsync(string key)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IBlobStore>().FindAsync(BlobBucket.Public, key, TestContext.Current.CancellationToken);
    }

    public async Task<FeedChangeRecord?> LatestFeedRowAsync(string regionId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SyncDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == OutreachMapsFeeds.Regions && change.RecordId == regionId)
            .OrderByDescending(change => change.Cursor).FirstOrDefaultAsync();
    }

    public async Task<int> FeedRowsAsync(string regionId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SyncDbContext>().FeedChanges
            .CountAsync(change => change.Feed == OutreachMapsFeeds.Regions && change.RecordId == regionId);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }
}

internal sealed class FakeUses : IRegionUses
{
    public List<RegionUse> Uses { get; } = [];

    public Task<IReadOnlyList<RegionUse>> UsesAsync(IReadOnlyCollection<string> regionIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RegionUse>>([.. Uses.Where(use => regionIds.Contains(use.RegionId))]);
}
