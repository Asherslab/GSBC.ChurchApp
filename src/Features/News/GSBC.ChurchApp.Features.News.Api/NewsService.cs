using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Content;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class NewsService(NewsDesk desk, NewsTelling telling, IAccessEvaluator access, AppCardChecks cards) : INewsService
{
    public async ValueTask<NewsPost> SaveAsync(NewsSave request, CallContext context = default)
    {
        Writer writer = await WriterAsync(context);
        if (await cards.ProblemAsync(context.ServerCallContext!.GetHttpContext().User, request.Post.Body, context.CancellationToken) is { } problem)
            throw new RpcException(new Status(StatusCode.InvalidArgument, problem));
        return await desk.SaveAsync(writer, request.Post, context.CancellationToken, request.Uploaded);
    }

    public async ValueTask<NewsPost> PublishAsync(PublishRequest request, CallContext context = default) =>
        await desk.PublishAsync(await WriterAsync(context), NewsDesk.ParseId(request.Id), request.At, context.CancellationToken);

    public async ValueTask<NewsPost> AskToPublishAsync(NewsRef request, CallContext context = default) =>
        await desk.AskToPublishAsync(await WriterAsync(context), NewsDesk.ParseId(request.Id), context.CancellationToken);

    public async ValueTask<NewsPost> BackToWritingAsync(NewsRef request, CallContext context = default) =>
        await desk.BackToWritingAsync(await WriterAsync(context), NewsDesk.ParseId(request.Id), context.CancellationToken);

    public async ValueTask<NewsPost> ArchiveAsync(NewsRef request, CallContext context = default) =>
        await desk.ArchiveAsync(await WriterAsync(context), NewsDesk.ParseId(request.Id), context.CancellationToken);

    public async ValueTask<NewsPost> PutBackAsync(NewsRef request, CallContext context = default) =>
        await desk.PutBackAsync(await WriterAsync(context), NewsDesk.ParseId(request.Id), context.CancellationToken);

    public async ValueTask<Done> DeleteAsync(NewsRef request, CallContext context = default)
    {
        await desk.DeleteAsync(await WriterAsync(context), NewsDesk.ParseId(request.Id), context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> TryOnMyPhoneAsync(NewsRef request, CallContext context = default)
    {
        await desk.TryOnPhoneAsync(await WriterAsync(context), NewsDesk.ParseId(request.Id), context.CancellationToken);
        return new Done();
    }

    public async ValueTask<NewsPost> AnswerAskAsync(AskAnswer request, CallContext context = default) =>
        await desk.AnswerAsync(await WriterAsync(context), NewsDesk.ParseId(request.Id), request.Yes, request.Tell, context.CancellationToken);

    public async ValueTask<NewsList> DeskAsync(DeskRequest request, CallContext context = default)
    {
        await WriterAsync(context);
        return new NewsList { Posts = [.. await desk.ListAsync(context.CancellationToken)] };
    }

    public async ValueTask<NewsReach> ReachAsync(ReachRequest request, CallContext context = default)
    {
        await WriterAsync(context);
        var reach = await telling.ReachAsync(request.Audience, context.CancellationToken);
        return new NewsReach { People = reach.People, Phones = reach.Phones };
    }

    private async Task<Writer> WriterAsync(CallContext context)
    {
        ClaimsPrincipal user = context.ServerCallContext!.GetHttpContext().User;
        Guid id = Caller.UserId(user) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));
        bool publishes = await access.IsAllowedAsync(user, NewsPermissions.Publish.Node, null, context.CancellationToken);
        if (!publishes && !await access.IsAllowedAsync(user, NewsPermissions.Write.Node, null, context.CancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Not permitted."));
        return new Writer(id, publishes);
    }
}
