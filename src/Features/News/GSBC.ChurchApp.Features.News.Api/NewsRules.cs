using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.News.Api;

internal static class NewsRules
{
    public const int LongestTitle = NewsLimits.LongestTitle;
    public const int LongestTell = NewsLimits.LongestTell;
    public const int LongestLinkLabel = 40;
    public const string DefaultLinkLabel = "Open it";

    public static string? WhyNot(NewsPost post)
    {
        if (post.Title.Trim().Length == 0)
            return "Enter a title for the post.";
        if (post.Title.Trim().Length > LongestTitle)
            return $"Keep the title under {LongestTitle} characters.";
        if (RichTextRules.ProblemsWith(post.Body, allowed: RichTextMedia.Posts) is [var first, ..])
            return first;
        if (!Audience.TryParse(post.Audience, out _))
            return "Choose who the post is for.";
        if (post.LinkPath.Trim().Length > 0 && !LinkTargets.IsSafe(post.LinkPath.Trim()))
            return "The link has to be a page in the app or an https:// address.";
        if (post.LinkLabel.Trim().Length > LongestLinkLabel)
            return $"Keep the link's words under {LongestLinkLabel} characters.";
        return WhyNotTell(post.TellTitle, post.TellBody);
    }

    public static string? WhyNotTell(string title, string body) =>
        title.Trim().Length > LongestTitle || body.Trim().Length > LongestTell
            ? $"Keep the notification under {LongestTitle} characters for its title and {LongestTell} for its line."
            : null;

    public static bool IsPublished(PostRecord post) => post.State == NewsState.Published;

    public static bool IsForEveryone(PostRecord post) => post.Audience == Audience.Everyone.Text;

    public static bool IsShared(PostRecord post) => post.Source is not null;

    public static bool WritersMayChange(PostRecord post) =>
        !IsShared(post) && (post.State is NewsState.Draft or NewsState.WaitingToPublish || post is { State: NewsState.Archived, PublishedAt: null });

    public static RichText BodyOf(PostRecord post) => ContentJson.TryRead(post.Body, out RichText? body) ? body! : new RichText();

    public static MediaRef? PictureOf(RichText body) => RichTextRules.FirstPicture(body);

    public static string TellTitleOf(PostRecord post) => post.TellTitle.Trim().Length > 0 ? post.TellTitle.Trim() : post.Title;

    public static string TellBodyOf(PostRecord post)
    {
        if (post.TellBody.Trim().Length > 0)
            return post.TellBody.Trim();
        string text = PlainText.Opening(BodyOf(post));
        return text.Length <= LongestTell ? text : text[..(LongestTell - 1)].TrimEnd() + "…";
    }

    public static NewsPost View(PostRecord post) => new()
    {
        Id = post.Id.ToString(),
        Title = post.Title,
        Body = BodyOf(post),
        Picture = PictureOf(BodyOf(post)),
        Audience = post.Audience,
        State = post.State,
        PublishAt = post.PublishAt,
        PublishedAt = post.PublishedAt,
        Author = post.AuthorName,
        CreatedAt = post.CreatedAt,
        EditedAt = post.EditedAt,
        LinkPath = post.LinkPath,
        LinkLabel = post.LinkPath.Length == 0 ? "" : post.LinkLabel.Length > 0 ? post.LinkLabel : DefaultLinkLabel,
        Tell = post.Tell,
        TellTitle = post.TellTitle,
        TellBody = post.TellBody,
        Urgent = post.Urgent,
        ToldAt = post.ToldAt,
        TimesTold = post.TimesTold,
        From = post.FromName,
        FromLink = post.FromLink,
        Shared = IsShared(post),
        AskedBy = post.AskedByName,
        AnsweredBy = post.AnsweredByName,
    };

    public static NewsPost ForReaders(PostRecord post)
    {
        NewsPost view = View(post);
        (view.TellTitle, view.TellBody, view.AskedBy, view.AnsweredBy) = ("", "", "", "");
        return view;
    }
}
