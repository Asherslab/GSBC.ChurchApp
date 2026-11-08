using GSBC.ChurchApp.Features.Sermons.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class Speakers(SermonsDbContext db, Artwork artwork, TimeProvider time, ILogger<Speakers> logger)
{
    private readonly Dictionary<string, SpeakerRecord> _resolved = new(StringComparer.Ordinal);

    public static string AliasOf(string name) => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    public async Task<SpeakerRecord> ResolveAsync(string name, CancellationToken cancellationToken)
    {
        string alias = AliasOf(name);
        if (_resolved.TryGetValue(alias, out SpeakerRecord? cached))
            return cached;

        SpeakerRecord? speaker = await db.SpeakerAliases.Where(known => known.Alias == alias)
            .Join(db.Speakers, known => known.SpeakerId, record => record.Id, (_, record) => record)
            .SingleOrDefaultAsync(cancellationToken);
        if (speaker is null)
        {
            Guid lasting = SourceIds.Speaker(alias);
            Guid id = await db.Speakers.AnyAsync(known => known.Id == lasting, cancellationToken) || db.Speakers.Local.Any(known => known.Id == lasting) ? Guid.CreateVersion7() : lasting;
            speaker = new SpeakerRecord { Id = id, Name = name.Trim(), CreatedAt = time.GetUtcNow().UtcDateTime };
            db.Speakers.Add(speaker);
            db.SpeakerAliases.Add(new SpeakerAlias { Alias = alias, SpeakerId = speaker.Id });
        }
        _resolved[alias] = speaker;
        return speaker;
    }

    public static Dictionary<string, string> ByAlias(IReadOnlyDictionary<string, string> byName)
    {
        Dictionary<string, string> byAlias = new(StringComparer.Ordinal);
        foreach ((string name, string url) in byName)
            byAlias.TryAdd(AliasOf(name), url);
        return byAlias;
    }

    public static IReadOnlyDictionary<string, string> Preferring(IReadOnlyDictionary<string, string> better, IReadOnlyDictionary<string, string> fallback)
    {
        Dictionary<string, string> byAlias = ByAlias(fallback);
        foreach ((string name, string url) in better)
            byAlias[AliasOf(name)] = url;
        return byAlias;
    }

    public async Task<IReadOnlyList<string>> TakePhotosAsync(IReadOnlyDictionary<string, string> photosByAlias, bool onlyWithout, CancellationToken cancellationToken)
    {
        List<string> unmatched = [];
        foreach ((string alias, string url) in photosByAlias)
        {
            Guid? id = await db.SpeakerAliases.Where(known => known.Alias == alias).Select(known => (Guid?)known.SpeakerId).SingleOrDefaultAsync(cancellationToken);
            if (id is null || await db.Speakers.FindAsync([id.Value], cancellationToken) is not { } speaker)
            {
                unmatched.Add(alias);
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
