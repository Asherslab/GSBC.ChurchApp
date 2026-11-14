using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.News;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.News.Client;

public static class NewsWords
{
    public const string ChurchTab = "church";
    public const string YoursTab = "yours";

    public static string Href(string id) => $"news/{id}";

    public static DateTime PostedAt(NewsPost post) => post.PublishedAt ?? post.CreatedAt;

    public static NewsItem Item(NewsPost post) => new(
        post.Id, post.Title, post.Body, post.Picture, PostedAt(post), post.EditedAt, post.Author, post.From, Href(post.Id), post.Urgent);

    public static string AudienceWords(string audience) =>
        !Audience.TryParse(audience, out Audience? parsed) || parsed == Audience.Everyone ? "Everyone"
        : parsed == Audience.SignedIn ? "Everyone signed in"
        : parsed.IsForRoles ? parsed.RoleIds.Count == 1 ? "People in a role" : $"People in {parsed.RoleIds.Count} roles"
        : "Some people";

    public static string State(NewsPost post) => post.State switch
    {
        NewsState.Draft => "Being written",
        NewsState.WaitingToPublish => "Waiting for someone to publish it",
        NewsState.Scheduled => "Scheduled",
        NewsState.Asked => $"{post.From} asks for church news",
        NewsState.Declined => $"Kept in {post.From}'s news",
        NewsState.Archived => "Archived",
        _ => "",
    };

    public static string ArchivedLine(NewsPost post, TimeProvider clock) =>
        post.PublishedAt is null ? "Archived before it was posted" : $"Archived, was posted {When(post, clock)}";

    public static string? NotPosted(NewsPost post, TimeProvider clock) => post.State switch
    {
        NewsState.Draft => $"A draft{By(post)}. It isn't posted, and nobody sees it yet.",
        NewsState.WaitingToPublish => $"Not posted yet.{(post.Author.Length > 0 ? $" {post.Author} asks for it to be published." : "")}",
        NewsState.Scheduled when post.PublishAt is { } at => $"Not posted yet. It's posted by itself {ChurchDates.Describe(at, DateLineStyle.Long, withTime: true, clock.GetUtcNow().UtcDateTime)}.",
        NewsState.Archived when post.PublishedAt is null => $"Archived before it was posted{By(post)}. Nobody sees it.",
        NewsState.Archived => $"Archived. It was posted {When(post, clock)}{By(post)}, and nobody sees it now.",
        _ => null,
    };

    private static string By(NewsPost post) => post.Author.Length > 0 ? $" by {post.Author}" : "";

    public static string When(NewsPost post, TimeProvider clock) =>
        ChurchDates.Describe(PostedAt(post), DateLineStyle.Long, withTime: false, clock.GetUtcNow().UtcDateTime) + (post.EditedAt is null ? "" : " · edited");

    public static string StateLine(NewsPost post, TimeProvider clock) => post.State switch
    {
        NewsState.Published when post.Shared => $"From {post.From}, in church news. Change it in the ministry; the change shows here too.",
        NewsState.Published => $"Published {When(post, clock)}{(post.ToldAt is not null ? ", with a notification" : "")}.",
        NewsState.Scheduled when post.PublishAt is { } at => $"Publishes by itself {ChurchDates.Describe(at, DateLineStyle.Long, withTime: true, clock.GetUtcNow().UtcDateTime)}.",
        NewsState.WaitingToPublish => $"{post.Author} asks for it to be published.",
        NewsState.Asked => $"{post.From}'s leaders ask for it to go in church news.",
        NewsState.Declined => $"Kept in {post.From}'s news{(post.AnsweredBy.Length > 0 ? $" by {post.AnsweredBy}" : "")}.",
        NewsState.Archived => "Archived. It's out of everyone's News. Put it back to change it or publish it again.",
        _ => "Being written. Nobody sees it yet.",
    };
}
