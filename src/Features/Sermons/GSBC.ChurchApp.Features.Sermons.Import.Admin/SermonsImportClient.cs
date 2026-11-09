using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Import.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Sermons.Import.Admin;

public sealed class SermonsImportClient(ChannelBase channel)
{
    private readonly ISermonImportService _import = channel.CreateGrpcService<ISermonImportService>();

    public ValueTask<ImportProgress> ProgressAsync() => _import.ProgressAsync(new ImportRequest());

    public ValueTask<ImportProgress> StartAsync() => _import.StartAsync(new ImportRequest());

    public ValueTask<ImportProgress> RetryAsync(string websitePath) => _import.RetryAsync(new ImportRetry { WebsitePath = websitePath });
}
