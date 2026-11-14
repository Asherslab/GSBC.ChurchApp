using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

public abstract class OutreachHost : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    internal TestClock Clock { get; } = new(ChurchTime.ToUtc(new DateTime(2026, 11, 14, 9, 0, 0)));
    internal RecordedNotifications Told { get; } = new();
    internal KnownPeople People { get; } = new();

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    protected static readonly Guid Staff = Guid.CreateVersion7();
    protected static readonly Guid Phone = Guid.CreateVersion7();
    protected static readonly string General = OutreachCampaigns.GeneralId.ToString();

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
        services.AddSingleton<IMinistryFacts>(new OutreachMinistryFacts());
        NoMedia media = new();
        services.AddSingleton<IMediaReferences>(media).AddSingleton<IMediaSystemFolders>(media);
        services.AddScoped<OutreachMinistries>().AddScoped<OutreachPublisher>().AddScoped<OutreachSettingsStore>().AddScoped<OutreachMedia>()
            .AddScoped<OutreachTelling>().AddScoped<OutreachLeaderboards>().AddScoped<Walking>().AddScoped<OutreachPlanning>().AddScoped<OutreachReview>()
            .AddScoped<OutreachKeeping>().AddScoped<OutreachPersonData>();
        services.AddSyncData();
        services.AddOutreachData();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<OutreachDbContext>().Database.MigrateAsync();
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

    private protected Task WalkAsync(Func<Walking, Task> work) => WithAsync(provider => work(provider.GetRequiredService<Walking>()));

    private protected Task PlanAsync(Func<OutreachPlanning, Task> work) => WithAsync(provider => work(provider.GetRequiredService<OutreachPlanning>()));

    private protected Task<T> ReviewAsync<T>(Func<OutreachReview, Task<T>> work) => WithAsync(provider => work(provider.GetRequiredService<OutreachReview>()));

    private protected Task ReviewAsync(Func<OutreachReview, Task> work) => WithAsync(provider => work(provider.GetRequiredService<OutreachReview>()));

    protected Task KeepAsync() => WithAsync(provider => provider.GetRequiredService<OutreachKeeping>().RunAsync(Cancel));

    protected static Stretch Side(string street, int metres = 100, int letterboxes = 10, StreetSide side = StreetSide.Left) => new()
    {
        Region = "church-area", RegionVersion = 1, Street = street, Side = side, From = 0, To = OutreachRules.WholeStreet,
        Middle = new GeoPoint { Lat = -27.35, Lng = 152.97 }, Metres = metres, Letterboxes = letterboxes,
    };

    protected WalkPiece Piece(string walkId, int seq, IEnumerable<Stretch> covered, int metres, TimeSpan after, params string[] campaigns) => new()
    {
        WalkId = walkId, Seq = seq, Campaigns = [.. campaigns], StartedAt = Clock.GetUtcNow().UtcDateTime - after, At = Clock.GetUtcNow().UtcDateTime,
        Metres = metres, Covered = [.. covered],
    };

    protected async Task<string> CampaignAsync(string name)
    {
        string id = Guid.CreateVersion7().ToString();
        await PlanAsync(planning => planning.SaveCampaignAsync(new CampaignSave { Campaign = new Campaign { Id = id, Name = name } }, Cancel));
        return id;
    }

    protected async Task<List<FeedChangeRecord>> LiveRowsAsync(string feed) =>
        (await WithAsync(provider => provider.GetRequiredService<OutreachDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == feed).OrderBy(change => change.Cursor).ToListAsync(Cancel)))
        .GroupBy(change => change.RecordId).Select(group => group.Last()).Where(change => !change.Deleted).ToList();

    protected async Task<List<T>> FeedAsync<T>(string feed) =>
        (await LiveRowsAsync(feed)).Select(row => OutreachPayloads.Decode<T>(row.Payload)).ToList();

    private protected Task<List<T>> RecordsAsync<T>(Func<OutreachDbContext, IQueryable<T>> query) =>
        WithAsync(provider => query(provider.GetRequiredService<OutreachDbContext>()).ToListAsync(Cancel));
}
