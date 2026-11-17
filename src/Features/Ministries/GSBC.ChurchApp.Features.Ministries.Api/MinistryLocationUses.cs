using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Platform.Api.Locations;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryLocationUses(MinistriesDbContext db) : ILocationUses
{
    public async Task<IReadOnlyCollection<string>> MapKeysAsync(CancellationToken cancellationToken) =>
        [.. (await db.Ministries.AsNoTracking().Where(ministry => ministry.Where != null).Select(ministry => ministry.Where).ToListAsync(cancellationToken))
            .Select(where => where?.Map?.Key ?? "").Where(key => key.Length > 0).Distinct()];
}
