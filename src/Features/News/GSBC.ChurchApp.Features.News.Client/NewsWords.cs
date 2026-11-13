using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.News;

namespace GSBC.ChurchApp.Features.News.Client;

public static class NewsWords
{
    public const string ChurchTab = "church";
    public const string YoursTab = "yours";

    public static string Href(string id) => $"news/{id}";

    public static DateTime PostedAt(NewsPost post) => post.PublishedAt ?? post.CreatedAt;

    public static NewsItem Item(NewsPost post) => new(
        post.Id, post.Title, post.Body, post.Picture, PostedAt(post), post.EditedAt, post.Author, post.From, Href(post.Id), post.Urgent);

    public static string State(NewsPost post) => post.State switch
    {
        NewsState.Draft => "Being written",
        NewsState.WaitingToPublish => "Waiting for someone to publish it",
        NewsState.Scheduled => "Scheduled",
        NewsState.Asked => $"{post.From} asks for church news",
        NewsState.Declined => $"Kept in {post.From}'s news",
        _ => "",
    };
}
