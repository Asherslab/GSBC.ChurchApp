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
        Assert.Contains("isn't an image", Assert.Single(RichTextRules.ProblemsWith(new RichText { Markdown = "![Notes](media:notes)", Media = [notes] })));
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
            # not a heading, \*not italic\*
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
            block => Assert.Equal(("hall", "The hall"), (Assert.IsType<ImageBlock>(block).MediaId, ((ImageBlock)block).Alt)),
            block => Assert.Equal("# not a heading, *not italic*", Assert.IsType<TextRun>(Assert.Single(Assert.IsType<ParagraphBlock>(block).Content)).Text));
    }

    [Fact]
    public void TextThatNeverClosesItsMarksStillParsesQuickly()
    {
        string hostile = string.Concat(Enumerable.Repeat("[*_**(", 40_000));

        Stopwatch watch = Stopwatch.StartNew();
        RichTextParser.Parse(hostile);

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"Took {watch.Elapsed}.");
    }
}
