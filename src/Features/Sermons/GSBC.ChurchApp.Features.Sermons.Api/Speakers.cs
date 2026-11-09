using GSBC.ChurchApp.Features.Sermons.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class Speakers(SermonsDbContext db, TimeProvider time)
{
    public static string KeyOf(string name) => string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private async Task<SpeakerRecord?> NamedAsync(string name, CancellationToken cancellationToken)
    {
        string key = KeyOf(name);
        return db.Speakers.Local.FirstOrDefault(speaker => KeyOf(speaker.Name) == key)
            ?? (await db.Speakers.ToListAsync(cancellationToken)).FirstOrDefault(speaker => KeyOf(speaker.Name) == key);
    }

    public async Task<SpeakerRecord> ResolveAsync(string name, CancellationToken cancellationToken, Guid? newId = null)
    {
        if (await NamedAsync(name, cancellationToken) is { } known)
            return known;
        SpeakerRecord speaker = new() { Id = newId ?? Guid.CreateVersion7(), Name = string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)), CreatedAt = time.GetUtcNow().UtcDateTime };
        db.Speakers.Add(speaker);
        return speaker;
    }
}
