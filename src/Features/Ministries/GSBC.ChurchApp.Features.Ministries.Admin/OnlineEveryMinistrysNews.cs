using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.News;

namespace GSBC.ChurchApp.Features.Ministries.Admin;

public sealed class OnlineEveryMinistrysNews(OnlineMinistries ministries) : IEveryMinistrysNews
{
    public IReadOnlyList<string> Feeds => ministries.Feeds;

    public async Task<IReadOnlyList<MinistryNewsItem>> LoadAsync() =>
        (await ministries.EveryMinistrysNewsAsync())
        .Select(post => MinistryNewsForStaff.Item(post, post.From, $"ministries/{post.MinistryId}/news")).ToList();

    public Task PutInChurchNewsAsync(MinistryNewsItem post, bool tell) => ministries.ShareToChurchNewsAsync(post.MinistryId, post.Item.Id, tell);
}
