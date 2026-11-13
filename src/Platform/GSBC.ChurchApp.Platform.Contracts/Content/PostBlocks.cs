using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public enum PostBlockKind
{
    Text,
    Heading,
    Pictures,
    Video,
    Audio,
    File,
    VideoLink,
}

public sealed class PostBlock
{
    public string Key { get; init; } = Guid.NewGuid().ToString("N");
    public PostBlockKind Kind { get; init; }
    public string Text { get; set; } = "";
    public string Caption { get; set; } = "";
    public string Link { get; set; } = "";
    public bool Smaller { get; set; }
    public List<MediaRef> Media { get; set; } = [];

    public bool IsEmpty => Kind switch
    {
        PostBlockKind.Text or PostBlockKind.Heading => Text.Trim().Length == 0,
        PostBlockKind.VideoLink => VideoLink.Of(Link) is null,
        _ => Media.Count == 0,
    };
}

public static class PostBlocks
{
    public static List<PostBlock> Of(RichText content)
    {
        List<PostBlock> blocks = [];
        List<string> text = [];
        foreach (string line in content.Markdown.Replace("\r\n", "\n").Split('\n'))
        {
            string trimmed = line.Trim();
            if (Heading(trimmed) is { } heading)
                Add(new PostBlock { Kind = PostBlockKind.Heading, Text = heading, Smaller = trimmed.StartsWith("### ", StringComparison.Ordinal) });
            else if (MediaLine(trimmed, content) is { } media)
                AddMedia(media);
            else if (RichTextParser.Parse(trimmed) is { Blocks: [EmbedBlock embed] })
                Add(new PostBlock { Kind = PostBlockKind.VideoLink, Link = embed.Video.WatchUrl, Caption = embed.Title });
            else
                text.Add(line);
        }
        FlushText();
        return blocks;

        void Add(PostBlock block)
        {
            FlushText();
            blocks.Add(block);
        }

        void AddMedia((MediaRef Media, string Alt, string Caption) found)
        {
            PostBlockKind kind = KindOf(found.Media);
            if (kind == PostBlockKind.Pictures && text.Count == 0 && blocks is [.., { Kind: PostBlockKind.Pictures } gallery])
            {
                text.Clear();
                gallery.Media.Add(Described(found.Media, found.Alt));
                return;
            }
            Add(new PostBlock { Kind = kind, Media = [Described(found.Media, found.Alt)], Caption = found.Caption });
        }

        void FlushText()
        {
            string joined = string.Join('\n', text).Trim('\n');
            text.Clear();
            if (joined.Trim().Length > 0)
                blocks.Add(new PostBlock { Kind = PostBlockKind.Text, Text = joined });
        }
    }

    public static RichText ToRichText(IEnumerable<PostBlock> blocks)
    {
        List<string> parts = [];
        List<MediaRef> media = [];
        foreach (PostBlock block in blocks.Where(block => !block.IsEmpty))
        {
            switch (block.Kind)
            {
                case PostBlockKind.Text:
                    parts.Add(block.Text.Trim('\n'));
                    break;
                case PostBlockKind.Heading:
                    parts.Add((block.Smaller ? "### " : "## ") + OneLine(block.Text));
                    break;
                case PostBlockKind.VideoLink:
                    parts.Add($"![{Bracketless(block.Caption)}]({VideoLink.Of(block.Link)!.WatchUrl})");
                    break;
                default:
                    parts.Add(string.Join('\n', block.Media.Select((item, index) => Line(item, index == 0 ? block.Caption : ""))));
                    media.AddRange(block.Media);
                    break;
            }
        }
        return new RichText { Markdown = string.Join("\n\n", parts), Media = media.DistinctBy(item => item.Id).ToList() };
    }

    public static PostBlockKind KindOf(MediaRef media) => RichTextRules.KindOf(media) switch
    {
        RichTextMedia.Pictures => PostBlockKind.Pictures,
        RichTextMedia.Video => PostBlockKind.Video,
        RichTextMedia.Audio => PostBlockKind.Audio,
        _ => PostBlockKind.File,
    };

    private static string Line(MediaRef media, string caption)
    {
        string described = Bracketless(media.AltText.Length > 0 ? media.AltText : media.FileName);
        string quoted = OneLine(caption).Replace("\"", "'", StringComparison.Ordinal);
        return quoted.Length == 0
            ? $"![{described}]({LinkTargets.MediaScheme}{media.Id})"
            : $"![{described}]({LinkTargets.MediaScheme}{media.Id} \"{quoted}\")";
    }

    private static string? Heading(string line) =>
        line.StartsWith("## ", StringComparison.Ordinal) ? line[3..].Trim() : line.StartsWith("### ", StringComparison.Ordinal) ? line[4..].Trim() : null;

    private static (MediaRef Media, string Alt, string Caption)? MediaLine(string line, RichText content) =>
        RichTextParser.Parse(line) is { Blocks: [MediaBlock block] } && content.MediaById(block.MediaId) is { } media ? (media, block.Alt, block.Caption) : null;

    private static MediaRef Described(MediaRef media, string alt) =>
        alt.Length == 0 || alt == media.AltText ? media : new MediaRef
        {
            Id = media.Id, Key = media.Key, ContentType = media.ContentType, Bytes = media.Bytes, Width = media.Width, Height = media.Height, Url = media.Url,
            IsPrivate = media.IsPrivate, AltText = alt, FileName = media.FileName, Version = media.Version, Sizes = media.Sizes,
        };

    private static string OneLine(string text) => string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string Bracketless(string text) => OneLine(text).Replace("[", "", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal);
}
