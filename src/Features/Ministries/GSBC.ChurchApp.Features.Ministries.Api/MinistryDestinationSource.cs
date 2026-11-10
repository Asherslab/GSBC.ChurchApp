using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryDestinationSource(MinistriesDbContext db) : IDestinationSource
{
    public DestinationKind Kind => MinistriesDestinations.Ministry;

    public async Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        List<MinistryRecord> found = await db.Ministries.AsNoTracking()
            .Where(ministry => text.Length == 0 || EF.Functions.ILike(ministry.Name, "%" + text.Replace("%", "").Replace("_", "") + "%"))
            .OrderBy(ministry => ministry.Name).Take(take).ToListAsync(cancellationToken);
        return found.Select(Place).ToList();
    }

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid ministryId) && await db.Ministries.AsNoTracking().SingleOrDefaultAsync(ministry => ministry.Id == ministryId, cancellationToken) is { } found
            ? Place(found)
            : null;

    public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    private static FoundPlace Place(MinistryRecord ministry) =>
        new(ministry.Id.ToString(), ministry.Name, "Ministries", Audience.Everyone);
}
