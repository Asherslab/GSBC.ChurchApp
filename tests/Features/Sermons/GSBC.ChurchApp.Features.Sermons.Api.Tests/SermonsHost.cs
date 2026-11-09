using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public abstract class SermonsHost : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    internal FakeWebsite Website { get; } = new();
    internal MemoryBlobs Blobs { get; } = new();
    internal AnyMedia Media { get; } = new();
    internal RecordedNotifications Notifications { get; } = new();
    internal RecordedRoles Roles { get; } = new();
    internal TestClock Clock { get; } = new(ChurchTime.ToUtc(new DateTime(2026, 11, 4, 20, 0, 0)));

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection([
                new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString),
                new($"{WebsiteOptions.Section}:BaseUrl", FakeWebsite.BaseUrl),
            ])
            .Build());
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IBlobStore>(Blobs);
        services.AddSingleton<IMediaReferences>(Media);
        services.AddSingleton<IMediaSystemFolders>(Media);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddOptions<WebsiteOptions>().Configure(options => options.BaseUrl = FakeWebsite.BaseUrl);
        services.AddHttpClient<WebsiteClient>().ConfigurePrimaryHttpMessageHandler(() => Website);
        services.AddScoped<Artwork>();
        services.AddScoped<Speakers>();
        services.AddScoped<ServiceNames>();
        services.AddScoped<SermonPublisher>();
        services.AddScoped<WebsiteReader>();
        services.AddScoped<SourceIdMove>();
        services.AddScoped<SermonsService>();
        services.AddScoped<SermonLibraryService>();
        services.AddScoped<SermonUploads>();
        services.AddScoped<SermonAnnouncer>();
        services.AddScoped<SermonFollowing>();
        services.AddSingleton<GSBC.ChurchApp.Platform.Api.Notifications.INotifications>(Notifications);
        services.AddSingleton<GSBC.ChurchApp.Platform.Api.Access.IRoleAssignments>(Roles);
        services.AddSingleton<GSBC.ChurchApp.Platform.Api.Access.IPhoneRoles>(Roles);
        services.AddScoped<PodcastFeedWriter>();
        services.AddOptions<PodcastOptions>();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = Environments.Production });
        services.AddSingleton<AudioCopier>();
        services.AddSingleton<WebsiteSchedule>();
        services.AddSyncData();
        services.AddSermonsData();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<SermonsDbContext>().Database.MigrateAsync();
        ServeTheSampleSite();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected static string Sample(string name) => SampleFiles.Text(name);

    private void ServeTheSampleSite()
    {
        Website.ServeFeed(Sample("feed.rss"));
        Website.ServeText($"{FakeWebsite.BaseUrl}/podcasts/media", Sample("media.html"));
        foreach (string slug in new[] { "safe-harbour", "fruit-of-the-spirit", "older-days" })
            Website.ServeText($"{FakeWebsite.BaseUrl}/podcasts/media/series/{slug}", Sample($"series-{slug}.html"));
        foreach (string mp3 in new[] { "2026/11/04/walking-in-the-light.mp3", "2026/11/01/harbour-4.mp3", "2026/11/01/mercy.mp3", "2026/10/25/harbour-3.mp3" })
            Website.ServeBytes($"https://files.example/{mp3}", ConstantBitrateMp3());
        foreach (string picture in new[] { "2026/11/01/art/harbour-4.png", "2026/10/25/art/harbour-3.png", "2026/10/series/safe-harbour.png", "2025/series/fruit.png", "2019/series/older.png" })
            Website.ServeBytes($"https://files.example/{picture}", Picture(1920, 1080));
        foreach (string person in new[] { "people/ellery.jpg", "people/varga.jpg" })
            Website.ServeBytes($"https://files.example/{person}", Picture(300, 400));
    }

    protected async Task ReadTheWebsiteAsync(bool force = false)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<WebsiteReader>().ReadAsync(force, Cancel);
    }

    protected async Task<T> WithAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    protected Task WithAsync(Func<IServiceProvider, Task> work) => WithAsync(async provider =>
    {
        await work(provider);
        return true;
    });

    protected async Task<List<FeedChangeRecord>> LiveRowsAsync(string feed)
    {
        List<FeedChangeRecord> rows = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == feed).ToListAsync(Cancel));
        return rows.GroupBy(change => change.RecordId)
            .Select(group => group.MaxBy(change => change.Cursor)!)
            .Where(change => !change.Deleted)
            .ToList();
    }

    protected async Task<List<Sermon>> CatalogueAsync() =>
        (await LiveRowsAsync(SermonsFeeds.Catalogue)).Select(row => SermonsPayloads.Decode<Sermon>(row.Payload)).ToList();

    private static byte[] ConstantBitrateMp3() => SampleFiles.ConstantBitrateMp3();

    protected static byte[] Picture(int width, int height, uint colour = 0xFF2A3D5E) => SampleFiles.Picture(width, height, colour);
}
