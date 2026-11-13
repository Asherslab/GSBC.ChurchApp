using System.Diagnostics;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class RichTextTests
{
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("vbscript:msgbox")]
    [InlineData("http://example.org")]
    [InlineData("//evil.example/page")]
    [InlineData("/\\evil.example")]
    [InlineData("https:evil.example")]
    [InlineData("file:///etc/passwd")]
    public void LinksTheAppCannotSafelyOpenAreRefused(string href)
    {
        IReadOnlyList<string> problems = RichTextRules.ProblemsWith(new RichText { Markdown = $"See [this]({href})." });

        Assert.Contains(problems, problem => problem.Contains("isn't a link the app can open"));
    }

    [Theory]
    [InlineData("https://gsbc.org.au/visit")]
    [InlineData("mailto:office@example.org")]
    [InlineData("tel:+61733334444")]
    [InlineData("/events/12")]
    public void WebEmailPhoneAndInAppLinksAreAllowed(string href) =>
        Assert.Empty(RichTextRules.ProblemsWith(new RichText { Markdown = $"See [this]({href})." }));

    [Fact]
    public void HtmlIsOnlyEverText()
    {
        RichTextDocument document = RichTextParser.Parse("<script>alert(1)</script> <img src=x onerror=alert(1)>");

        ParagraphBlock paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(document.Blocks));
        TextRun text = Assert.IsType<TextRun>(Assert.Single(paragraph.Content));
        Assert.Equal("<script>alert(1)</script> <img src=x onerror=alert(1)>", text.Text);
    }

    [Fact]
    public void ImagesMustBeImagesFromTheTextsOwnMediaOnTheirOwnLine()
    {
        MediaRef photo = new() { Id = "photo", ContentType = "image/jpeg" };
        MediaRef notes = new() { Id = "notes", ContentType = "application/pdf", FileName = "notes.pdf" };

        Assert.Empty(RichTextRules.ProblemsWith(new RichText { Markdown = "![The hall](media:photo)", Media = [photo] }));
        Assert.Contains("isn't in the media library", Assert.Single(RichTextRules.ProblemsWith(new RichText { Markdown = "![The hall](media:other)", Media = [photo] })));
        Assert.Contains("isn't a picture", Assert.Single(RichTextRules.ProblemsWith(new RichText { Markdown = "![Notes](media:notes)", Media = [notes] })));
        Assert.Contains("on a line of its own", Assert.Single(RichTextRules.ProblemsWith(new RichText { Markdown = "Look ![x](https://tracker.example/p.gif) here" })));
    }

    [Fact]
    public void TheSubsetIsHeadingsEmphasisListsQuotesLinksAndImages()
    {
        RichTextDocument document = RichTextParser.Parse("""
            ## Sunday lunch
            Bring a **plate to _share_**, see the [roster](/ministries/12/roster).
            Second line

            - Mains
              1. Surnames A–M
            - Desserts

            > Taste and see
            >
            > that the Lord is good.

            ![The hall](media:hall)
            #not a heading, \*not italic\*
            """);

        Assert.Collection(document.Blocks,
            block => Assert.Equal(2, Assert.IsType<HeadingBlock>(block).Level),
            block => Assert.Collection(Assert.IsType<ParagraphBlock>(block).Content,
                inline => Assert.Equal("Bring a ", Assert.IsType<TextRun>(inline).Text),
                inline => Assert.IsType<EmphasisRun>(Assert.IsType<StrongRun>(inline).Content[1]),
                inline => Assert.Equal(", see the ", Assert.IsType<TextRun>(inline).Text),
                inline => Assert.Equal("/ministries/12/roster", Assert.IsType<LinkRun>(inline).Href),
                inline => Assert.Equal(".", Assert.IsType<TextRun>(inline).Text),
                inline => Assert.IsType<LineBreakRun>(inline),
                inline => Assert.Equal("Second line", Assert.IsType<TextRun>(inline).Text)),
            block =>
            {
                ListBlock list = Assert.IsType<ListBlock>(block);
                Assert.False(list.Ordered);
                Assert.Equal(2, list.Items.Count);
                Assert.True(list.Items[0].Nested?.Ordered);
            },
            block => Assert.Equal(2, Assert.IsType<QuoteBlock>(block).Paragraphs.Count),
            block => Assert.Equal(("hall", "The hall"), (Assert.IsType<MediaBlock>(block).MediaId, ((MediaBlock)block).Alt)),
            block => Assert.Equal("#not a heading, *not italic*", Assert.IsType<TextRun>(Assert.Single(Assert.IsType<ParagraphBlock>(block).Content)).Text));
    }

    [Fact]
    public void PostsCarryCaptionedPicturesGalleriesVideoAudioFilesAndVideoLinks()
    {
        RichText post = new()
        {
            Markdown = """
                ![The choir](media:choir "Carols on the lawn")
                ![The lawn](media:lawn)
                ![Candles](media:candles "Lit at 7pm")

                ![Camp highlights](media:camp)

                ![The talk](media:talk "Saturday night")

                ![Registration form](media:form)

                ![Christmas Eve](https://youtu.be/dQw4w9WgXcQ)
                """,
            Media = [Picture("choir"), Picture("lawn"), Picture("candles"), Kind("camp", "video/mp4"), Kind("talk", "audio/mpeg"), Kind("form", "application/pdf")],
        };

        RichTextDocument document = RichTextParser.Parse(post.Markdown);

        Assert.Empty(RichTextRules.ProblemsWith(post, allowed: RichTextMedia.Posts));
        Assert.Collection(document.Blocks,
            block => Assert.Equal(
                [("choir", "Carols on the lawn"), ("lawn", ""), ("candles", "Lit at 7pm")],
                Assert.IsType<GalleryBlock>(block).Items.Select(item => (item.MediaId, item.Caption))),
            block => Assert.Equal("camp", Assert.IsType<MediaBlock>(block).MediaId),
            block => Assert.Equal("Saturday night", Assert.IsType<MediaBlock>(block).Caption),
            block => Assert.Equal("Registration form", Assert.IsType<MediaBlock>(block).Alt),
            block => Assert.Equal(new VideoLink(VideoSite.YouTube, "dQw4w9WgXcQ"), Assert.IsType<EmbedBlock>(block).Video));
    }

    [Fact]
    public void OnlyPicturesGoWhereOnlyPicturesAreAllowed()
    {
        RichText withVideo = new() { Markdown = "![Camp](media:camp)\n\n![Eve](https://vimeo.com/76979871)", Media = [Kind("camp", "video/mp4")] };

        IReadOnlyList<string> problems = RichTextRules.ProblemsWith(withVideo);

        Assert.Contains(problems, problem => problem.Contains("isn't a picture"));
        Assert.Contains(problems, problem => problem.Contains("Videos can't be shown in here"));
    }

    [Theory]
    [InlineData("https://youtube.com.evil.example/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ\"><script>")]
    [InlineData("https://vimeo.com/76979871/../../evil")]
    [InlineData("https://tracker.example/p.gif")]
    public void OnlyYouTubeAndVimeoLinksBecomePlayers(string link)
    {
        RichTextDocument document = RichTextParser.Parse($"![Video]({link})");

        Assert.Empty(document.Videos);
        Assert.Contains(document.Problems, problem => problem.Contains("on a line of its own"));
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ?rel=0&playsinline=1")]
    [InlineData("https://m.youtube.com/shorts/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ?rel=0&playsinline=1")]
    [InlineData("https://vimeo.com/76979871", "https://player.vimeo.com/video/76979871?dnt=1")]
    public void APlayerIsBuiltFromTheVideosIdOnly(string link, string player) =>
        Assert.Equal(player, VideoLink.Of(link)?.PlayerUrl);

    private static MediaRef Picture(string id) => Kind(id, "image/jpeg");

    private static MediaRef Kind(string id, string contentType) => new() { Id = id, ContentType = contentType, FileName = id };

    [Fact]
    public void TextThatNeverClosesItsMarksStillParsesQuickly()
    {
        string hostile = string.Concat(Enumerable.Repeat("[*_**(", 40_000));

        Stopwatch watch = Stopwatch.StartNew();
        RichTextParser.Parse(hostile);

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"Took {watch.Elapsed}.");
    }
}
