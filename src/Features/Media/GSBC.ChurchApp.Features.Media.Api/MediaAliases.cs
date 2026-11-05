using GSBC.ChurchApp.Features.Media.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api;

internal static class MediaAliases
{
    public static async Task<Guid> ResolveAsync(MediaDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Aliases.AsNoTracking().Where(alias => alias.Id == id).Select(alias => (Guid?)alias.MediaId).FirstOrDefaultAsync(cancellationToken) ?? id;

    public static async Task<Dictionary<Guid, Guid>> ResolveAsync(MediaDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        Dictionary<Guid, Guid> aliases = await db.Aliases.AsNoTracking().Where(alias => ids.Contains(alias.Id)).ToDictionaryAsync(alias => alias.Id, alias => alias.MediaId, cancellationToken);
        return ids.Distinct().ToDictionary(id => id, id => aliases.GetValueOrDefault(id, id));
    }
}
