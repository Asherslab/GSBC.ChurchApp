using GSBC.ChurchApp.Features.Sermons.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class Speakers(SermonsDbContext db, Artwork artwork, TimeProvider time, ILogger<Speakers> logger)
{
    private Dictionary<string, Guid>? _byName;

    public static string KeyOf(string name) => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    public async Task<SpeakerRecord> ResolveAsync(string name, CancellationToken cancellationToken)
    {
        Dictionary<string, Guid> byName = await ByNameAsync(cancellationToken);
        string key = KeyOf(name);
        if (byName.TryGetValue(key, out Guid known) && await db.Speakers.FindAsync([known], cancellationToken) is { } speaker)
            return speaker;

        Guid lasting = SourceIds.Speaker(key);
        Guid id = await db.Speakers.AnyAsync(other => other.Id == lasting, cancellationToken) || db.Speakers.Local.Any(other => other.Id == lasting) ? Guid.CreateVersion7() : lasting;
        speaker = new SpeakerRecord { Id = id, Name = name.Trim(), CreatedAt = time.GetUtcNow().UtcDateTime };
        db.Speakers.Add(speaker);
        byName[key] = speaker.Id;
        return speaker;
    }

    private async Task<Dictionary<string, Guid>> ByNameAsync(CancellationToken cancellationToken)
    {
        if (_byName is not null)
            return _byName;
        List<SpeakerRecord> speakers = await db.Speakers.AsNoTracking().ToListAsync(cancellationToken);
        HashSet<Guid> exists = [.. speakers.Select(speaker => speaker.Id)];
        var credited = await db.Sermons.AsNoTracking().Where(sermon => sermon.SourceSpeakerId != null)
            .OrderBy(sermon => sermon.PreachedAt).Select(sermon => new { sermon.SourceSpeaker, Source = sermon.SourceSpeakerId, Shown = sermon.SpeakerCorrected ? sermon.SpeakerId : sermon.SourceSpeakerId })
            .ToListAsync(cancellationToken);
        HashSet<Guid?> withSermons = [.. credited.Select(sermon => sermon.Shown)];
        Dictionary<string, Guid> byName = new(StringComparer.Ordinal);
        foreach (var sermon in credited.Where(sermon => sermon.SourceSpeaker.Length > 0))
            if ((withSermons.Contains(sermon.Source) ? sermon.Source : sermon.Shown) is { } id && exists.Contains(id))
                byName[KeyOf(sermon.SourceSpeaker)] = id;
        foreach (SpeakerRecord speaker in speakers)
            byName.TryAdd(KeyOf(speaker.Name), speaker.Id);
        return _byName = byName;
    }

    public static Dictionary<string, string> ByKey(IReadOnlyDictionary<string, string> byName)
    {
        Dictionary<string, string> byKey = new(StringComparer.Ordinal);
        foreach ((string name, string url) in byName)
            byKey.TryAdd(KeyOf(name), url);
        return byKey;
    }

    public static IReadOnlyDictionary<string, string> Preferring(IReadOnlyDictionary<string, string> better, IReadOnlyDictionary<string, string> fallback)
    {
        Dictionary<string, string> byKey = ByKey(fallback);
        foreach ((string name, string url) in better)
            byKey[KeyOf(name)] = url;
        return byKey;
    }

    public async Task<IReadOnlyList<string>> TakePhotosAsync(IReadOnlyDictionary<string, string> photosByKey, bool onlyWithout, CancellationToken cancellationToken)
    {
        Dictionary<string, Guid> byName = await ByNameAsync(cancellationToken);
        List<string> unmatched = [];
        foreach ((string key, string url) in photosByKey)
        {
            if (!byName.TryGetValue(key, out Guid id) || await db.Speakers.FindAsync([id], cancellationToken) is not { } speaker)
            {
                unmatched.Add(key);
                continue;
            }
            if (speaker.PhotoSourceUrl == url || (onlyWithout && speaker.PhotoSourceUrl is not null))
                continue;
            speaker.PhotoSourceUrl = url;
            speaker.PhotoHash = null;
            speaker.Photo = null;
        }
        return unmatched;
    }

    public async Task FillInPhotosAsync(CancellationToken cancellationToken)
    {
        foreach (SpeakerRecord speaker in await db.Speakers.Where(speaker => speaker.PhotoHash == null && speaker.PhotoSourceUrl != null).ToListAsync(cancellationToken))
        {
            try
            {
                ArtworkPicture photo = await artwork.PictureAsync(speaker.PhotoSourceUrl!, speaker.Name, Artwork.SpeakerOwner(speaker.Id), cancellationToken);
                (speaker.PhotoHash, speaker.Photo) = (photo.Hash, photo.Picture);
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidDataException)
            {
                logger.LogWarning(exception, "Couldn't get {Speaker}'s photo; trying again next time", speaker.Name);
            }
        }
    }
}
