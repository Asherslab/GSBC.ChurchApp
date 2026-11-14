using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Outreach.Client;

[AdminOnly]
public sealed class OutreachMapsClient(ChannelBase channel)
{
    private readonly IOutreachMapsService _maps = channel.CreateGrpcService<IOutreachMapsService>();

    public async Task<RegionList> RegionsAsync() => await _maps.RegionsAsync(new RegionListRequest());

    public async Task<MapAreaList> AreasAsync() => await _maps.AreasAsync(new MapAreaListRequest());

    public async Task<MapArea> AddAsync(string name, double latitude, double longitude, double radiusKm) =>
        await _maps.AddAsync(new AddMapArea { Name = name, CentreLatitude = latitude, CentreLongitude = longitude, RadiusKm = radiusKm });

    public async Task<MapArea> RenameAsync(string id, string name) => await _maps.RenameAsync(new RenameMapArea { Id = id, Name = name });

    public async Task<MapArea> RebuildAsync(string id) => await _maps.RebuildAsync(new MapAreaRef { Id = id });

    public async Task<MapAreaList> DeleteAsync(string id) => await _maps.DeleteAsync(new MapAreaRef { Id = id });
}
