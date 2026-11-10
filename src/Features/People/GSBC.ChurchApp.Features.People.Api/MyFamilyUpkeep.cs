using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class MyFamilyUpkeep(
    FeedChangeBroadcaster broadcaster, IServiceScopeFactory scopes, TimeProvider time, ILogger<MyFamilyUpkeep> logger) : BackgroundService
{
    private const string AccountsPeople = "accounts.people";
    private static readonly TimeSpan Gather = TimeSpan.FromMilliseconds(200);
    private long _seen;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using FeedChangeBroadcaster.Subscription changes = broadcaster.Subscribe();
        await CatchUpAsync(everyone: true, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            ChangedFeeds? changed = await changes.WaitAsync(Timeout.InfiniteTimeSpan, stoppingToken);
            if (changed is null || (!changed.Includes(PeopleFeeds.Mine) && !changed.Includes(AccountsPeople)))
                continue;
            await Task.Delay(Gather, time, stoppingToken);
            await CatchUpAsync(everyone: false, stoppingToken);
        }
    }

    private async Task CatchUpAsync(bool everyone, CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            PeopleDbContext db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
            List<(long Cursor, string RecordId)> marked = (await db.FeedChanges.AsNoTracking()
                    .Where(change => (change.Feed == PeopleFeeds.Mine || change.Feed == AccountsPeople) && change.Cursor > _seen)
                    .Select(change => new { change.Cursor, change.RecordId }).ToListAsync(stoppingToken))
                .Select(change => (change.Cursor, change.RecordId)).ToList();
            List<Guid> people = everyone
                ? [.. await scope.ServiceProvider.GetRequiredService<IPeopleIndex>().EveryoneAsync(stoppingToken)]
                : marked.Select(change => Guid.TryParse(change.RecordId, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();
            if (people.Count > 0)
                await scope.ServiceProvider.GetRequiredService<MyFamilyFeed>().PublishAsync(people, stoppingToken);
            if (marked.Count > 0)
                _seen = marked.Max(change => change.Cursor);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Couldn't bring the family feed up to date");
        }
    }
}
