using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonDeletion(
    SermonsDbContext db, SermonPublisher publisher, SourceIdMove sourceIds, IMediaReferences media, IRoleAssignments roles, IBlobStore blobs,
    PodcastFeedWriter podcast, TimeProvider time)
{
    public async Task<SeriesDeletion> SeriesDeletionAsync(Guid id, CancellationToken cancellationToken)
    {
        SeriesRecord series = await db.Series.AsNoTracking().SingleOrDefaultAsync(record => record.Id == id, cancellationToken) ?? throw NotFound("That series");
        List<SermonRecord> sermons = await db.Sermons.AsNoTracking().Where(sermon => !sermon.Hidden).ToListAsync(cancellationToken);
        return new SeriesDeletion
        {
            Sermons = sermons.Count(sermon => sermon.EffectiveSeriesId == series.Id),
            Followers = await roles.HoldingCountAsync(SermonsPermissions.SeriesFollowerRole, series.Id.ToString(), cancellationToken),
        };
    }

    public async Task DeleteSpeakerAsync(Guid id, Guid? moveTo, CancellationToken cancellationToken)
    {
        SpeakerRecord speaker = await db.Speakers.FindAsync([id], cancellationToken) ?? throw NotFound("That speaker");
        if (moveTo is { } to && (to == id || !await db.Speakers.AnyAsync(other => other.Id == to, cancellationToken)))
            throw Invalid("Choose another speaker from the list to move the sermons to.");

        DateTime now = time.GetUtcNow().UtcDateTime;
        await using (IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            foreach (SermonRecord sermon in await db.Sermons.Where(sermon => sermon.SourceSpeakerId == id || sermon.SpeakerId == id).ToListAsync(cancellationToken))
                MoveSpeaker(sermon, id, moveTo);
            db.Speakers.Remove(speaker);
            Unpublish(SermonsFeeds.Speakers, id, speaker.PublishedHash, now);
            await ForwardAsync(id, moveTo, FormerId.Speaker, now, cancellationToken);
            publisher.MarkAdmin(id);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        if (moveTo is null)
        {
            await media.UseAsync(Artwork.SpeakerOwner(id), [], cancellationToken);
            await media.UseAsync(Artwork.ChosenPhotoOwner(id), [], cancellationToken);
        }
        await FinishAsync(cancellationToken);
    }

    public async Task DeleteSeriesAsync(Guid id, Guid? moveTo, CancellationToken cancellationToken)
    {
        SeriesRecord series = await db.Series.FindAsync([id], cancellationToken) ?? throw NotFound("That series");
        if (moveTo is { } to && (to == id || !await db.Series.AnyAsync(other => other.Id == to, cancellationToken)))
            throw Invalid("Choose another series from the list to move the sermons to.");

        DateTime now = time.GetUtcNow().UtcDateTime;
        await using (IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            foreach (SermonRecord sermon in await db.Sermons.Where(sermon => sermon.SourceSeriesId == id || sermon.SeriesId == id).ToListAsync(cancellationToken))
                MoveSeries(sermon, id, moveTo);
            db.Series.Remove(series);
            Unpublish(SermonsFeeds.Series, id, series.PublishedHash, now);
            await ForwardAsync(id, moveTo, FormerId.Series, now, cancellationToken);
            publisher.MarkAdmin(id);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        if (moveTo is null)
        {
            await media.UseAsync(Artwork.SeriesOwner(id), [], cancellationToken);
            await media.UseAsync(Artwork.ChosenArtworkOwner(id), [], cancellationToken);
            await roles.ForgetScopeAsync(ScopeKind.SermonSeries, [id.ToString()], cancellationToken);
        }
        await FinishAsync(cancellationToken);
    }

    public async Task DeleteSermonAsync(Guid id, CancellationToken cancellationToken)
    {
        SermonRecord sermon = await db.Sermons.FindAsync([id], cancellationToken) ?? throw NotFound("That sermon");
        if (sermon.Origin != SermonSources.Uploads)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "A sermon from the website can only be hidden: the website would bring it back."));
        if (!sermon.Hidden)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Hide it from the app first, then delete it."));

        DateTime now = time.GetUtcNow().UtcDateTime;
        await using (IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            foreach (ProgressRecord progress in await db.Progress.Where(progress => progress.SermonId == id).ToListAsync(cancellationToken))
            {
                db.Progress.Remove(progress);
                db.FeedChanges.Add(new FeedChangeRecord
                {
                    Feed = SermonsFeeds.Progress, RecordId = $"{progress.UserId:N}-{id:N}", ScopeId = progress.UserId.ToString(), Version = now.Ticks, Deleted = true,
                });
            }
            db.Sermons.Remove(sermon);
            Unpublish(SermonsFeeds.Catalogue, id, sermon.PublishedHash, now);
            await db.FormerIds.Where(former => former.CurrentId == id).ExecuteDeleteAsync(cancellationToken);
            publisher.MarkAdmin(id);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        await media.UseAsync(SermonUploads.ArtworkOwner(id), [], cancellationToken);
        await media.UseAsync(Artwork.SermonOwner(id), [], cancellationToken);
        await media.UseAsync(AudioCopier.AudioOwner(id), [], cancellationToken);
        if (sermon.AudioMedia is null && UploadedAudioKey(sermon.AudioUrl) is { } key && blobs.PublicUrlOf(key).ToString() == sermon.AudioUrl)
            await blobs.DeleteAsync(BlobBucket.Public, key, cancellationToken);
        await FinishAsync(cancellationToken);
    }

    internal static void MoveSpeaker(SermonRecord sermon, Guid from, Guid? to)
    {
        if (sermon.SourceSpeakerId == from)
            sermon.SourceSpeakerId = to;
        if (sermon.SpeakerCorrected && sermon.SpeakerId == from)
            sermon.SpeakerId = to;
        if (sermon is { SpeakerCorrected: true, SpeakerId: { } shown } && shown == sermon.SourceSpeakerId)
            (sermon.SpeakerCorrected, sermon.SpeakerId) = (false, null);
    }

    internal static void MoveSeries(SermonRecord sermon, Guid from, Guid? to)
    {
        if (sermon.SourceSeriesId == from)
            sermon.SourceSeriesId = to;
        if (sermon.SeriesCorrected && sermon.SeriesId == from)
            sermon.SeriesId = to;
        if (sermon is { SeriesCorrected: true, SeriesId: { } shown, Origin: SermonSources.Website } && shown == sermon.SourceSeriesId)
            (sermon.SeriesCorrected, sermon.SeriesId) = (false, null);
    }

    internal static string? UploadedAudioKey(string audioUrl) =>
        audioUrl.LastIndexOf("sermons/audio/", StringComparison.Ordinal) is var at and >= 0 && SermonUploads.AudioKey().IsMatch(audioUrl[at..]) ? audioUrl[at..] : null;

    private async Task ForwardAsync(Guid id, Guid? moveTo, string kind, DateTime now, CancellationToken cancellationToken)
    {
        if (moveTo is { } to)
            await SourceIdMove.RememberAsync(db, id, to, kind, now, settled: false, cancellationToken);
        else
            await db.FormerIds.Where(former => former.CurrentId == id).ExecuteDeleteAsync(cancellationToken);
    }

    private void Unpublish(string feed, Guid id, string? publishedHash, DateTime now)
    {
        if (publishedHash is not null)
            db.FeedChanges.Add(new FeedChangeRecord { Feed = feed, RecordId = id.ToString(), Version = now.Ticks, Deleted = true });
    }

    private async Task FinishAsync(CancellationToken cancellationToken)
    {
        await sourceIds.SettleAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        if ((await SourceStates.GetAsync(db, cancellationToken)).Source == SermonSources.Uploads)
            await podcast.WriteAsync(cancellationToken);
    }

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException NotFound(string what) => new(new Status(StatusCode.NotFound, $"{what} has gone."));
}
