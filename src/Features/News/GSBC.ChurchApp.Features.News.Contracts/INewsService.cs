using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.News.Contracts;

[Service("gsbc.news.News")]
public interface INewsService
{
    [SignedIn]
    ValueTask<NewsPost> SaveAsync(NewsSave request, CallContext context = default);

    [SignedIn]
    ValueTask<NewsPost> PublishAsync(PublishRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<NewsPost> AskToPublishAsync(NewsRef request, CallContext context = default);

    [SignedIn]
    ValueTask<NewsPost> BackToWritingAsync(NewsRef request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> DeleteAsync(NewsRef request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> TryOnMyPhoneAsync(NewsRef request, CallContext context = default);

    [SignedIn]
    ValueTask<NewsPost> AnswerAskAsync(AskAnswer request, CallContext context = default);

    [SignedIn]
    ValueTask<NewsList> DeskAsync(DeskRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<NewsReach> ReachAsync(ReachRequest request, CallContext context = default);
}
