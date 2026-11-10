using Grpc.Core;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Platform.Client.Destinations;

[AdminOnly]
public sealed class DestinationSearching(ChannelBase channel)
{
    private readonly IDestinationsService _destinations = channel.CreateGrpcService<IDestinationsService>();

    public ValueTask<DestinationResults> SearchAsync(string text, string? area = null, string? openableBy = null, int take = 0) =>
        _destinations.SearchAsync(new DestinationSearch { Text = text, Area = area, OpenableBy = openableBy, Take = take });
}
