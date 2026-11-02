using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public static class RichTextRules
{
    public static IReadOnlyList<string> ProblemsWith(RichText content, int maximumLength = RichText.MaximumLength)
    {
        if (content.Markdown.Length > maximumLength)
            return [$"Keep it under {maximumLength:N0} characters."];

        RichTextDocument document = RichTextParser.Parse(content.Markdown);
        List<string> problems = [.. document.Problems];
        problems.AddRange(document.Links
            .Where(href => !LinkTargets.IsSafe(href))
            .Distinct()
            .Select(href => $"'{Shortened(href)}' isn't a link the app can open. Use https://, mailto:, tel: or a page in the app such as /events."));
        foreach (string id in document.ImageIds.Distinct())
        {
            MediaRef? media = content.MediaById(id);
            if (media is null)
                problems.Add("An image isn't in the media library. Choose it again.");
            else if (!media.IsImage)
                problems.Add($"'{media.FileName}' isn't an image. Link to it instead.");
        }
        return problems.Distinct().ToList();
    }

    public static RichText WithOnlyUsedMedia(RichText content)
    {
        HashSet<string> used = [.. RichTextParser.Parse(content.Markdown).ImageIds];
        return new RichText
        {
            Markdown = content.Markdown,
            Media = content.Media.Where(media => used.Contains(media.Id)).DistinctBy(media => media.Id).ToList(),
        };
    }

    private static string Shortened(string href) => href.Length <= 60 ? href : href[..57] + "…";
}
