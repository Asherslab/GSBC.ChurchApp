using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class PostBlocksTests
{
    private static readonly MediaRef Choir = Media("choir", "image/jpeg", "Choir");
    private static readonly MediaRef Lawn = Media("lawn", "image/jpeg", "Lawn");
    private static readonly MediaRef Talk = Media("talk", "audio/mpeg", "The talk");
    private static readonly MediaRef Form = Media("form", "application/pdf", "Form");

    [Fact]
    public void APostReadsBackAsTheBlocksItWasWrittenIn()
    {
        List<PostBlock> written =
        [
            new() { Kind = PostBlockKind.Text, Text = "Christmas is close.\n\n- Carols\n- Eve" },
            new() { Kind = PostBlockKind.Heading, Text = "Carols on the lawn" },
            new() { Kind = PostBlockKind.Pictures, Media = [Choir, Lawn], Caption = "Last year's carols" },
            new() { Kind = PostBlockKind.Heading, Text = "Times", Smaller = true },
            new() { Kind = PostBlockKind.VideoLink, Link = "https://youtu.be/dQw4w9WgXcQ", Caption = "A message from our pastor" },
            new() { Kind = PostBlockKind.Audio, Media = [Talk], Caption = "Saturday night" },
            new() { Kind = PostBlockKind.File, Media = [Form] },
        ];

        RichText content = PostBlocks.ToRichText(written);
        List<PostBlock> read = PostBlocks.Of(content);

        Assert.Empty(RichTextRules.ProblemsWith(content, allowed: RichTextMedia.Posts));
        Assert.Equal(written.Select(Shape), read.Select(Shape));
        Assert.Equal(content.Markdown, PostBlocks.ToRichText(read).Markdown);
        Assert.Single(RichTextParser.Parse(content.Markdown).Blocks.OfType<GalleryBlock>());
    }

    [Fact]
    public void TwoPicturesWithABlankLineBetweenStayTwoBlocks()
    {
        RichText content = new() { Markdown = "![Choir](media:choir)\n\n![Lawn](media:lawn)", Media = [Choir, Lawn] };

        Assert.Equal([PostBlockKind.Pictures, PostBlockKind.Pictures], PostBlocks.Of(content).Select(block => block.Kind));
    }

    [Fact]
    public void APostWrittenBeforeBlocksOpensAsOneTextBlock()
    {
        RichText old = PlainText.AsRichText("Pick up by 8:30.\n1. Not a list\n## Not a heading");

        PostBlock only = Assert.Single(PostBlocks.Of(old));

        Assert.Equal(PostBlockKind.Text, only.Kind);
        Assert.Equal(old.Markdown, PostBlocks.ToRichText([only]).Markdown);
    }

    [Fact]
    public void EmptyBlocksAreLeftOutAndCaptionsCannotBreakTheLine()
    {
        RichText content = PostBlocks.ToRichText(
        [
            new() { Kind = PostBlockKind.Text, Text = "  " },
            new() { Kind = PostBlockKind.Pictures, Media = [Choir], Caption = "Say \"cheese\")\nplease" },
        ]);

        MediaBlock picture = Assert.IsType<MediaBlock>(Assert.Single(RichTextParser.Parse(content.Markdown).Blocks));
        Assert.Equal("Say 'cheese') please", picture.Caption);
    }

    private static (PostBlockKind, string, string, bool, string) Shape(PostBlock block) =>
        (block.Kind, block.Text, block.Caption, block.Smaller, string.Join(",", block.Media.Select(media => media.Id)) + VideoLink.Of(block.Link)?.Id);

    private static MediaRef Media(string id, string contentType, string alt) => new() { Id = id, ContentType = contentType, AltText = alt, FileName = id };
}
