using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Sermons.Api;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Features.Sermons.Import.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Import.Api.Website;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api;

internal sealed class ImportProblem(string message) : Exception(message);

internal sealed class SermonImporter(
    SermonsDbContext sermons, ArchiveClient website, IBlobStore blobs, IMediaSystemFolders folders, IMediaReferences references,
    Speakers speakers, ServiceNames services, SermonPublisher publisher, TimeProvider time)
{
    private const int ChunkBytes = 256 * 1024;
    private const int AltTextLimit = 200;
    public async Task ImportAsync(ImportedSermon item, Dictionary<string, Guid> seriesBySlug, Func<Task> progressed, CancellationToken cancellationToken)
    {
        Guid id = LastingIds.Sermon(website.WebsiteUrl, item.WebsitePath);
        if (await sermons.Sermons.AnyAsync(sermon => sermon.Id == id, cancellationToken))
        {
            (item.SermonId, item.AlreadyThere) = (id, true);
            return;
        }

        WebsiteSermon page = SermonPages.Read(item.WebsitePath, await website.PageAsync(SermonPages.PathOf(item.WebsitePath), cancellationToken))
            ?? throw new ImportProblem("Its page on the website has no sermon audio, title or date to bring across.");
        (item.Title, item.PreachedOn, item.WebsiteId) = (page.Title, page.PreachedOn, page.WebsiteId);

        string file = Path.Combine(Path.GetTempPath(), $"gsbc-import-{id:N}");
        try
        {
            (string sha256, string contentType, string extension) = await DownloadAsync(page.AudioUrl, file, item, progressed, cancellationToken);
            MediaRef audio = await StoreAudioAsync(file, sha256, contentType, extension, page, cancellationToken);
            if (await sermons.Sermons.AsNoTracking().Where(sermon => sermon.AudioMedia != null).Select(sermon => new { sermon.Id, sermon.AudioMedia })
                    .ToListAsync(cancellationToken) is var held && held.FirstOrDefault(sermon => sermon.AudioMedia!.Id == audio.Id) is { } same)
            {
                (item.SermonId, item.AlreadyThere) = (same.Id, true);
                return;
            }

            await AddAsync(id, page, audio, contentType, await DurationAsync(file, extension, cancellationToken), seriesBySlug, cancellationToken);
            item.SermonId = id;
        }
        finally
        {
            File.Delete(file);
        }
    }

    private async Task AddAsync(Guid id, WebsiteSermon page, MediaRef audio, string contentType, int duration, Dictionary<string, Guid> seriesBySlug,
        CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        CreditedTitle? credited = page.Speaker is null ? GuestTitles.Split(page.Title) : null;
        string title = credited?.Title ?? page.Title;
        SpeakerRecord? speaker = await SpeakerAsync(page, credited, cancellationToken);
        string service = await services.NameAsync(ServiceLabels.FromPage(page.ServiceLabel, speaker?.Name ?? "", page.PreachedOn), cancellationToken);
        MediaRef? artwork = await PictureAsync(Artwork(page), title, SermonMedia.SermonOwner(id), cancellationToken);

        sermons.Sermons.Add(new SermonRecord
        {
            Id = id,
            PreachedAt = ChurchTime.ToUtc(page.PreachedOn.ToDateTime(TimeOnly.MinValue)),
            AddedAt = now,
            UpdatedAt = now,
            Title = title.Length <= SermonLibraryService.TitleLimit ? title : title[..SermonLibraryService.TitleLimit],
            SpeakerId = speaker?.Id,
            SeriesId = await SeriesAsync(page.Series, seriesBySlug, cancellationToken),
            Service = service,
            YouTubeId = page.YouTubeId,
            Passage = page.Passage.Length <= SermonLibraryService.PassageLimit ? page.Passage : page.Passage[..SermonLibraryService.PassageLimit],
            Summary = Description(page.Description, page.ServiceLabel, service),
            AudioUrl = blobs.PublicUrlOf(audio.Key).ToString(),
            AudioType = contentType,
            AudioBytes = audio.Bytes,
            DurationSeconds = duration,
            AudioMedia = audio,
            Artwork = artwork,
            AnnouncedAt = now,
            CheckedAt = now,
        });
        await sermons.SaveChangesAsync(cancellationToken);
        await references.UseAsync(SermonMedia.AudioOwner(id), [audio], cancellationToken);
        await publisher.PublishAsync(cancellationToken);
    }

    internal static string Description(string description, string serviceLabel, string service) =>
        Plain(description) is var plain && (plain == Plain(serviceLabel) || plain == Plain(service)) ? "" : description;

    private static string Plain(string text) => new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static IReadOnlyList<string> Artwork(WebsiteSermon page)
    {
        string? fileHost = Uri.TryCreate(page.AudioUrl, UriKind.Absolute, out Uri? audio) ? audio.Host : null;
        return page.ArtworkUrls.Select(url => TithelyArtwork.OriginalOf(url, fileHost) ?? url).Distinct().ToList();
    }

    private async Task<SpeakerRecord?> SpeakerAsync(WebsiteSermon page, CreditedTitle? credited, CancellationToken cancellationToken)
    {
        if ((page.Speaker ?? credited?.Speaker) is not { Length: > 0 } name)
            return null;
        SpeakerRecord speaker = await speakers.ResolveAsync(name, cancellationToken, LastingIds.Speaker(Speakers.KeyOf(name)));
        speaker.Honorific ??= credited?.Honorific;
        if (speaker.Photo is null && page.SpeakerPhotoUrl is { } photo)
            speaker.Photo = await PictureAsync([photo], speaker.Name, SermonMedia.SpeakerOwner(speaker.Id), cancellationToken);
        return speaker;
    }

    private async Task<Guid?> SeriesAsync(WebsiteSeriesLink? link, Dictionary<string, Guid> seriesBySlug, CancellationToken cancellationToken)
    {
        if (link is null)
            return null;
        if (seriesBySlug.TryGetValue(link.Slug, out Guid known) && await sermons.Series.AnyAsync(series => series.Id == known, cancellationToken))
            return known;
        WebsiteSeries series = SeriesPage.Read(link.Slug, link.Name, await website.PageAsync(SeriesPage.PathOf(link.Slug), cancellationToken));
        Guid lasting = LastingIds.Series(link.Slug);
        SeriesRecord? record = await sermons.Series.FindAsync([lasting], cancellationToken)
            ?? (await sermons.Series.ToListAsync(cancellationToken)).FirstOrDefault(existing => string.Equals(existing.Name.Trim(), series.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (record is null)
        {
            record = new SeriesRecord { Id = lasting, Name = series.Name, CreatedAt = time.GetUtcNow().UtcDateTime };
            sermons.Series.Add(record);
        }
        if (record.Description.Length == 0)
            record.Description = series.Description;
        if (record.Artwork is null && series.ArtworkUrl is { } artwork)
            record.Artwork = await PictureAsync([artwork], record.Name, SermonMedia.SeriesOwner(record.Id), cancellationToken);
        return seriesBySlug[link.Slug] = record.Id;
    }

    private async Task<MediaRef?> PictureAsync(IReadOnlyList<string> urls, string altText, string owner, CancellationToken cancellationToken)
    {
        (byte[] Bytes, KeptFile File)? largest = null;
        foreach (string url in urls)
            if (Picture(await website.BytesAsync(url, cancellationToken), url) is { } picture
                && (largest is null || (long)picture.File.Width * picture.File.Height > (long)largest.Value.File.Width * largest.Value.File.Height))
                largest = picture;
        if (largest is not { } chosen)
            return null;
        MediaRef kept = await folders.KeepAsync(SermonMedia.Pictures, chosen.File, cancellationToken);
        await references.UseAsync(owner, [kept], cancellationToken);
        kept.AltText = altText.Length <= AltTextLimit ? altText : altText[..AltTextLimit];
        return kept;
    }

    internal static (byte[] Bytes, KeptFile File)? Picture(byte[] bytes, string url)
    {
        using SKCodec? codec = SKCodec.Create(new SKMemoryStream(bytes));
        if (codec is null)
            return null;
        (string contentType, string extension)? format = codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Jpeg => ("image/jpeg", "jpg"),
            SKEncodedImageFormat.Png => ("image/png", "png"),
            SKEncodedImageFormat.Webp => ("image/webp", "webp"),
            SKEncodedImageFormat.Gif => ("image/gif", "gif"),
            _ => null,
        };
        if (format is not { } known)
            return null;
        bool turned = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        (int width, int height) = turned ? (codec.Info.Height, codec.Info.Width) : (codec.Info.Width, codec.Info.Height);
        return (bytes, new KeptFile(bytes, known.contentType, known.extension, FileNameOf(url, bytes, known.extension), width, height));
    }

    private static string FileNameOf(string url, byte[] bytes, string extension)
    {
        string named = Uri.TryCreate(url, UriKind.Absolute, out Uri? address) ? Uri.UnescapeDataString(address.Segments[^1]).Trim('/') : "";
        string stem = Path.GetFileNameWithoutExtension(named);
        return stem.Length is > 0 and <= 80 && stem.All(letter => char.IsLetterOrDigit(letter) || letter is '-' or '_' or '.' or ' ')
            ? $"{stem}.{extension}"
            : $"{Convert.ToHexStringLower(SHA256.HashData(bytes))[..12]}.{extension}";
    }

    private async Task<(string Sha256, string ContentType, string Extension)> DownloadAsync(string url, string file, ImportedSermon item, Func<Task> progressed,
        CancellationToken cancellationToken)
    {
        (Stream body, long? length, string? type) = await website.OpenAsync(url, cancellationToken);
        string extension = Path.GetExtension(new Uri(url).AbsolutePath).TrimStart('.').ToLowerInvariant() is "m4a" || type is "audio/mp4" or "audio/x-m4a" ? "m4a" : "mp3";
        (item.Bytes, item.CopiedBytes) = (length ?? 0, 0);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[ChunkBytes];
        DateTimeOffset told = time.GetUtcNow();
        await using (body)
        await using (FileStream local = File.Create(file))
        {
            int read;
            while ((read = await body.ReadAsync(buffer, cancellationToken)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                await local.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                item.CopiedBytes += read;
                if (time.GetUtcNow() - told >= TimeSpan.FromSeconds(1))
                {
                    told = time.GetUtcNow();
                    await progressed();
                }
            }
        }
        if (item.CopiedBytes == 0)
            throw new ImportProblem("Its audio file on the website is empty.");
        item.Bytes = item.CopiedBytes;
        return (Convert.ToHexStringLower(hash.GetHashAndReset()), SermonAudio.TypeOf(extension), extension);
    }

    private async Task<MediaRef> StoreAudioAsync(string file, string sha256, string contentType, string extension, WebsiteSermon page, CancellationToken cancellationToken)
    {
        string key = ContentAddress.KeyFor(sha256, extension);
        long bytes = new FileInfo(file).Length;
        bool putHere = false;
        if (await blobs.FindAsync(BlobBucket.Public, key, cancellationToken) is null)
        {
            await using FileStream upload = File.OpenRead(file);
            await blobs.PutAsync(BlobBucket.Public, key, upload, new BlobHeaders(contentType, ContentAddress.ImmutableCacheControl), cancellationToken);
            putHere = true;
        }
        string fileName = $"{page.PreachedOn:yyyy-MM-dd} {page.Title}.{extension}";
        MediaRef kept = await folders.KeepStoredAsync(SermonMedia.Audio, new StoredFile(key, sha256, bytes, contentType, fileName), cancellationToken);
        if (putHere && kept.Key != key)
            await blobs.DeleteAsync(BlobBucket.Public, key, cancellationToken);
        return kept;
    }

    private static async Task<int> DurationAsync(string file, string extension, CancellationToken cancellationToken)
    {
        if (extension != "mp3")
            return 0;
        await using FileStream audio = File.OpenRead(file);
        return await Mp3Duration.ReadAsync(audio, audio.Length, cancellationToken) ?? 0;
    }
}
