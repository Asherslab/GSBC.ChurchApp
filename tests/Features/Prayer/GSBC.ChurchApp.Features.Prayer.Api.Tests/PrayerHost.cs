using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

public abstract class PrayerHost : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    internal TestClock Clock { get; } = new(ChurchTime.ToUtc(new DateTime(2026, 11, 11, 9, 0, 0)));
    internal RecordedNotifications Told { get; } = new();
    internal KnownPeople People { get; } = new();

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    protected static readonly Guid Staff = Guid.CreateVersion7();

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
            .Build());
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<INotifications>(Told);
        services.AddSingleton<IPeopleIndex>(People);
        services.AddSingleton<IFamilies>(People);
        services.AddScoped<PrayerPublisher>().AddScoped<PrayerTelling>().AddScoped<PrayerRequests>().AddScoped<PastoralCare>().AddScoped<PrayerKeeping>().AddScoped<HandledRequests>();
        services.AddScoped<PrayerPersonData>();
        services.AddSyncData();
        services.AddPrayerData();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<PrayerDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
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

    private protected Task AppAsync(Func<PrayerRequests, Task> work) => WithAsync(provider => work(provider.GetRequiredService<PrayerRequests>()));

    private protected Task StaffAsync(Func<PastoralCare, Task> work) => WithAsync(provider => work(provider.GetRequiredService<PastoralCare>()));

    protected async Task<string> SendAsync(Guid install, Guid? user, string text, bool forWall = false, bool showName = false, string name = "")
    {
        string id = Guid.NewGuid().ToString();
        await AppAsync(app => app.SendAsync(install, user,
            new PrayerRequestSend { Id = id, Text = text, ForWall = forWall, ShowName = showName, Name = name }, Cancel));
        return id;
    }

    protected Task ShowAsync(string id, bool showName = true, string? queuedId = null) => StaffAsync(care => care.ShowOnWallAsync(Staff,
        new WallSave { Id = id, Text = "Please pray for us.", ShowName = showName, Until = Clock.GetUtcNow().UtcDateTime.AddDays(30) }, queuedId, Cancel));

    protected async Task<List<FeedChangeRecord>> RowsAsync(string feed) =>
        await WithAsync(provider => provider.GetRequiredService<PrayerDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == feed).OrderBy(change => change.Cursor).ToListAsync(Cancel));

    protected async Task<List<FeedChangeRecord>> LiveRowsAsync(string feed) =>
        (await RowsAsync(feed)).GroupBy(change => change.RecordId).Select(group => group.Last()).Where(change => !change.Deleted).ToList();

    protected async Task<List<WallPost>> WallAsync() =>
        (await LiveRowsAsync(PrayerFeeds.Wall)).Select(row => PrayerPayloads.Decode<WallPost>(row.Payload)).ToList();

    private protected Task<RequestRecord?> RecordAsync(string id) =>
        WithAsync(provider => provider.GetRequiredService<PrayerDbContext>().Requests.AsNoTracking().FirstOrDefaultAsync(request => request.Id == Guid.Parse(id), Cancel));
}
