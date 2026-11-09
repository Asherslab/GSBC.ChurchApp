using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SourceIdMove(
    SermonsDbContext db, IMediaReferences media, IRoleAssignments roles, SermonPublisher publisher, TimeProvider time, ILogger<SourceIdMove> logger)
{
    private sealed record Move(string Kind, Guid From, Guid To);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        List<Move> moves = await PlanAsync(cancellationToken);
        if (moves.Count > 0)
        {
            await MoveAsync(moves, cancellationToken);
            logger.LogInformation("Gave {Count} sermons, series and speakers from the website their lasting ids", moves.Count);
            await publisher.PublishAsync(cancellationToken);
        }
        await SettleAsync(cancellationToken);
    }

    private async Task<List<Move>> PlanAsync(CancellationToken cancellationToken)
    {
        List<Move> moves = [];
        foreach (var sermon in await db.Sermons.AsNoTracking().Where(sermon => sermon.Origin == WebsiteReader.Origin && sermon.ExternalId != null)
                     .Select(sermon => new { sermon.Id, sermon.ExternalId }).ToListAsync(cancellationToken))
            moves.Add(new Move(FormerId.Sermon, sermon.Id, SourceIds.Sermon(sermon.ExternalId!)));

        foreach (var series in await db.Series.AsNoTracking().Where(series => series.Slug != null).Select(series => new { series.Id, series.Slug }).ToListAsync(cancellationToken))
            moves.Add(new Move(FormerId.Series, series.Id, SourceIds.Series(series.Slug!)));

        var sourceNames = await db.Sermons.AsNoTracking().Where(sermon => sermon.Origin == WebsiteReader.Origin && sermon.SourceSpeakerId != null)
            .Select(sermon => new { SpeakerId = sermon.SourceSpeakerId!.Value, sermon.SourceSpeaker }).ToListAsync(cancellationToken);
        foreach (SpeakerRecord speaker in await db.Speakers.AsNoTracking().ToListAsync(cancellationToken))
        {
            List<string> names = sourceNames.Where(name => name.SpeakerId == speaker.Id).Select(name => Speakers.KeyOf(name.SourceSpeaker)).ToList();
            if (names.Count == 0)
                continue;
            string key = names.Contains(Speakers.KeyOf(speaker.Name))
                ? Speakers.KeyOf(speaker.Name)
                : names.GroupBy(name => name).OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).First().Key;
            moves.Add(new Move(FormerId.Speaker, speaker.Id, SourceIds.Speaker(key)));
        }

        moves.RemoveAll(move => move.From == move.To);
        HashSet<Guid> taken = [.. await db.Sermons.Select(sermon => sermon.Id).ToListAsync(cancellationToken),
            .. await db.Series.Select(series => series.Id).ToListAsync(cancellationToken),
            .. await db.Speakers.Select(speaker => speaker.Id).ToListAsync(cancellationToken)];
        List<Move> clashing = moves.Where(move => taken.Contains(move.To) || moves.Count(other => other.To == move.To) > 1).ToList();
        foreach (Move clash in clashing)
            logger.LogWarning("Keeping {Kind} {From} where it is: its lasting id {To} is already used", clash.Kind, clash.From, clash.To);
        return moves.Except(clashing).ToList();
    }

    private async Task MoveAsync(IReadOnlyList<Move> moves, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (Move move in moves)
        {
            await MoveRecordAsync(move, now, cancellationToken);
            switch (move.Kind)
            {
                case FormerId.Sermon:
                    await MoveProgressAsync(move, now, cancellationToken);
                    break;
                case FormerId.Series:
                    await db.Database.ExecuteSqlAsync($"UPDATE sermons.sermons SET source_series_id = {move.To} WHERE source_series_id = {move.From}", cancellationToken);
                    await db.Database.ExecuteSqlAsync($"UPDATE sermons.sermons SET series_id = {move.To} WHERE series_id = {move.From}", cancellationToken);
                    break;
                default:
                    await db.Database.ExecuteSqlAsync($"UPDATE sermons.sermons SET source_speaker_id = {move.To} WHERE source_speaker_id = {move.From}", cancellationToken);
                    await db.Database.ExecuteSqlAsync($"UPDATE sermons.sermons SET speaker_id = {move.To} WHERE speaker_id = {move.From}", cancellationToken);
                    break;
            }
            await RememberAsync(db, move.From, move.To, move.Kind, now, settled: false, cancellationToken);
            db.FeedChanges.Add(FeedChangeRecord.Marker(SermonsFeeds.Admin, move.To.ToString(), now));
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public static async Task<Guid> CurrentAsync(SermonsDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.FormerIds.AsNoTracking().Where(former => former.Id == id).Select(former => (Guid?)former.CurrentId).SingleOrDefaultAsync(cancellationToken) ?? id;

    public static async Task<Dictionary<Guid, Guid>> CurrentOfAsync(SermonsDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.FormerIds.AsNoTracking().Where(former => ids.Contains(former.Id)).ToDictionaryAsync(former => former.Id, former => former.CurrentId, cancellationToken);

    public static async Task RememberAsync(SermonsDbContext db, Guid from, Guid to, string kind, DateTime now, bool settled, CancellationToken cancellationToken)
    {
        await db.FormerIds.Where(former => former.CurrentId == from).ExecuteUpdateAsync(set => set.SetProperty(former => former.CurrentId, to), cancellationToken);
        db.FormerIds.Add(new FormerId { Id = from, CurrentId = to, Kind = kind, MovedAt = now, Settled = settled });
    }

    private async Task MoveProgressAsync(Move move, DateTime now, CancellationToken cancellationToken)
    {
        List<ProgressRecord> heard = await db.Progress.AsNoTracking().Where(progress => progress.SermonId == move.From).ToListAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"UPDATE sermons.progress SET sermon_id = {move.To} WHERE sermon_id = {move.From}", cancellationToken);
        foreach (ProgressRecord progress in heard)
        {
            db.FeedChanges.Add(new FeedChangeRecord
            {
                Feed = SermonsFeeds.Progress, RecordId = $"{progress.UserId:N}-{move.From:N}", ScopeId = progress.UserId.ToString(), Version = now.Ticks, Deleted = true,
            });
            progress.SermonId = move.To;
            db.FeedChanges.Add(SermonsService.Row(progress));
        }
    }

    private async Task MoveRecordAsync(Move move, DateTime now, CancellationToken cancellationToken)
    {
        (bool published, int moved) = move.Kind switch
        {
            FormerId.Sermon => (await db.Sermons.AnyAsync(sermon => sermon.Id == move.From && sermon.PublishedHash != null, cancellationToken),
                await db.Database.ExecuteSqlAsync($"UPDATE sermons.sermons SET id = {move.To}, published_hash = NULL WHERE id = {move.From}", cancellationToken)),
            FormerId.Series => (await db.Series.AnyAsync(series => series.Id == move.From && series.PublishedHash != null, cancellationToken),
                await db.Database.ExecuteSqlAsync($"UPDATE sermons.series SET id = {move.To}, published_hash = NULL WHERE id = {move.From}", cancellationToken)),
            _ => (await db.Speakers.AnyAsync(speaker => speaker.Id == move.From && speaker.PublishedHash != null, cancellationToken),
                await db.Database.ExecuteSqlAsync($"UPDATE sermons.speakers SET id = {move.To}, published_hash = NULL WHERE id = {move.From}", cancellationToken)),
        };
        if (moved != 1)
            throw new InvalidOperationException($"Moving {move.Kind} {move.From} changed {moved} rows.");
        if (published)
            db.FeedChanges.Add(new FeedChangeRecord { Feed = FeedOf(move.Kind), RecordId = move.From.ToString(), Version = now.Ticks, Deleted = true });
    }

    private static string FeedOf(string kind) => kind switch
    {
        FormerId.Sermon => SermonsFeeds.Catalogue,
        FormerId.Series => SermonsFeeds.Series,
        _ => SermonsFeeds.Speakers,
    };

    private async Task SettleAsync(CancellationToken cancellationToken)
    {
        foreach (FormerId former in await db.FormerIds.Where(former => !former.Settled).ToListAsync(cancellationToken))
        {
            switch (former.Kind)
            {
                case FormerId.Sermon:
                    SermonRecord? sermon = await db.Sermons.AsNoTracking().SingleOrDefaultAsync(record => record.Id == former.CurrentId, cancellationToken);
                    await MoveUsesAsync(Artwork.SermonOwner(former.Id), Artwork.SermonOwner(former.CurrentId), sermon?.Artwork, cancellationToken);
                    await MoveUsesAsync(AudioCopier.AudioOwner(former.Id), AudioCopier.AudioOwner(former.CurrentId), sermon?.AudioMedia, cancellationToken);
                    break;
                case FormerId.Series:
                    SeriesRecord? series = await db.Series.AsNoTracking().SingleOrDefaultAsync(record => record.Id == former.CurrentId, cancellationToken);
                    await MoveUsesAsync(Artwork.SeriesOwner(former.Id), Artwork.SeriesOwner(former.CurrentId), series?.Artwork, cancellationToken);
                    await MoveUsesAsync(Artwork.ChosenArtworkOwner(former.Id), Artwork.ChosenArtworkOwner(former.CurrentId), series?.ChosenArtwork, cancellationToken);
                    await roles.MoveScopeAsync(ScopeKind.SermonSeries, former.Id.ToString(), former.CurrentId.ToString(), cancellationToken);
                    break;
                default:
                    SpeakerRecord? speaker = await db.Speakers.AsNoTracking().SingleOrDefaultAsync(record => record.Id == former.CurrentId, cancellationToken);
                    await MoveUsesAsync(Artwork.SpeakerOwner(former.Id), Artwork.SpeakerOwner(former.CurrentId), speaker?.Photo, cancellationToken);
                    await MoveUsesAsync(Artwork.ChosenPhotoOwner(former.Id), Artwork.ChosenPhotoOwner(former.CurrentId), speaker?.ChosenPhoto, cancellationToken);
                    break;
            }
            former.Settled = true;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task MoveUsesAsync(string from, string to, MediaRef? held, CancellationToken cancellationToken)
    {
        if (held is not null)
            await media.UseAsync(to, [held], cancellationToken);
        await media.UseAsync(from, [], cancellationToken);
    }
}
