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

    internal MemoryBlobs Blobs { get; } = new();
    internal AnyMedia Media { get; } = new();
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
            ])
            .Build());
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IBlobStore>(Blobs);
        services.AddSingleton<IMediaReferences>(Media);
        services.AddSingleton<IMediaSystemFolders>(Media);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<Speakers>();
        services.AddScoped<ServiceNames>();
        services.AddScoped<SermonPublisher>();
        services.AddScoped<SermonsService>();
        services.AddScoped<SermonLibraryService>();
        services.AddScoped<SermonUploads>();
        services.AddScoped<SermonDeletion>();
        services.AddScoped<SermonAnnouncer>();
        services.AddScoped<SermonFollowing>();
        services.AddSingleton<GSBC.ChurchApp.Platform.Api.Notifications.INotifications>(new QuietNotifications());
        services.AddSingleton<GSBC.ChurchApp.Platform.Api.Access.IRoleAssignments>(Roles);
        services.AddSingleton<GSBC.ChurchApp.Platform.Api.Access.IPhoneRoles>(Roles);
        services.AddScoped<PodcastFeedWriter>();
        services.AddOptions<PodcastOptions>();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = Environments.Production });
        services.AddSingleton<AudioKeeper>();
        services.AddSyncData();
        services.AddSermonsData();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<SermonsDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected Task SeedLibraryAsync() => WithAsync(async provider =>
    {
        SermonsDbContext db = provider.GetRequiredService<SermonsDbContext>();
        DateTime now = Clock.GetUtcNow().UtcDateTime;
        SpeakerRecord jo = Speaker("Jo Ellery"), lee = Speaker("Ps Lee Varga"), sam = Speaker("Sam Rivers");
        SeriesRecord harbour = new() { Id = Guid.CreateVersion7(), Name = "Safe Harbour", CreatedAt = now };
        SeriesRecord fruit = new() { Id = Guid.CreateVersion7(), Name = "Fruit Of The Spirit", CreatedAt = now };
        db.Speakers.AddRange(jo, lee, sam);
        db.Series.AddRange(harbour, fruit);
        db.Sermons.AddRange(
            Sermon("Safe Harbour: Part 3 - Harbour Of Rest", new DateTime(2026, 10, 25), jo, harbour, SermonServices.SundayMorning),
            Sermon("Safe Harbour: Part 4 - Harbour Of Hope", new DateTime(2026, 11, 1), jo, harbour, SermonServices.SundayMorning),
            Sermon("Walking In The Light", new DateTime(2026, 11, 4), lee, fruit, "Wednesday Night"),
            Sermon("Mercy Before Judgement", new DateTime(2026, 11, 1), sam, null, SermonServices.SundayEvening));
        await db.SaveChangesAsync(Cancel);
        await provider.GetRequiredService<SermonPublisher>().PublishAsync(Cancel);

        SpeakerRecord Speaker(string name) => new() { Id = Guid.CreateVersion7(), Name = name, CreatedAt = now };

        SermonRecord Sermon(string title, DateTime day, SpeakerRecord speaker, SeriesRecord? series, string service) => new()
        {
            Id = Guid.CreateVersion7(),
            Title = title,
            PreachedAt = ChurchTime.ToUtc(day),
            AddedAt = now,
            UpdatedAt = now,
            SpeakerId = speaker.Id,
            SeriesId = series?.Id,
            Service = service,
            AudioUrl = $"https://media.example/{Guid.CreateVersion7():N}.mp3",
            AnnouncedAt = now,
            CheckedAt = now,
        };
    });

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
}
