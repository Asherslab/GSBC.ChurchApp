using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.News;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistryNewsForStaff
{
    public static MinistryNewsItem Item(Post post, string ministry, string href)
    {
        ChurchNewsStanding standing = Standing(post.ChurchNews);
        NewsItem item = MinistryNewsSource.Item(post, ministry) with { Href = href, Status = Status(standing) };
        return new MinistryNewsItem(item, post.MinistryId, standing);
    }

    private static ChurchNewsStanding Standing(string state) => state switch
    {
        ChurchNewsStates.Asked => ChurchNewsStanding.Asked,
        ChurchNewsStates.Shared => ChurchNewsStanding.InChurchNews,
        ChurchNewsStates.Declined => ChurchNewsStanding.Kept,
        _ => ChurchNewsStanding.None,
    };

    private static string Status(ChurchNewsStanding standing) => standing switch
    {
        ChurchNewsStanding.Asked => "Asking for church news",
        ChurchNewsStanding.InChurchNews => "In church news",
        _ => "",
    };
}

public sealed class StoredEveryMinistrysNews(StoredMinistries ministries, IMinistryWrites writes) : IEveryMinistrysNews
{
    public IReadOnlyList<string> Feeds => MinistriesFeeds.Published;

    public async Task<IReadOnlyList<MinistryNewsItem>> LoadAsync() =>
        (await ministries.EveryMinistrysNewsAsync())
        .Select(each => MinistryNewsForStaff.Item(each.Post, each.Ministry, MinistryLinks.Post(each.Post.MinistryId, each.Post.Id))).ToList();

    public Task PutInChurchNewsAsync(MinistryNewsItem post, bool tell) => writes.ShareToChurchNewsAsync(post.MinistryId, post.Item.Id, tell);
}
