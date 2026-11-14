using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

[Flags]
public enum RichTextMedia
{
    Pictures = 1,
    Video = 2,
    Audio = 4,
    Files = 8,
    VideoLinks = 16,
    Layout = 32,
    AppCards = 64,
    Posts = Pictures | Video | Audio | Files | VideoLinks | Layout | AppCards,
}

public static class RichTextRules
{
    public static IReadOnlyList<string> ProblemsWith(RichText content, int maximumLength = RichText.MaximumLength, RichTextMedia allowed = RichTextMedia.Pictures)
    {
        if (content.Markdown.Length > maximumLength)
            return [$"Keep it under {maximumLength:N0} characters."];

        RichTextDocument document = RichTextParser.Parse(content.Markdown);
        List<string> problems = [.. document.Problems];
        problems.AddRange(document.Links
            .Where(href => !LinkTargets.IsSafe(href))
            .Distinct()
            .Select(href => $"'{Shortened(href)}' isn't a link the app can open. Use https://, mailto:, tel: or a page in the app such as /events."));
        foreach (string id in document.MediaIds.Distinct())
        {
            MediaRef? media = content.MediaById(id);
            if (media is null)
                problems.Add("A picture or file isn't in the media library. Choose it again.");
            else if (!allowed.HasFlag(KindOf(media)))
                problems.Add(allowed == RichTextMedia.Pictures ? $"'{media.FileName}' isn't a picture. Link to it instead." : $"'{media.FileName}' can't go in here. Link to it instead.");
        }
        if (document.Videos.Count > 0 && !allowed.HasFlag(RichTextMedia.VideoLinks))
            problems.Add("Videos can't be shown in here. Link to the video instead.");
        problems.AddRange(document.Directives.Select(directive => ProblemWith(directive, allowed)).OfType<string>());
        return problems.Distinct().ToList();
    }

    public static IReadOnlyList<string> ChatProblems(string text) =>
        RichTextParser.Parse(text, RichTextScope.Chat).Links
            .Where(href => !LinkTargets.IsSafe(href))
            .Distinct()
            .Select(href => $"'{Shortened(href)}' isn't a link the app can open. Use https://, mailto: or tel:, or a page in the app such as /events.")
            .ToList();

    public static MediaRef? FirstPicture(RichText content) =>
        RichTextParser.Parse(content.Markdown).MediaIds.Select(content.MediaById).FirstOrDefault(media => media is { IsImage: true });

    public static RichTextMedia KindOf(MediaRef media) =>
        media.IsImage ? RichTextMedia.Pictures
        : media.IsVideo ? RichTextMedia.Video
        : media.IsAudio ? RichTextMedia.Audio
        : RichTextMedia.Files;

    public static RichText WithOnlyUsedMedia(RichText content)
    {
        HashSet<string> used = [.. RichTextParser.Parse(content.Markdown).MediaIds];
        return new RichText
        {
            Markdown = content.Markdown,
            Media = content.Media.Where(media => used.Contains(media.Id)).DistinctBy(media => media.Id).ToList(),
        };
    }

    private static string? ProblemWith(DirectiveBlock directive, RichTextMedia allowed) =>
        AppCards.ProblemWith(directive) is { } problem ? problem
        : allowed.HasFlag(RichTextMedia.AppCards) ? null
        : $"A card for {AppCards.WithArticle(AppCards.Of(directive)!.Kind)} can't go in here. Link to it instead.";

    private static string Shortened(string href) => href.Length <= 60 ? href : href[..57] + "…";
}
