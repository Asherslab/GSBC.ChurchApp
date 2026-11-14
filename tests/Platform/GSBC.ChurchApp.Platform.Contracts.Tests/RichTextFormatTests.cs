using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class RichTextFormatTests
{
    [Theory]
    [InlineData("***both***")]
    [InlineData("___both___")]
    [InlineData("**_both_**")]
    [InlineData("*__both__*")]
    public void BoldItalicIsBothWithNoStrayMarks(string markdown)
    {
        Inline outer = Assert.Single(Only(markdown));

        Inline inner = Assert.Single(outer switch
        {
            StrongRun strong => strong.Content,
            EmphasisRun emphasis => emphasis.Content,
            _ => throw new InvalidOperationException(outer.ToString()),
        });
        Assert.True(outer is StrongRun && inner is EmphasisRun || outer is EmphasisRun && inner is StrongRun);
        Assert.Equal("both", Text(inner));
    }

    [Fact]
    public void MarksNestInsideEachOtherWhicheverClosesFirst()
    {
        EmphasisRun italic = Assert.IsType<EmphasisRun>(Assert.Single(Only("***bold** then italic*")));
        Assert.Equal("bold", Text(Assert.IsType<StrongRun>(italic.Content[0])));
        Assert.Equal(" then italic", Text(italic.Content[1]));

        StrongRun bold = Assert.IsType<StrongRun>(Assert.Single(Only("**bold *italic***")));
        Assert.Equal("italic", Text(Assert.IsType<EmphasisRun>(bold.Content[1])));
    }

    [Fact]
    public void StrikethroughHighlightCodeSubscriptAndSuperscript()
    {
        IReadOnlyList<Inline> inlines = Only("~~gone~~ ==look== `a*b*` H~2~O x^2^");

        Assert.Equal("gone", Text(Assert.IsType<StrikeRun>(inlines[0])));
        Assert.Equal("look", Text(Assert.IsType<HighlightRun>(inlines[2])));
        Assert.Equal("a*b*", Assert.IsType<CodeRun>(inlines[4]).Text);
        Assert.Equal("2", Text(Assert.IsType<SubscriptRun>(inlines[6])));
        Assert.Equal("2", Text(Assert.IsType<SuperscriptRun>(inlines[8])));
    }

    [Theory]
    [InlineData("about ~5 to ~10 people")]
    [InlineData("a == b")]
    [InlineData("2 ^ 3")]
    [InlineData("snake_case_name")]
    public void LoneMarksStayText(string markdown) => Assert.Equal(markdown, Text(Assert.Single(Only(markdown))));

    [Fact]
    public void HeadingsGetSmallerWithEveryHash()
    {
        RichTextDocument document = RichTextParser.Parse("# One\n## Two\n### Three #\n#Not one");

        Assert.Equal([1, 2, 3], document.Blocks.OfType<HeadingBlock>().Select(heading => heading.Level));
        Assert.Equal("Three", Text(document.Blocks.OfType<HeadingBlock>().Last().Content[0]));
        Assert.IsType<ParagraphBlock>(document.Blocks[^1]);
    }

    [Fact]
    public void FencedCodeKeepsItsTextAsIs()
    {
        RichTextDocument document = RichTextParser.Parse("```text\n**not bold**\n  <b>kept</b>\n```\nAfter");

        Assert.Equal("**not bold**\n  <b>kept</b>", Assert.IsType<CodeBlock>(document.Blocks[0]).Text);
        Assert.IsType<ParagraphBlock>(document.Blocks[1]);
    }

    [Theory]
    [InlineData("---")]
    [InlineData("***")]
    [InlineData("_ _ _")]
    public void ALineAcrossIsThreeMarksOnTheirOwn(string line) =>
        Assert.IsType<RuleBlock>(RichTextParser.Parse($"Above\n{line}\nBelow").Blocks[1]);

    [Fact]
    public void ACardSplitStartsABlockAndNeverUnderlinesText()
    {
        RichTextDocument split = RichTextParser.Parse("First part\n\n===\n\nSecond part");
        RichTextDocument underline = RichTextParser.Parse("Not a split\n===");
        RichTextDocument afterHeading = RichTextParser.Parse("## Part one\n===\nPart two");

        Assert.IsType<CardSplitBlock>(split.Blocks[1]);
        Assert.Equal(2, split.Cards().Count);
        Assert.Single(underline.Cards());
        Assert.Equal(2, afterHeading.Cards().Count);
        Assert.Equal("===", Text(Assert.IsType<ParagraphBlock>(Assert.Single(underline.Blocks)).Content[^1]));
    }

    [Fact]
    public void ANotificationLineComesFromTheFirstCardWithText()
    {
        RichText post = new() { Markdown = "![Lawn](media:lawn)\n\n===\n\nCarols at 6pm.\n\n===\n\nParking on the street." };

        Assert.Equal("Carols at 6pm.", PlainText.Opening(post));
        Assert.Equal("Carols at 6pm. Parking on the street.", PlainText.Of(post));
    }

    [Fact]
    public void TablesHaveAHeaderAlignmentsAndRows()
    {
        TableBlock table = Assert.IsType<TableBlock>(Assert.Single(RichTextParser.Parse("""
            | Service | Time | Room |
            |:--------|:----:|-----:|
            | **Carols** | 6pm | Lawn \| oval |
            | Eve | 7pm |
            """).Blocks));

        Assert.Equal([TableAlignment.Left, TableAlignment.Centre, TableAlignment.Right], table.Alignments);
        Assert.Equal("Service", Text(table.Header.Cells[0][0]));
        Assert.IsType<StrongRun>(table.Rows[0].Cells[0][0]);
        Assert.Equal("Lawn | oval", Text(table.Rows[0].Cells[2][0]));
        Assert.Empty(table.Rows[1].Cells[2]);
    }

    [Fact]
    public void ALineWithABarIsTextWithoutADelimiterRow() =>
        Assert.IsType<ParagraphBlock>(Assert.Single(RichTextParser.Parse("Bring a plate | or two").Blocks));

    [Fact]
    public void FootnotesAreNumberedInTheOrderTheyAreUsed()
    {
        RichTextDocument document = RichTextParser.Parse("Gloves[^gloves] and rugs[^rugs], gloves again[^gloves], none[^missing].\n\n[^rugs]: In the hall.\n[^gloves]: In the shed\n  by the hall.");

        IReadOnlyList<Inline> inlines = Assert.IsType<ParagraphBlock>(Assert.Single(document.Blocks)).Content;
        Assert.Equal([1, 2, 1], inlines.OfType<FootnoteRun>().Select(note => note.Number));
        Assert.Contains(inlines, inline => inline is TextRun { Text: var text } && text.Contains("[^missing]"));
        Assert.Equal(["gloves", "rugs"], document.Footnotes.Select(note => note.Label));
        Assert.IsType<LineBreakRun>(document.Footnotes[0].Content[1]);
    }

    [Fact]
    public void EmojiShortcodesBecomeEmojiAndUnknownOnesStayText() =>
        Assert.Equal("Thanks 🙏 :not-a-code: 10:30", Text(Assert.Single(Only("Thanks :pray: :not-a-code: 10:30"))));

    [Theory]
    [InlineData("::event{id=4f1c2d3e4f1c2d3e4f1c2d3e4f1c2d3e}", AppCardKind.Event, EventShows.OneDate)]
    [InlineData("::event{id=4f1c2d3e4f1c2d3e4f1c2d3e4f1c2d3e-2026-12-20}", AppCardKind.Event, EventShows.OneDate)]
    [InlineData("::event{id=4f1c2d3e4f1c2d3e4f1c2d3e4f1c2d3e show=next}", AppCardKind.Event, EventShows.NextDate)]
    [InlineData("::event{id=4f1c2d3e4f1c2d3e4f1c2d3e4f1c2d3e show=pattern}", AppCardKind.Event, EventShows.Pattern)]
    [InlineData("::event{id=4f1c2d3e4f1c2d3e4f1c2d3e4f1c2d3e show=next look=box}", AppCardKind.Event, EventShows.NextDate)]
    [InlineData("::sermon{id=8a1b look=line}", AppCardKind.Sermon, EventShows.OneDate)]
    [InlineData("::ministry{id=welcome-team}", AppCardKind.Ministry, EventShows.OneDate)]
    [InlineData("::lifegroup{id=0193a1b2c3d4}", AppCardKind.LifeGroup, EventShows.OneDate)]
    public void AnAppCardReadsBackAsTheLineItWasWrittenAs(string line, AppCardKind kind, EventShows shows)
    {
        AppCard card = Assert.Single(RichTextParser.Parse(line).AppCards);

        Assert.Equal((kind, shows, line), (card.Kind, card.Shows, card.Directive));
        Assert.Empty(RichTextRules.ProblemsWith(new RichText { Markdown = line }, allowed: RichTextMedia.Posts));
    }

    [Theory]
    [InlineData("::event{}")]
    [InlineData("::event{id=\"a b\"}")]
    [InlineData("::sermon{id=8a1b show=next}")]
    [InlineData("::event{id=8a1b show=always}")]
    [InlineData("::ministry{id=8a1b onclick=x}")]
    [InlineData("::ministry{id=8a1b look=huge}")]
    public void AnAppCardThatLostWhatItPointsToIsRefused(string line) =>
        Assert.Contains("lost what it points to", Assert.Single(RichTextRules.ProblemsWith(new RichText { Markdown = line }, allowed: RichTextMedia.Posts)));

    [Fact]
    public void AppCardsGoOnlyWhereThePlaceAllowsThemAndOtherDirectivesNowhere()
    {
        Assert.Contains("can't go in here", Assert.Single(RichTextRules.ProblemsWith(new RichText { Markdown = "::event{id=8a1b}" }, allowed: RichTextMedia.Pictures | RichTextMedia.Layout)));
        Assert.Contains("isn't something the app can show", Assert.Single(RichTextRules.ProblemsWith(new RichText { Markdown = "::script{src=x}" }, allowed: RichTextMedia.Posts)));
    }

    [Fact]
    public void AnAppCardIsNeverPartOfAPostsPlainText() =>
        Assert.Equal("Come along.", PlainText.Of(new RichText { Markdown = "::ministry{id=hidden-team}\n\nCome along." }));

    [Fact]
    public void AVideoLinkCanCarryACaption()
    {
        EmbedBlock embed = Assert.IsType<EmbedBlock>(Assert.Single(RichTextParser.Parse("![Christmas Eve](https://youtu.be/dQw4w9WgXcQ \"From last year\")").Blocks));

        Assert.Equal("From last year", embed.Title);
    }

    [Fact]
    public void ANumberedListCanEndWithAnEmptyItem()
    {
        ListBlock list = Assert.IsType<ListBlock>(Assert.Single(RichTextParser.Parse("1. and number points\n2.").Blocks));

        Assert.True(list.Ordered);
        Assert.Equal(2, list.Items.Count);
    }

    [Fact]
    public void PlainTextIsEscapedSoItNeverFormats()
    {
        const string typed = "=== ~~a~~ ==b== `c` x^2^ :pray: | d |";

        Assert.Equal(typed, PlainText.Of(PlainText.AsRichText(typed)));
    }

    [Fact]
    public void ChatsFormatTextButKeepHeadingsTablesCardsAndPicturesAsTyped()
    {
        RichTextDocument chat = RichTextParser.Parse("# Not a heading\n\n===\n\n| a | b |\n|---|---|\n\n**bold** and `code`\n\n- one\n- two\n\n![x](media:abc)", RichTextScope.Chat);

        Assert.All(chat.Blocks, block => Assert.True(block is ParagraphBlock or ListBlock, block.GetType().Name));
        Assert.Equal("# Not a heading", Text(Assert.IsType<ParagraphBlock>(chat.Blocks[0]).Content[0]));
        Assert.Contains(chat.Blocks, block => block is ParagraphBlock { Content: [StrongRun, ..] });
        Assert.Empty(chat.MediaIds);
        Assert.Empty(chat.Problems);
    }

    [Fact]
    public void AChatMessageWithALinkTheAppCannotOpenIsRefused()
    {
        Assert.Contains("isn't a link the app can open", Assert.Single(RichTextRules.ChatProblems("See [this](javascript:alert(1))")));
        Assert.Empty(RichTextRules.ChatProblems("See [the roster](/ministries/12) and **bring a plate**"));
        Assert.Equal("See the roster and bring a plate", PlainText.OfChat("See [the roster](/ministries/12) and **bring a plate**"));
    }

    private static IReadOnlyList<Inline> Only(string markdown) =>
        Assert.IsType<ParagraphBlock>(Assert.Single(RichTextParser.Parse(markdown).Blocks)).Content;

    private static string Text(Inline inline) => inline switch
    {
        TextRun text => text.Text,
        StrongRun strong => string.Concat(strong.Content.Select(Text)),
        EmphasisRun emphasis => string.Concat(emphasis.Content.Select(Text)),
        StrikeRun strike => string.Concat(strike.Content.Select(Text)),
        HighlightRun highlight => string.Concat(highlight.Content.Select(Text)),
        SubscriptRun subscript => string.Concat(subscript.Content.Select(Text)),
        SuperscriptRun superscript => string.Concat(superscript.Content.Select(Text)),
        _ => "",
    };
}
