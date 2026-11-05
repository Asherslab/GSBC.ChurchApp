using System.Security.Claims;
using GSBC.ChurchApp.Features.Media.Contracts;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class MediaService(MediaLibrary library) : IMediaService
{
    public async ValueTask<UploadTicket> BeginUploadAsync(BeginUploadRequest request, CallContext context = default) =>
        await library.BeginUploadAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<UploadedMedia> CompleteUploadAsync(CompleteUploadRequest request, CallContext context = default) =>
        await library.CompleteUploadAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<MediaMatches> FindByHashAsync(FindByHashRequest request, CallContext context = default) =>
        await library.FindByHashAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<MediaLink> GetLinkAsync(LinkRequest request, CallContext context = default) =>
        await library.GetLinkAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<MediaList> ListAsync(MediaQuery request, CallContext context = default) =>
        await library.ListAsync(UserOf(context), request, context.CancellationToken);

    public async ValueTask<MediaItemInfo> RemoveOriginalAsync(RemoveOriginalRequest request, CallContext context = default) =>
        await library.RemoveOriginalAsync(request.Id, context.CancellationToken);

    public async ValueTask<Done> DeleteAsync(DeleteMediaRequest request, CallContext context = default)
    {
        await library.DeleteAsync(request.Id, context.CancellationToken);
        return new Done();
    }

    internal static ClaimsPrincipal UserOf(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();
}
