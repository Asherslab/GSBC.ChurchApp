using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public static class UploadedMedia
{
    private const string Prefix = "upload-";

    public static string PlaceholderId(string sha256) => Prefix + sha256.ToLowerInvariant();

    public static bool IsPlaceholder(string mediaId) => mediaId.StartsWith(Prefix, StringComparison.Ordinal);

    public static RichText Resolve(RichText content, IReadOnlyCollection<MediaRef> uploaded)
    {
        string markdown = content.Markdown;
        List<MediaRef> media = content.Media.Where(item => !IsPlaceholder(item.Id)).ToList();
        foreach (MediaRef file in uploaded)
        {
            string placeholder = LinkTargets.MediaScheme + PlaceholderId(Path.GetFileNameWithoutExtension(file.Key));
            if (!markdown.Contains(placeholder, StringComparison.Ordinal))
                continue;
            markdown = markdown.Replace(placeholder, LinkTargets.MediaScheme + file.Id, StringComparison.Ordinal);
            media.Add(file);
        }
        return new RichText { Markdown = markdown, Media = media };
    }
}
