using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Import.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Import.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api;

internal sealed class SermonImportService(SermonsImportDbContext db, ImportRunner runner, TimeProvider time) : ISermonImportService
{
    private const int FailuresShown = 200;

    public async ValueTask<ImportProgress> ProgressAsync(ImportRequest request, CallContext context = default) =>
        await ProgressAsync(context.CancellationToken);

    public async ValueTask<ImportProgress> StartAsync(ImportRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ImportRun run = await ImportRuns.GetAsync(db, cancellationToken);
        if (run.State is ImportStates.NotStarted or ImportStates.Finished)
        {
            run.Number++;
            (run.State, run.StartedAt, run.FinishedAt, run.Problem, run.ListingPages, run.CurrentPath) = (ImportStates.Waiting, Now, null, null, 0, null);
            await db.Sermons.Where(sermon => sermon.State == ImportedSermon.Failed)
                .ExecuteUpdateAsync(set => set.SetProperty(sermon => sermon.State, ImportedSermon.Waiting).SetProperty(sermon => sermon.Problem, (string?)null), cancellationToken);
            await SaveAsync(cancellationToken);
            runner.Wake();
        }
        return await ProgressAsync(cancellationToken);
    }

    public async ValueTask<ImportProgress> RetryAsync(ImportRetry request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        ImportedSermon sermon = await db.Sermons.FindAsync([request.WebsitePath], cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That sermon isn't in the import."));
        if (sermon.State != ImportedSermon.Failed)
            return await ProgressAsync(cancellationToken);
        ImportRun run = await ImportRuns.GetAsync(db, cancellationToken);
        (sermon.State, sermon.Problem, sermon.SeenInRun) = (ImportedSermon.Waiting, null, run.Number);
        if (run.State is ImportStates.NotStarted or ImportStates.Finished)
            (run.State, run.FinishedAt) = (ImportStates.Importing, null);
        await SaveAsync(cancellationToken);
        runner.Wake();
        return await ProgressAsync(cancellationToken);
    }

    private async Task<ImportProgress> ProgressAsync(CancellationToken cancellationToken)
    {
        ImportRun run = await db.Runs.AsNoTracking().SingleOrDefaultAsync(cancellationToken) ?? new ImportRun();
        List<ImportedSermon> seen = await db.Sermons.AsNoTracking().Where(sermon => sermon.SeenInRun == run.Number || sermon.State == ImportedSermon.Failed)
            .ToListAsync(cancellationToken);
        return Progress(run, seen);
    }

    internal static ImportProgress Progress(ImportRun run, IReadOnlyCollection<ImportedSermon> seen) => new()
    {
        State = run.State,
        StartedAt = run.StartedAt,
        FinishedAt = run.FinishedAt,
        Problem = run.Problem,
        ListingPages = run.ListingPages,
        WebsiteCount = run.WebsiteCount ?? 0,
        Found = seen.Count(sermon => sermon.SeenInRun == run.Number),
        Copied = seen.Count(sermon => sermon is { State: ImportedSermon.Imported, AlreadyThere: false } && sermon.ImportedInRun == run.Number),
        Skipped = seen.Count(sermon => sermon.SeenInRun == run.Number && sermon.State == ImportedSermon.Imported
            && (sermon.AlreadyThere || sermon.ImportedInRun != run.Number)),
        Failed = seen.Count(sermon => sermon.State == ImportedSermon.Failed),
        Waiting = seen.Count(sermon => sermon.SeenInRun == run.Number && sermon.State == ImportedSermon.Waiting),
        Current = seen.FirstOrDefault(sermon => sermon.WebsitePath == run.CurrentPath) is { } current ? Item(current) : null,
        Failures = seen.Where(sermon => sermon.State == ImportedSermon.Failed).OrderByDescending(sermon => sermon.WebsitePath).Take(FailuresShown).Select(Item).ToList(),
    };

    private static ImportItem Item(ImportedSermon sermon) => new()
    {
        WebsitePath = sermon.WebsitePath,
        Title = sermon.Title,
        PreachedOn = sermon.PreachedOn?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        Problem = sermon.Problem,
        Bytes = sermon.Bytes,
        CopiedBytes = sermon.CopiedBytes,
    };

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        db.FeedChanges.Add(FeedChangeRecord.Marker(SermonsImportFeeds.Progress, ImportRuns.RecordId, Now));
        await db.SaveChangesAsync(cancellationToken);
    }

    private DateTime Now => time.GetUtcNow().UtcDateTime;
}
