using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class ConfigurationService(PlacementEditor editor) : IConfigurationService
{
    public async ValueTask<AreaPlacements> GetAreaAsync(AreaQuery request, CallContext context = default) =>
        await editor.GetAreaAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<AreaPlacements> SaveAsync(SaveRequest request, CallContext context = default) =>
        await editor.SaveAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<ChangeList> ListChangesAsync(AreaQuery request, CallContext context = default) =>
        await editor.ListChangesAsync(CallerOf(context), request, context.CancellationToken);

    public async ValueTask<AreaPlacements> RollBackAsync(RollBackRequest request, CallContext context = default) =>
        await editor.RollBackAsync(CallerOf(context), request, context.CancellationToken);

    private static ClaimsPrincipal CallerOf(CallContext context) => context.ServerCallContext!.GetHttpContext().User;
}
