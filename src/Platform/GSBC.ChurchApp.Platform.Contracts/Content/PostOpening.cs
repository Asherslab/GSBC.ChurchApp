using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public sealed record OpeningFrame(MediaRef? Media, VideoLink? Video, string Caption);

public sealed record PostOpening(IReadOnlyList<OpeningFrame> Frames, int Blocks)
{
    public static PostOpening Of(RichText content)
    {
        List<OpeningFrame> frames = [];
        int blocks = 0;
        foreach (Block block in RichTextParser.Parse(content.Markdown).Blocks)
        {
            IReadOnlyList<OpeningFrame>? framed = FramesOf(content, block);
            if (framed is null)
                break;
            frames.AddRange(framed);
            blocks++;
        }

        return new PostOpening(frames, blocks);
    }

    private static IReadOnlyList<OpeningFrame>? FramesOf(RichText content, Block block) => block switch
    {
        GalleryBlock gallery => FramesOf(content, gallery.Items),
        MediaBlock single => FramesOf(content, [single]),
        EmbedBlock { Video.Site: VideoSite.YouTube } embed => [new OpeningFrame(null, embed.Video, embed.Title)],
        _ => null,
    };

    private static List<OpeningFrame>? FramesOf(RichText content, IEnumerable<MediaBlock> items)
    {
        List<OpeningFrame> frames = [];
        foreach (MediaBlock item in items)
        {
            if (Frame(content, item) is not { } frame)
                return null;
            frames.Add(frame);
        }

        return frames;
    }

    private static OpeningFrame? Frame(RichText content, MediaBlock block) =>
        content.MediaById(block.MediaId) is { } media && (media.IsImage || media.IsVideo)
            ? new OpeningFrame(media, null, block.Caption.Length > 0 ? block.Caption : block.Alt)
            : null;
}
