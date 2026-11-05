using System.Text.RegularExpressions;
using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Api.Website;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed partial class SermonUploads(
    SermonsDbContext db, IBlobStore blobs, IMediaReferences media, Speakers speakers, SermonPublisher publisher, PodcastFeedWriter podcast,
    SermonAnnouncer announcer, TimeProvider time)
{
    public static readonly TimeSpan UploadWindow = TimeSpan.FromHours(2);
    private const int HeadBytes = 64 * 1024;

    public async Task<AudioUploadTicket> BeginAsync(AudioUploadRequest request, CancellationToken cancellationToken)
    {
        await RequireUploadsAsync(cancellationToken);
        if (!SermonAudio.TryGetExtension(request.ContentType, out string extension))
            throw Invalid("Choose an mp3 or m4a file.");
        if (request.Bytes is <= 0 or > SermonAudio.LargestBytes)
            throw Invalid($"Sermon audio can be up to {SermonAudio.LargestBytes / 1024 / 1024} MB.");
        string key = $"sermons/audio/{Guid.CreateVersion7():N}.{extension}";
        Uri url = await blobs.PresignUploadAsync(BlobBucket.Public, key, SermonAudio.TypeOf(extension), UploadWindow);
        return new AudioUploadTicket { Key = key, Url = url.ToString(), ContentType = SermonAudio.TypeOf(extension), ExpiresAt = time.GetUtcNow().UtcDateTime + UploadWindow };
    }

    public async Task<SermonRecord> AddAsync(SermonUpload request, CancellationToken cancellationToken)
    {
        await RequireUploadsAsync(cancellationToken);
        if (!AudioKey().IsMatch(request.AudioKey))
            throw Invalid("Upload the audio first.");
        if (await db.Sermons.AnyAsync(sermon => sermon.AudioUrl == blobs.PublicUrlOf(request.AudioKey).ToString(), cancellationToken))
            throw Invalid("That audio is already a sermon.");
        if (await blobs.FindAsync(BlobBucket.Public, request.AudioKey, cancellationToken) is not { Bytes: > 0 } audio)
            throw Invalid("The audio hasn't finished uploading. Try again in a moment.");
        if (request.Title.Trim() is not { Length: > 0 and <= SermonLibraryService.TitleLimit } title)
            throw Invalid($"Give it a title of up to {SermonLibraryService.TitleLimit} characters.");
        if (request.PreachedOn == default)
            throw Invalid("Choose the day it was preached.");
        string? youTube = string.IsNullOrWhiteSpace(request.YouTube) ? null
            : YouTubeIds.Find(request.YouTube) ?? throw Invalid("That isn't a YouTube link. Paste the video's address.");

        DateTime now = time.GetUtcNow().UtcDateTime;
        SpeakerRecord speaker = await SpeakerAsync(request, cancellationToken);
        Guid? seriesId = await SeriesAsync(request, now, cancellationToken);
        string extension = request.AudioKey[(request.AudioKey.LastIndexOf('.') + 1)..];
        MediaRef? artwork = request.Artwork is null ? null : (await media.VerifyAsync([request.Artwork], cancellationToken)).FirstOrDefault();

        SermonRecord sermon = new()
        {
            Id = Guid.CreateVersion7(),
            Origin = SermonSources.Uploads,
            PreachedAt = ChurchTime.ToUtc(DateOnly.FromDateTime(request.PreachedOn).ToDateTime(TimeOnly.MinValue)),
            AddedAt = now,
            UpdatedAt = now,
            SourceTitle = title,
            SourceSpeaker = speaker.Name,
            SourceSpeakerId = speaker.Id,
            SourceSeriesId = seriesId,
            SourceService = request.Service.Trim(),
            SourceYouTubeId = youTube,
            Summary = request.Summary.Trim(),
            AudioUrl = blobs.PublicUrlOf(request.AudioKey).ToString(),
            AudioType = SermonAudio.TypeOf(extension),
            AudioBytes = audio.Bytes,
            DurationSeconds = await DurationAsync(request, audio.Bytes, extension, cancellationToken),
            Artwork = artwork,
        };
        db.Sermons.Add(sermon);
        if (artwork is not null)
            await media.UseAsync($"sermons/sermon/{sermon.Id}", [artwork], cancellationToken);
        publisher.MarkAdmin(sermon.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(cancellationToken);
        await podcast.WriteAsync(cancellationToken);
        await announcer.AnnounceAsync(cancellationToken);
        return sermon;
    }

    private async Task<SpeakerRecord> SpeakerAsync(SermonUpload request, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(request.SpeakerId, out Guid id))
            return await db.Speakers.FindAsync([id], cancellationToken) ?? throw Invalid("Choose a speaker from the list.");
        if (request.NewSpeaker?.Trim() is { Length: > 0 and <= SermonLibraryService.NameLimit } name)
            return await speakers.ResolveAsync(name, cancellationToken);
        throw Invalid("Choose who preached it.");
    }

    private async Task<Guid?> SeriesAsync(SermonUpload request, DateTime now, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(request.SeriesId, out Guid id))
            return await db.Series.AnyAsync(series => series.Id == id, cancellationToken) ? id : throw Invalid("Choose a series from the list.");
        if (request.NewSeries?.Trim() is not { Length: > 0 } name)
            return null;
        if (name.Length > SermonLibraryService.NameLimit)
            throw Invalid($"Give the series a name of up to {SermonLibraryService.NameLimit} characters.");
        if (await db.Series.FirstOrDefaultAsync(series => series.Name.ToLower() == name.ToLower(), cancellationToken) is { } existing)
            return existing.Id;
        SeriesRecord created = new() { Id = Guid.CreateVersion7(), Name = name, CreatedAt = now };
        db.Series.Add(created);
        return created.Id;
    }

    private async Task<int> DurationAsync(SermonUpload request, long bytes, string extension, CancellationToken cancellationToken)
    {
        if (extension != "mp3")
            return Math.Max(0, request.DurationSeconds);
        await using Stream audio = await blobs.OpenAsync(BlobBucket.Public, request.AudioKey, cancellationToken);
        byte[] head = new byte[HeadBytes];
        int read = await audio.ReadAtLeastAsync(head, HeadBytes, throwOnEndOfStream: false, cancellationToken);
        int tag = Mp3Duration.TagBytes(head.AsSpan(0, read));
        if (tag > 0 && tag < read)
            return Mp3Duration.Seconds(head.AsSpan(tag, read - tag), bytes - tag) ?? Math.Max(0, request.DurationSeconds);
        return tag == 0 ? Mp3Duration.Seconds(head.AsSpan(0, read), bytes) ?? Math.Max(0, request.DurationSeconds) : Math.Max(0, request.DurationSeconds);
    }

    private async Task RequireUploadsAsync(CancellationToken cancellationToken)
    {
        if ((await SourceStates.GetAsync(db, cancellationToken)).Source != SermonSources.Uploads)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Sermons come from the website at the moment. Switch Where new sermons come from to uploads first."));
    }

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    [GeneratedRegex("^sermons/audio/[0-9a-f]{32}\\.(mp3|m4a)$")]
    private static partial Regex AudioKey();
}
