using Grpc.Core;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.News.Client;

[AdminOnly]
public sealed class OnlineNews(ChannelBase channel) : INewsReads
{
    private readonly INewsService _service = channel.CreateGrpcService<INewsService>();

    public IReadOnlyList<string> Feeds => [NewsFeeds.Desk];

    public async Task<IReadOnlyList<NewsPost>> PublishedAsync() => (await DeskAsync()).Where(post => post.State == NewsState.Published).ToList();

    public async Task<IReadOnlyList<NewsPost>> DeskAsync() => (await _service.DeskAsync(new DeskRequest())).Posts;

    public async Task<NewsReach> ReachAsync(string audience) => await _service.ReachAsync(new ReachRequest { Audience = audience });
}
