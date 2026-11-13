using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.News.Api;

internal static class NewsRules
{
    public const int LongestTitle = 120;
    public const int LongestTell = 240;
    public const int LongestLinkLabel = 40;
    public const string DefaultLinkLabel = "Open it";

    public static string? WhyNot(NewsPost post)
    {
        if (post.Title.Trim().Length == 0)
            return "Give it a title.";
        if (post.Title.Trim().Length > LongestTitle)
            return $"Keep the title under {LongestTitle} characters.";
        if (RichTextRules.ProblemsWith(post.Body, allowed: RichTextMedia.Posts) is [var first, ..])
            return first;
        if (!Audience.TryParse(post.Audience, out _))
            return "Choose who it's for.";
        if (post.LinkPath.Trim().Length > 0 && !LinkTargets.IsSafe(post.LinkPath.Trim()))
            return "The link has to be a page in the app or an https:// address.";
        if (post.LinkLabel.Trim().Length > LongestLinkLabel)
            return $"Keep the link's words under {LongestLinkLabel} characters.";
        if (post.TellTitle.Trim().Length > LongestTitle || post.TellBody.Trim().Length > LongestTell)
            return $"Keep the notification under {LongestTitle} characters for its title and {LongestTell} for its line.";
        return null;
    }

    public static bool IsPublished(PostRecord post) => post.State == NewsState.Published;

    public static bool IsForEveryone(PostRecord post) => post.Audience == Audience.Everyone.Text;

    public static bool IsShared(PostRecord post) => post.Source is not null;

    public static bool WritersMayChange(PostRecord post) => post.State is NewsState.Draft or NewsState.WaitingToPublish && !IsShared(post);

    public static RichText BodyOf(PostRecord post) => ContentJson.TryRead(post.Body, out RichText? body) ? body! : new RichText();

    public static MediaRef? PictureOf(RichText body)
    {
        IReadOnlyList<string> used = RichTextParser.Parse(body.Markdown).MediaIds;
        return used.Select(body.MediaById).FirstOrDefault(media => media is { IsImage: true });
    }

    public static string TellTitleOf(PostRecord post) => post.TellTitle.Trim().Length > 0 ? post.TellTitle.Trim() : post.Title;

    public static string TellBodyOf(PostRecord post)
    {
        if (post.TellBody.Trim().Length > 0)
            return post.TellBody.Trim();
        string text = PlainText.Of(BodyOf(post));
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
