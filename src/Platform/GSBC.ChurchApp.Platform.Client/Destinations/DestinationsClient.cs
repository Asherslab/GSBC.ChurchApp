using Grpc.Core;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Platform.Client.Destinations;

public sealed class DestinationsClient(ChannelBase channel, DestinationCatalog catalog)
{
    private readonly IDestinationsService _destinations = channel.CreateGrpcService<IDestinationsService>();

    public DestinationCatalog Catalog => catalog;

    public IReadOnlyCollection<string> Feeds => catalog.Kinds.SelectMany(kind => kind.Feeds ?? []).ToHashSet();

    public ValueTask<DestinationResults> SearchAsync(string text, string? area = null, string? openableBy = null, int take = 0) =>
        _destinations.SearchAsync(new DestinationSearch { Text = text, Area = area, OpenableBy = openableBy, Take = take });

    public ValueTask<Destination> DescribeAsync(string path) =>
        _destinations.DescribeAsync(new DestinationRequest { Path = path });
}
