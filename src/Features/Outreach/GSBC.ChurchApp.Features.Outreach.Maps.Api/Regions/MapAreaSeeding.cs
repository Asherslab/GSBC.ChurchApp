using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;

internal static class MapAreaSeeding
{
    public const string ChurchAreaId = "church";
    public const string ChurchAreaName = "Around the church";

    public static async Task SeedAsync(IServiceScopeFactory scopes, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        MapSourcesOptions options = scope.ServiceProvider.GetRequiredService<IOptions<MapSourcesOptions>>().Value;
        if (!options.SeedChurchArea)
            return;
        OutreachMapsDbContext db = scope.ServiceProvider.GetRequiredService<OutreachMapsDbContext>();
        if (await db.Areas.AnyAsync(cancellationToken))
            return;
        GeoBounds bounds = GeoArea.Around(new GeoPoint(options.ChurchLatitude, options.ChurchLongitude), options.ChurchRadiusKm * 1000);
        db.Areas.Add(MapAreas.New(ChurchAreaId, ChurchAreaName, bounds, scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync(cancellationToken);
    }
}
