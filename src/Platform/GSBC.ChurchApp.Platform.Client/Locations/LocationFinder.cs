using Grpc.Core;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Platform.Client.Locations;

public sealed class LocationFinder(ChannelBase channel)
{
    private readonly ILocationsService _locations = channel.CreateGrpcService<ILocationsService>();

    public async Task<FoundLocations> FindAsync(string text, CancellationToken cancellationToken = default) =>
        await _locations.FindAsync(new LocationSearch { Text = text }, cancellationToken);

    public async Task<FoundLocation?> NearestAsync(double latitude, double longitude, CancellationToken cancellationToken = default) =>
        (await _locations.NearestAsync(new LocationPoint { Latitude = latitude, Longitude = longitude }, cancellationToken)).Found.FirstOrDefault();

    public async Task<LocationMap?> PrepareMapAsync(double latitude, double longitude, CancellationToken cancellationToken = default) =>
        await _locations.PrepareMapAsync(new LocationPoint { Latitude = latitude, Longitude = longitude }, cancellationToken) is { Url.Length: > 0 } map ? map : null;
}
