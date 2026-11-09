using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Sermons.Import.Contracts;

[Service("gsbc.sermons.Import")]
public interface ISermonImportService
{
    [Requires(SermonsImportPermissions.ImportNode)]
    ValueTask<ImportProgress> ProgressAsync(ImportRequest request, CallContext context = default);

    [Requires(SermonsImportPermissions.ImportNode)]
    ValueTask<ImportProgress> StartAsync(ImportRequest request, CallContext context = default);

    [Requires(SermonsImportPermissions.ImportNode)]
    ValueTask<ImportProgress> RetryAsync(ImportRetry request, CallContext context = default);
}
