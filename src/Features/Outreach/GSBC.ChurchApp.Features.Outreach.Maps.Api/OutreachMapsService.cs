using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Geo;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Regions;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api;

internal sealed class OutreachMapsService(OutreachMapsDbContext db, AreaBuildRunner runner, AreaBuilder builder, IBlobStore blobs, TimeProvider time)
    : IOutreachMapsService
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async ValueTask<RegionList> RegionsAsync(RegionListRequest request, CallContext context = default) => new()
    {
        Regions = (await db.Regions.AsNoTracking().OrderBy(region => region.Id).ToListAsync(context.CancellationToken))
            .Select(region => RegionFeed.ToMessage(region, blobs)).ToList(),
    };

    public async ValueTask<MapAreaList> AreasAsync(MapAreaListRequest request, CallContext context = default) => await ListAsync(context.CancellationToken);

    public async ValueTask<MapArea> AddAsync(AddMapArea request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        string name = request.Name.Trim();
        if (name.Length == 0)
            throw Invalid("Give the area a name.");
        if (request.RadiusKm is < MapAreas.SmallestRadiusKm or > MapAreas.LargestRadiusKm)
            throw Invalid($"An area reaches between {MapAreas.SmallestRadiusKm:0} and {MapAreas.LargestRadiusKm:0} km from its centre.");
        if (request.CentreLatitude is < -90 or > 90 || request.CentreLongitude is < -180 or > 180)
            throw Invalid("That centre isn't a place on the map.");
        HashSet<string> taken = (await db.Areas.Select(area => area.Id).ToListAsync(cancellationToken)).ToHashSet();
        GeoBounds bounds = GeoArea.Around(new GeoPoint(request.CentreLatitude, request.CentreLongitude), request.RadiusKm * 1000);
        MapAreaRecord added = MapAreas.New(MapAreas.Slug(name, taken), name, bounds, Now);
        db.Areas.Add(added);
        db.FeedChanges.Add(RegionFeed.BuildChanged(added.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        runner.Wake();
        return MapAreas.ToMessage(added, [], blobs);
    }

    public async ValueTask<MapArea> RenameAsync(RenameMapArea request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.Id, cancellationToken);
        string name = request.Name.Trim();
        if (name.Length == 0)
            throw Invalid("Give the area a name.");
        area.Name = name;
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await db.SaveChangesAsync(cancellationToken);
        return await MessageAsync(area, cancellationToken);
    }

    public async ValueTask<MapArea> RebuildAsync(MapAreaRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.Id, cancellationToken);
        if (area.State is not (MapAreaStates.Waiting or MapAreaStates.Building))
        {
            (area.State, area.Problem, area.Stage) = (MapAreaStates.Waiting, null, null);
            db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
            await db.SaveChangesAsync(cancellationToken);
            runner.Wake();
        }
        return await MessageAsync(area, cancellationToken);
    }

    public async ValueTask<MapAreaList> DeleteAsync(MapAreaRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MapAreaRecord area = await FindAsync(request.Id, cancellationToken);
        if (area.State == MapAreaStates.Building)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Its maps are being built. Delete it once that finishes."));
        db.Areas.Remove(area);
        db.FeedChanges.Add(RegionFeed.BuildChanged(area.Id, Now));
        await builder.RemoveAsync(area.Id, new HashSet<string>(), cancellationToken);
        await builder.ForgetOldFilesAsync(cancellationToken);
        return await ListAsync(cancellationToken);
    }

    private async Task<MapAreaList> ListAsync(CancellationToken cancellationToken)
    {
        List<MapAreaRecord> areas = await db.Areas.AsNoTracking().OrderBy(area => area.CreatedAt).ToListAsync(cancellationToken);
        ILookup<string, RegionRecord> regions = (await db.Regions.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(region => region.AreaId);
        return new MapAreaList { Areas = areas.Select(area => MapAreas.ToMessage(area, regions[area.Id], blobs)).ToList() };
    }

    private async Task<MapArea> MessageAsync(MapAreaRecord area, CancellationToken cancellationToken) =>
        MapAreas.ToMessage(area, await db.Regions.AsNoTracking().Where(region => region.AreaId == area.Id).ToListAsync(cancellationToken), blobs);

    private async Task<MapAreaRecord> FindAsync(string id, CancellationToken cancellationToken) =>
        await db.Areas.FindAsync([id], cancellationToken) ?? throw new RpcException(new Status(StatusCode.NotFound, "That area isn't here any more."));

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));
}
