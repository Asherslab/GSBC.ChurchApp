using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static GSBC.ChurchApp.Features.Configuration.Api.PlacementEditor;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class TileReplacing(IServiceScopeFactory scopes, SurfaceCatalog surfaces, ILogger<TileReplacing> logger) : BackgroundService
{
    private static readonly IReadOnlyList<string> KeptValues = [HubTileKeys.Columns, HubTileKeys.Rows];

    public static string KeyFor(string retired) => $"replaced:{retired}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            ConfigurationDbContext db = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            PlacementEditor placements = scope.ServiceProvider.GetRequiredService<PlacementEditor>();
            foreach (Surface surface in surfaces.In(HubSurfaces.Area).Where(surface => surface.Replaces is not null))
            {
                int moved = await ReplaceOnceAsync(db, placements, surface, stoppingToken);
                if (moved > 0)
                    logger.LogInformation("Turned {Count} {Retired} tiles on Home into {Surface}", moved, surface.Replaces, surface.Key);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Replacing retired Home tiles failed");
        }
    }

    public static async Task<int> ReplaceOnceAsync(ConfigurationDbContext db, PlacementEditor placements, Surface surface, CancellationToken cancellationToken)
    {
        if (surface.Replaces is not { } retired)
            return 0;
        string key = KeyFor(retired);
        if (await db.Seeds.AnyAsync(seed => seed.Key == key, cancellationToken))
            return 0;

        List<PlacementRecord> tiles = await db.Placements.Where(placement => placement.Area == HubSurfaces.Area.Key && placement.Surface == retired)
            .ToListAsync(cancellationToken);
        List<Change> changes = tiles.Select(tile => Replaced(tile, surface)).ToList();
        db.Seeds.Add(new SeedRecord { Key = key, SeededAt = DateTime.UtcNow });
        await placements.CommitAsync(Guid.Empty, changes, rollsBack: null, draft: null, cancellationToken);
        return changes.Count;
    }

    private static Change Replaced(PlacementRecord tile, Surface surface)
    {
        PlacementState before = PlacementState.Of(tile);
        SortedDictionary<string, string> values = new(before.Values.Where(pair => KeptValues.Contains(pair.Key)).ToDictionary());
        return new Change(tile, before, before with { Surface = surface.Key, Values = values });
    }
}
