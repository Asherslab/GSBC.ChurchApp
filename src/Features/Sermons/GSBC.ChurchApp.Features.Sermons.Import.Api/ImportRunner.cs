using GSBC.ChurchApp.Features.Sermons.Api;
using GSBC.ChurchApp.Features.Sermons.Import.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Import.Api.Website;
using GSBC.ChurchApp.Features.Sermons.Import.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api;

internal sealed class ImportRunner(IServiceScopeFactory scopes, TimeProvider time, ILogger<ImportRunner> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);
    private const long AdvisoryLockKey = 0x6773_6263_696d_7074;
    private readonly SemaphoreSlim _wake = new(0, 1);

    public void Wake()
    {
        if (_wake.CurrentCount == 0)
            _wake.Release();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Importing sermons from the website failed; trying again shortly");
            }
            await _wake.WaitAsync(Every, stoppingToken);
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        SermonsImportDbContext db = scope.ServiceProvider.GetRequiredService<SermonsImportDbContext>();
        if (await ImportRuns.GetAsync(db, cancellationToken) is not { State: ImportStates.Waiting or ImportStates.Finding or ImportStates.Importing })
            return;
        await db.Database.OpenConnectionAsync(cancellationToken);
        if (!await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock({AdvisoryLockKey}) AS \"Value\"").SingleAsync(cancellationToken))
            return;
        try
        {
            ImportRun run = await ImportRuns.GetAsync(db, cancellationToken);
            ArchiveClient website = scope.ServiceProvider.GetRequiredService<ArchiveClient>();
            if (run.State is ImportStates.Waiting or ImportStates.Finding && !await FindAsync(db, run, website, cancellationToken))
                return;
            await ImportAsync(db, run, cancellationToken);
        }
        finally
        {
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_unlock({AdvisoryLockKey})", CancellationToken.None);
        }
    }

    private async Task<bool> FindAsync(SermonsImportDbContext db, ImportRun run, ArchiveClient website, CancellationToken cancellationToken)
    {
        run.State = ImportStates.Finding;
        await SaveAsync(db, cancellationToken);
        Dictionary<string, ImportedSermon> known = await db.Sermons.ToDictionaryAsync(sermon => sermon.WebsitePath, cancellationToken);
        int last = 1;
        for (int page = 1; page <= last; page++)
        {
            ListingPage listing;
            try
            {
                listing = ListingPages.Read(await website.PageAsync(ListingPages.PathOf(page), cancellationToken));
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                run.Problem = $"Couldn't read page {page} of the website's sermons ({exception.Message}). Start the import again to carry on.";
                run.State = ImportStates.Finished;
                run.FinishedAt = Now;
                await SaveAsync(db, cancellationToken);
                return false;
            }
            if (page == 1)
                (last, run.WebsiteCount) = (listing.LastPage, listing.WebsiteCount);
            foreach (ListedSermon listed in listing.Sermons)
            {
                if (!known.TryGetValue(listed.Path, out ImportedSermon? sermon))
                {
                    sermon = new ImportedSermon { WebsitePath = listed.Path, Title = listed.Title, PreachedOn = DayOf(listed.Path) };
                    db.Sermons.Add(sermon);
                    known[listed.Path] = sermon;
                }
                sermon.SeenInRun = run.Number;
                sermon.UpdatedAt = Now;
            }
            run.ListingPages = page;
            await SaveAsync(db, cancellationToken);
        }
        run.State = ImportStates.Importing;
        await SaveAsync(db, cancellationToken);
        return true;
    }

    private async Task ImportAsync(SermonsImportDbContext db, ImportRun run, CancellationToken cancellationToken)
    {
        Dictionary<string, Guid> seriesBySlug = [];
        while (await db.Sermons.Where(sermon => sermon.SeenInRun == run.Number && sermon.State == ImportedSermon.Waiting)
                   .OrderByDescending(sermon => sermon.WebsitePath).FirstOrDefaultAsync(cancellationToken) is { } next)
        {
            run.CurrentPath = next.WebsitePath;
            await SaveAsync(db, cancellationToken);
            try
            {
                await using AsyncServiceScope sermonScope = scopes.CreateAsyncScope();
                await sermonScope.ServiceProvider.GetRequiredService<SermonImporter>().ImportAsync(next, seriesBySlug, () => SaveAsync(db, cancellationToken), cancellationToken);
                (next.State, next.Problem, next.ImportedInRun) = (ImportedSermon.Imported, null, run.Number);
            }
            catch (Exception exception) when (ProblemOf(exception, cancellationToken) is { } problem)
            {
                logger.LogWarning(exception, "Couldn't import {Path}", next.WebsitePath);
                (next.State, next.Problem) = (ImportedSermon.Failed, problem);
            }
            next.UpdatedAt = Now;
            await SaveAsync(db, cancellationToken);
        }

        run.CurrentPath = null;
        run.State = ImportStates.Finished;
        run.FinishedAt = Now;
        await SaveAsync(db, cancellationToken);
        await using AsyncServiceScope podcastScope = scopes.CreateAsyncScope();
        await podcastScope.ServiceProvider.GetRequiredService<PodcastFeedWriter>().WriteAsync(cancellationToken);
    }

    internal static string? ProblemOf(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        ImportProblem problem => problem.Message,
        HttpRequestException { StatusCode: { } status } => $"The website answered {(int)status}.",
        HttpRequestException => "The website couldn't be reached.",
        TaskCanceledException when !cancellationToken.IsCancellationRequested => "The website took too long to answer.",
        IOException => "The download was cut short.",
        InvalidDataException => "A file from the website wasn't what it said it was.",
        OperationCanceledException => null,
        _ => "Something went wrong bringing it across. Try it again.",
    };

    internal static DateOnly? DayOf(string websitePath) =>
        DateOnly.TryParseExact(websitePath.Length >= 17 ? websitePath[7..17] : "", "yyyy-MM-dd", out DateOnly day) ? day : null;

    private async Task SaveAsync(SermonsImportDbContext db, CancellationToken cancellationToken)
    {
        db.FeedChanges.Add(FeedChangeRecord.Marker(SermonsImportFeeds.Progress, ImportRuns.RecordId, Now));
        await db.SaveChangesAsync(cancellationToken);
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;
}

internal static class ImportRuns
{
    public const string RecordId = "run";

    public static async Task<ImportRun> GetAsync(SermonsImportDbContext db, CancellationToken cancellationToken) =>
        await db.Runs.FindAsync([ImportRun.SingleId], cancellationToken) ?? db.Runs.Add(new ImportRun()).Entity;
}
