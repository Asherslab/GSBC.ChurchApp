using System.Security.Claims;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class FeedRetentionTests : IAsyncLifetime
{
    private const string Feed = "events";
    private static readonly DateTime Now = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddSyncData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
    }

    [Fact]
    public async Task AfterPruningEveryPhoneConvergesOnTheLiveRecords()
    {
        await AddAsync("e1", 1, committedAt: Now.AddDays(-200));
        await AddAsync("e2", 1, committedAt: Now.AddDays(-200));
        long oldPhone = await AddAsync("e3", 1, committedAt: Now.AddDays(-150));
        await AddAsync("e1", 2, committedAt: Now.AddDays(-120));
        await AddAsync("e2", 2, committedAt: Now.AddDays(-100), deleted: true);
        long upToDate = await AddAsync("e4", 1, committedAt: Now.AddDays(-1));

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
            Assert.True(await new FeedRetention(scope.ServiceProvider.GetRequiredService<SyncDbContext>()).PruneAsync(Now, TestContext.Current.CancellationToken));

        FeedBatch fresh = await PullAsync(0);
        Assert.False(fresh.Reset);
        Assert.Equal(["e3:1", "e1:2", "e4:1"], fresh.Changes.Select(change => $"{change.Id}:{change.Version}"));

        FeedBatch behind = await PullAsync(oldPhone);
        Assert.True(behind.Reset);
        Assert.Empty(behind.Changes);
        Assert.True(behind.HasMore);
        Assert.Equal(0, behind.Cursor);

        Assert.Empty((await PullAsync(upToDate)).Changes);
    }

    private async Task<long> AddAsync(string id, long version, DateTime committedAt, bool deleted = false)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        SyncDbContext db = scope.ServiceProvider.GetRequiredService<SyncDbContext>();
        FeedChangeRecord change = new()
        {
            Feed = Feed, RecordId = id, Version = version, Deleted = deleted, Payload = deleted ? [] : [1], CommittedAt = committedAt,
        };
        db.FeedChanges.Add(change);
        await db.SaveChangesAsync();
        return change.Cursor;
    }

    private async Task<FeedBatch> PullAsync(long cursor)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        SyncService sync = new(
            scope.ServiceProvider.GetRequiredService<SyncDbContext>(),
            new FeedRegistry([new FeedDefinition(Feed, FeedAudience.Public)]),
            new NothingAllowed(),
            new FeedChangeBroadcaster(),
            new LiveFeedTests.Sessions(),
            new TestClock());

        PullReply reply = await sync.PullAsync(new PullRequest { Cursors = [new FeedCursor { Feed = Feed, Cursor = cursor }] });
        return reply.Feeds.SingleOrDefault() ?? new FeedBatch { Feed = Feed, Cursor = cursor };
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    private sealed class NothingAllowed : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(false);

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);
    }
}
