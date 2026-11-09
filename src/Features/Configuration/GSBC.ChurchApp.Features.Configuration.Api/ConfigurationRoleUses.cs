using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class ConfigurationRoleUses(ConfigurationDbContext db) : IRoleUses
{
    private static readonly IReadOnlyList<SurfaceArea> Areas = [HubSurfaces.Area, TabBarSurfaces.Area, AppearanceSurfaces.Area];

    public async ValueTask<IReadOnlyList<string>> UsesAsync(Guid roleId, CancellationToken cancellationToken)
    {
        string id = roleId.ToString("D");
        List<string> areas = await db.Placements.AsNoTracking()
            .Where(placement => placement.Audience.Contains(id))
            .Select(placement => placement.Area)
            .ToListAsync(cancellationToken);
        return areas.GroupBy(area => area)
            .Select(area => $"{Things(area.Count())} on {Areas.FirstOrDefault(known => known.Key == area.Key)?.Title ?? "the app"}")
            .ToList();
    }

    private static string Things(int count) => count == 1 ? "something" : $"{count} things";
}
