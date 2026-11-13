using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Api.Tests;

public abstract class NewsHost : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    internal TestClock Clock { get; } = new(ChurchTime.ToUtc(new DateTime(2026, 11, 13, 9, 0, 0)));
    internal RecordedNotifications Told { get; } = new();
    internal KnownPeople People { get; } = new();
    internal LibraryMedia Media { get; } = new();

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    protected static readonly Guid Office = Guid.CreateVersion7();
    protected static readonly Guid Pastor = Guid.CreateVersion7();
    protected static readonly Guid Leader = Guid.CreateVersion7();

    private protected static readonly Writer Writing = new(Office, MayPublish: false);
    private protected static readonly Writer Publishing = new(Pastor, MayPublish: true);

    public async ValueTask InitializeAsync()
    {
        People.Names[Office] = "Office Writer";
        People.Names[Pastor] = "Pastor Publisher";
        People.Names[Leader] = "Kids Leader";
        _database = await TestDatabase.CreateAsync();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
            .Build());
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<INotifications>(Told);
        services.AddSingleton<IPeopleIndex>(People);
        services.AddSingleton<IMediaReferences>(Media);
        services.AddSingleton<IMediaSystemFolders>(Media);
        services.AddScoped<NewsPublisher>().AddScoped<NewsTelling>().AddScoped<NewsDesk>().AddScoped<IChurchNews, ChurchNewsShares>().AddScoped<NewsPersonData>();
        services.AddSyncData();
        services.AddNewsData();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<NewsDbContext>().Database.MigrateAsync();
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

    private protected Task<T> DeskAsync<T>(Func<NewsDesk, Task<T>> work) => WithAsync(provider => work(provider.GetRequiredService<NewsDesk>()));

    private protected Task DeskAsync(Func<NewsDesk, Task> work) => WithAsync(async provider =>
    {
        await work(provider.GetRequiredService<NewsDesk>());
        return true;
    });

    private protected Task SharesAsync(Func<IChurchNews, Task> work) => WithAsync(async provider =>
    {
        await work(provider.GetRequiredService<IChurchNews>());
        return true;
    });

    protected static NewsPost Draft(string title = "Carols on the lawn", string audience = "", bool tell = true) => new()
    {
        Id = Guid.NewGuid().ToString(), New = true, Title = title, Body = new RichText { Markdown = "Bring a **picnic rug**." },
        Audience = audience.Length > 0 ? audience : Audience.Everyone.Text, Tell = tell,
    };

    private protected async Task<NewsPost> WriteAsync(NewsPost draft, Writer? writer = null) =>
        await DeskAsync(desk => desk.SaveAsync(writer ?? Publishing, draft, Cancel));

    private protected async Task<NewsPost> PublishedAsync(NewsPost draft) =>
        await DeskAsync(desk => desk.PublishAsync(Publishing, Guid.Parse(draft.Id), null, Cancel));

    protected async Task<List<FeedChangeRecord>> RowsAsync(string feed) =>
        await WithAsync(provider => provider.GetRequiredService<NewsDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == feed).OrderBy(change => change.Cursor).ToListAsync(Cancel));

    protected async Task<List<FeedChangeRecord>> LiveRowsAsync(string feed) =>
        (await RowsAsync(feed)).GroupBy(change => change.RecordId).Select(group => group.Last()).Where(change => !change.Deleted).ToList();

    protected async Task<List<NewsPost>> ReadersSeeAsync(string feed) =>
        (await LiveRowsAsync(feed)).Select(row => NewsPayloads.Decode<NewsPost>(row.Payload)).ToList();
}
