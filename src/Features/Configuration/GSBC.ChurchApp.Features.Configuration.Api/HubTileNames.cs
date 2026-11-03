using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Contracts.Analytics;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class HubTileNames(ConfigurationDbContext db) : ISubjectNames
{
    public string SubjectKind => SubjectKinds.HubTile;

    public async ValueTask<IReadOnlyDictionary<string, string>> NamesAsync(IReadOnlyCollection<string> subjects, CancellationToken cancellationToken)
    {
        List<Guid> ids = subjects.Select(subject => Guid.TryParse(subject, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToList();
        List<PlacementRecord> tiles = await db.Placements
            .Where(placement => ids.Contains(placement.Id) && placement.Area == HubSurfaces.Area.Key)
            .ToListAsync(cancellationToken);
        return tiles.ToDictionary(tile => tile.Id.ToString(), tile => tile.Values.GetValueOrDefault(HubSurfaces.LinkTile.Title.Key, "A tile"));
    }
}
