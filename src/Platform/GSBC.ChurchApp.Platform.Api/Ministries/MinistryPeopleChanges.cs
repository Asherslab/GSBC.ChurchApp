using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Platform.Api.Ministries;

public interface IMinistryPeopleChanged
{
    Task ChangedAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken);
}

public static class MinistryPeopleChanges
{
    public const string Feed = "ministries.people-changed";

    public static FeedChangeRecord Signal(Guid ministryId, DateTime at) => FeedChangeRecord.Marker(Feed, ministryId.ToString(), at);

    public static IServiceCollection AddMinistryPeopleHandler<THandler>(this IServiceCollection services) where THandler : class, IMinistryPeopleChanged
    {
        services.AddScoped<IMinistryPeopleChanged, THandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MinistryPeopleWatcher>());
        return services;
    }

    public static async Task<(long Cursor, IReadOnlyList<Guid> Ministries)> SinceAsync(SyncDbContext db, long after, CancellationToken cancellationToken)
    {
        List<(long Cursor, string RecordId)> rows = (await db.FeedChanges.AsNoTracking()
                .Where(change => change.Feed == Feed && change.Cursor > after)
                .Select(change => new { change.Cursor, change.RecordId })
                .ToListAsync(cancellationToken))
            .Select(row => (row.Cursor, row.RecordId)).ToList();
        return rows.Count == 0
            ? (after, [])
            : (rows.Max(row => row.Cursor), rows.Select(row => Guid.TryParse(row.RecordId, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList());
    }

    public static async Task<long> LatestAsync(SyncDbContext db, CancellationToken cancellationToken) =>
        await db.FeedChanges.AsNoTracking().Where(change => change.Feed == Feed).MaxAsync(change => (long?)change.Cursor, cancellationToken) ?? 0;
}

internal sealed class MinistryPeopleWatcher(IServiceScopeFactory scopes, FeedChangeBroadcaster broadcaster, ILogger<MinistryPeopleWatcher> logger) : BackgroundService
{
    private static readonly TimeSpan Sweep = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using FeedChangeBroadcaster.Subscription subscription = broadcaster.Subscribe();
        long seen = await LatestAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            ChangedFeeds? changed = await subscription.WaitAsync(Sweep, stoppingToken);
            if (changed is null || changed.Includes(MinistryPeopleChanges.Feed))
                seen = await DeliverAsync(seen, stoppingToken);
        }
    }

    private async Task<long> LatestAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                return await MinistryPeopleChanges.LatestAsync(scope.ServiceProvider.GetRequiredService<SyncDbContext>(), stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Couldn't read where the ministry people signals are up to; trying again shortly");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<long> DeliverAsync(long seen, CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            (long cursor, IReadOnlyList<Guid> ministries) = await MinistryPeopleChanges.SinceAsync(scope.ServiceProvider.GetRequiredService<SyncDbContext>(), seen, stoppingToken);
            if (ministries.Count == 0)
                return cursor;
            foreach (IMinistryPeopleChanged handler in scope.ServiceProvider.GetServices<IMinistryPeopleChanged>())
                await DeliverToAsync(handler, ministries, stoppingToken);
            return cursor;
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Couldn't pass on that a ministry's people changed; trying again on the next change");
            return seen;
        }
    }

    private async Task DeliverToAsync(IMinistryPeopleChanged handler, IReadOnlyList<Guid> ministries, CancellationToken stoppingToken)
    {
        try
        {
            await handler.ChangedAsync(ministries, stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "{Handler} couldn't catch up with a ministry's people; its own sweep will", handler.GetType().Name);
        }
    }
}
