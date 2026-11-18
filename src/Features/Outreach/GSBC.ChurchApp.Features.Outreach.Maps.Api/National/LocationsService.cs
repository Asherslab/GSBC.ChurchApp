using Grpc.Core;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.National;

internal sealed class LocationsService(NationalAddresses addresses, PlaceMaps maps) : ILocationsService
{
    private const int LongestSearch = 200;

    public async ValueTask<FoundLocations> FindAsync(LocationSearch request, CallContext context = default) =>
        request.Text.Length > LongestSearch ? new FoundLocations() : await addresses.FindAsync(request.Text, context.CancellationToken);

    public async ValueTask<FoundLocations> NearestAsync(LocationPoint request, CallContext context = default) =>
        await addresses.NearestAsync(Checked(request.Latitude, 90), Checked(request.Longitude, 180), context.CancellationToken);

    public async ValueTask<LocationMap> PrepareMapAsync(LocationPoint request, CallContext context = default) =>
        await maps.PrepareAsync(Checked(request.Latitude, 85), Checked(request.Longitude, 180), context.CancellationToken);

    private static double Checked(double value, double limit) =>
        double.IsFinite(value) && Math.Abs(value) <= limit ? value : throw new RpcException(new Status(StatusCode.InvalidArgument, "That place isn't on the map."));
}
