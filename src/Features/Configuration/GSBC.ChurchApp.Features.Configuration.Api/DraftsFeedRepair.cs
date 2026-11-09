using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class DraftsFeedRepair(IServiceScopeFactory scopes, ILogger<DraftsFeedRepair> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            int removed = await RemoveMissingAsync(scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>(), stoppingToken);
            if (removed > 0)
                logger.LogWarning("Took {Count} drafts off the drafts feed that were no longer in the database", removed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Checking the drafts feed against the drafts failed");
        }
    }

    public static async Task<int> RemoveMissingAsync(ConfigurationDbContext db, CancellationToken cancellationToken)
    {
        List<FeedChangeRecord> latest = await db.FeedChanges.AsNoTracking()
            .Where(change => change.Feed == ConfigurationFeeds.Drafts)
            .GroupBy(change => change.RecordId)
            .Select(versions => versions.OrderByDescending(change => change.Cursor).First())
            .ToListAsync(cancellationToken);
        HashSet<string> drafts = (await db.Drafts.AsNoTracking().Select(draft => draft.Id).ToListAsync(cancellationToken)).Select(id => id.ToString()).ToHashSet();

        List<FeedChangeRecord> missing = latest.Where(change => !change.Deleted && !drafts.Contains(change.RecordId)).ToList();
        foreach (FeedChangeRecord gone in missing)
            db.FeedChanges.Add(new FeedChangeRecord
            {
                Feed = ConfigurationFeeds.Drafts, RecordId = gone.RecordId, Version = gone.Version + 1, Deleted = true, ScopeId = gone.ScopeId,
            });
        await db.SaveChangesAsync(cancellationToken);
        return missing.Count;
    }
}
