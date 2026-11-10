using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class MyAccountUpkeep(
    FeedChangeBroadcaster broadcaster, IServiceScopeFactory scopes, TimeProvider time, ILogger<MyAccountUpkeep> logger) : BackgroundService
{
    private static readonly TimeSpan Sweep = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Gather = TimeSpan.FromMilliseconds(200);
    private long _seen;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using FeedChangeBroadcaster.Subscription changes = broadcaster.Subscribe();
        await CatchUpAsync(everyone: true, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            ChangedFeeds? changed = await changes.WaitAsync(Sweep, stoppingToken);
            if (changed is not null && !changed.Includes(AccountsFeeds.People))
                continue;
            await Task.Delay(Gather, time, stoppingToken);
            await CatchUpAsync(everyone: changed is null, stoppingToken);
        }
    }

    private async Task CatchUpAsync(bool everyone, CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            AccountsDbContext db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
            List<(long Cursor, string RecordId)> marked = (await db.FeedChanges.AsNoTracking()
                    .Where(change => change.Feed == AccountsFeeds.People && change.Cursor > _seen)
                    .Select(change => new { change.Cursor, change.RecordId }).ToListAsync(stoppingToken))
                .Select(change => (change.Cursor, change.RecordId)).ToList();
            List<Guid> people = marked.Select(change => Guid.TryParse(change.RecordId, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();
            if (everyone || people.Count > 0)
                await scope.ServiceProvider.GetRequiredService<MyAccountFeed>().PublishAsync(everyone ? null : people, stoppingToken);
            if (marked.Count > 0)
                _seen = marked.Max(change => change.Cursor);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Couldn't bring the account feed up to date");
        }
    }
}
