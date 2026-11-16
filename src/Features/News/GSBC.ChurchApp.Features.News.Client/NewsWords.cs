using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Client.News;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.News.Client;

public static class NewsWords
{
    public const string ChurchTab = "church";
    public const string YoursTab = "yours";
    public const string EveryTab = "every";

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
        NewsState.Draft => $"A draft{By(post)}. The post isn't published, and nobody can see it yet.",
        NewsState.WaitingToPublish => $"Not posted yet.{(post.Author.Length > 0 ? $" {post.Author} asks for the post to be published." : "")}",
        NewsState.Scheduled when post.PublishAt is { } at => $"Not posted yet. The post publishes by itself {ChurchDates.Describe(at, DateLineStyle.Long, withTime: true, clock.GetUtcNow().UtcDateTime)}.",
        NewsState.Archived when post.PublishedAt is null => $"Archived before it was posted{By(post)}. Nobody can see it.",
        NewsState.Archived => $"Archived. The post was published {When(post, clock)}{By(post)}, and nobody can see it now.",
        _ => null,
    };

    private static string By(NewsPost post) => post.Author.Length > 0 ? $" by {post.Author}" : "";

    public static string When(NewsPost post, TimeProvider clock) =>
        ChurchDates.Describe(PostedAt(post), DateLineStyle.Long, withTime: false, clock.GetUtcNow().UtcDateTime) + (post.EditedAt is null ? "" : " · edited");

    public static string Told(NewsPost post, TimeProvider clock) => post.ToldAt switch
    {
        null => "",
        { } told when post.TimesTold > 1 => $", with a notification (sent again {SentAt(told, clock)})",
        _ => ", with a notification",
    };

    private const string UpdatedMark = "Updated: ";

    public static string Updated(string title)
    {
        while (title.StartsWith(UpdatedMark, StringComparison.OrdinalIgnoreCase))
            title = title[UpdatedMark.Length..].TrimStart();
        string words = UpdatedMark + title;
        return words.Length <= NewsLimits.LongestTitle ? words : words[..(NewsLimits.LongestTitle - 1)].TrimEnd() + "…";
    }

    public static string SentAt(DateTime utc, TimeProvider clock)
    {
        DateTime at = ChurchTime.ToChurch(utc);
        string day = ChurchDates.Relative(at.Date, ChurchTime.ToChurch(clock.GetUtcNow().UtcDateTime).Date);
        return $"{(day is "Today" or "Yesterday" ? day.ToLowerInvariant() : $"on {day}")} at {ChurchDates.Time(at)}";
    }

    public static string InASentence(string when) =>
        when.StartsWith("Today", StringComparison.Ordinal) || when.StartsWith("Yesterday", StringComparison.Ordinal) ? char.ToLowerInvariant(when[0]) + when[1..] : when;

    public static string Posted(NewsItem item, TimeProvider clock) =>
        ChurchDates.Describe(item.PostedAt, DateLineStyle.Relative, withTime: false, clock.GetUtcNow().UtcDateTime) + (item.EditedAt is null ? "" : " · edited");

    public static string StateLine(NewsPost post, TimeProvider clock) => post.State switch
    {
        NewsState.Published when post.Shared => $"From {post.From}, in church news. Change the post in the ministry, and church news shows the change too.",
        NewsState.Published => $"Published {When(post, clock)}{Told(post, clock)}.",
        NewsState.Scheduled when post.PublishAt is { } at => $"Publishes by itself {ChurchDates.Describe(at, DateLineStyle.Long, withTime: true, clock.GetUtcNow().UtcDateTime)}.",
        NewsState.WaitingToPublish => $"{post.Author} asks for the post to be published.",
        NewsState.Asked => $"{post.From}'s leaders ask for the post to go in church news.",
        NewsState.Declined => $"Kept in {post.From}'s news{(post.AnsweredBy.Length > 0 ? $" by {post.AnsweredBy}" : "")}.",
        NewsState.Archived => "Archived. The post is out of everyone's News. Put it back to change or publish it again.",
        _ => "Being written. Nobody can see the post yet.",
    };
}
