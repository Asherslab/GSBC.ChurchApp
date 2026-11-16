using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

[Service("gsbc.outreach.Maps")]
public interface IOutreachMapsService
{
    [SignedIn]
    ValueTask<RegionList> RegionsAsync(RegionListRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<MapOverview> OverviewAsync(MapOverviewRequest request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<MapAreaList> AreasAsync(MapAreaListRequest request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<MapArea> AddAsync(AddMapArea request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<MapArea> RenameAsync(RenameMapArea request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<MapArea> RebuildAsync(MapAreaRef request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<MapArea> RetryAsync(MapAreaRef request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<MapAreaList> DeleteAsync(MapAreaRef request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<PlaceList> FindPlacesAsync(PlaceSearch request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<MapAreaPreview> PreviewAsync(MapAreaPlan request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<RegionChangeCheck> CheckChangeAsync(RegionChange request, CallContext context = default);

    [Requires(OutreachMapsPermissions.ManageNode)]
    ValueTask<MapArea> ChangeRegionsAsync(RegionChange request, CallContext context = default);
}
