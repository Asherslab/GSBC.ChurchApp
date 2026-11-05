using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class SyncOriginTests : IAsyncLifetime
{
    private const string Series = "sermons.series";
    private const string Events = "events";
    private static readonly FeedRegistry Feeds = new([new FeedDefinition(Series, FeedAudience.Public), new FeedDefinition(Events, FeedAudience.Public)]);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private ServiceProvider _main = null!;
    private ServiceProvider _rebuilt = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _main = await DatabaseAsync("main");
        _rebuilt = await DatabaseAsync("rebuilt");
    }

    public async ValueTask DisposeAsync()
    {
        await _main.DisposeAsync();
        await _rebuilt.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task EachDatabaseHasItsOwnOrigin()
    {
        Assert.NotEqual((await PullAsync(_main, null, [])).Origin, (await PullAsync(_rebuilt, null, [])).Origin);
    }

    [Fact]
    public async Task APhoneFromAnotherDatabaseStartsEveryFeedAgain()
    {
        await AddAsync(_main, Series, "anchored-main");
        long seen = await AddAsync(_main, Events, "easter-main");
        string? mainOrigin = (await PullAsync(_main, null, [])).Origin;
        await AddAsync(_rebuilt, Series, "anchored-rebuilt");
        await AddAsync(_rebuilt, Events, "easter-rebuilt");
        await AddAsync(_rebuilt, Events, "pentecost-rebuilt");

        PullReply reply = await PullAsync(_rebuilt, mainOrigin, new() { [Series] = seen, [Events] = seen, ["retired.feed"] = seen });

        Assert.All(reply.Feeds, batch => Assert.True(batch is { Reset: true, Cursor: 0, Changes.Count: 0 }));
        Assert.Equal([Events, "retired.feed", Series], reply.Feeds.Select(batch => batch.Feed).Order());
        Assert.Equal((await PullAsync(_rebuilt, null, [])).Origin, reply.Origin);
    }

    [Fact]
    public async Task APhoneThatHasNeverHeardAnOriginStartsAgainOnce()
    {
        long seen = await AddAsync(_main, Series, "anchored");

        PullReply first = await PullAsync(_main, "unknown", new() { [Series] = seen });
        PullReply again = await PullAsync(_main, first.Origin, new() { [Series] = 0 });
        PullReply after = await PullAsync(_main, first.Origin, new() { [Series] = again.Feeds.Single().Cursor });

        Assert.True(first.Feeds.Single().Reset);
        Assert.Equal(["anchored"], again.Feeds.Single().Changes.Select(change => change.Id));
        Assert.Empty(after.Feeds);
    }

    [Fact]
    public async Task ACursorAheadOfTheServerStartsTheFeedAgain()
    {
        long seen = await AddAsync(_main, Series, "anchored");
        string? origin = (await PullAsync(_main, null, [])).Origin;

        FeedBatch batch = (await PullAsync(_main, origin, new() { [Series] = seen + 10 })).Feeds.Single();

        Assert.True(batch.Reset);
        Assert.Equal(0, batch.Cursor);
        Assert.True(batch.HasMore);
    }

    [Fact]
    public async Task TheSameOriginAndAValidCursorNeverStartAgain()
    {
        await AddAsync(_main, Series, "anchored");
        long seen = await AddAsync(_main, Series, "ten-commandments");
        long expired = await AddAsync(_main, Events, "cancelled", deleted: true, committedAt: DateTime.UtcNow.AddDays(-200));
        await using (AsyncServiceScope scope = _main.CreateAsyncScope())
            await new FeedRetention(scope.ServiceProvider.GetRequiredService<SyncDbContext>()).PruneAsync(DateTime.UtcNow, TestContext.Current.CancellationToken);
        string? origin = (await PullAsync(_main, null, [])).Origin;

        PullReply current = await PullAsync(_main, origin, new() { [Series] = seen, [Events] = expired });
        PullReply olderClient = await PullAsync(_main, null, new() { [Series] = seen - 1 });

        Assert.Empty(current.Feeds);
        FeedBatch caughtUp = olderClient.Feeds.Single();
        Assert.False(caughtUp.Reset);
        Assert.Equal(["ten-commandments"], caughtUp.Changes.Select(change => change.Id));
    }

    private async Task<ServiceProvider> DatabaseAsync(string name)
    {
        string connection = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = name }.ConnectionString;
        ServiceProvider services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", connection)])
                .Build())
            .AddSyncData()
            .BuildServiceProvider();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        return services;
    }

    private static async Task<long> AddAsync(ServiceProvider database, string feed, string id, bool deleted = false, DateTime? committedAt = null)
    {
        await using AsyncServiceScope scope = database.CreateAsyncScope();
        SyncDbContext db = scope.ServiceProvider.GetRequiredService<SyncDbContext>();
        FeedChangeRecord change = new()
        {
            Feed = feed, RecordId = id, Version = 1, Deleted = deleted, Payload = deleted ? [] : [1], CommittedAt = committedAt ?? DateTime.UtcNow,
        };
        db.FeedChanges.Add(change);
        await db.SaveChangesAsync();
        return change.Cursor;
    }

    private static async Task<PullReply> PullAsync(ServiceProvider database, string? origin, Dictionary<string, long> cursors)
    {
        await using AsyncServiceScope scope = database.CreateAsyncScope();
        SyncService sync = new(
            scope.ServiceProvider.GetRequiredService<SyncDbContext>(), Feeds, new NothingAllowed(), new FeedChangeBroadcaster(), new LiveFeedTests.Sessions());
        return await sync.PullAsync(new PullRequest
        {
            Origin = origin,
            Cursors = cursors.Select(cursor => new FeedCursor { Feed = cursor.Key, Cursor = cursor.Value }).ToList(),
        });
    }

    private sealed class NothingAllowed : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(false);

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);
    }
}
