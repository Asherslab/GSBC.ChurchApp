using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.News;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class MinistryNewsSource(StoredMinistries ministries) : INewsSource
{
    public IReadOnlyList<string> Feeds => MinistriesFeeds.Published;

    public async Task<NewsFromSource> LoadAsync()
    {
        YourNews yours = await ministries.YourNewsAsync();
        return new NewsFromSource(yours.InAny, yours.Posts.Select(each => Item(each.Post, each.Ministry)).ToList());
    }

    public static NewsItem Item(Post post, string ministry)
    {
        RichText content = post.Content ?? PlainText.AsRichText(post.Body);
        return new NewsItem(post.Id, post.Title, content, RichTextRules.FirstPicture(content), post.PostedAt, post.EditedAt, post.Author, ministry,
            MinistryLinks.Post(post.MinistryId, post.Id), post.Important);
    }
}
