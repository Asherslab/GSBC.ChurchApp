using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class PastedTextTests
{
    private static PastedNode Text(string text) => new("t", Text: text);

    private static PastedNode Node(string kind, params PastedNode[] children) => new(kind, Children: children);

    private static PastedNode Link(string href, string text) => new("a", Href: href, Children: [Text(text)]);

    private static string Pasted(bool headings, params PastedNode[] blocks) => PastedText.ToMarkdown(Node("p", blocks), headings);

    [Fact]
    public void BoldItalicAndLinksFromAnotherDocumentStayFormatting()
    {
        string markdown = Pasted(false, Node("p", Node("b", Text("Bring a friend")), Text(" to "), Node("i", Text("carols")), Text(" at "), Link("https://example.org/carols", "the lawn")));

        Assert.Equal("**Bring a friend** to *carols* at [the lawn](https://example.org/carols)", markdown);
    }

    [Fact]
    public void WordsListParagraphsBecomeAListWithoutTheirBulletCharacters()
    {
        PastedNode item(string text) => Node("li", new PastedNode("marker", Text: "·"), Text(text));

        Assert.Equal("- Carols\n- Christmas Eve", Pasted(false, item("Carols"), item("Christmas Eve")));
    }

    [Fact]
    public void NumberedListsCountFromOne() =>
        Assert.Equal("1. First\n2. Second", Pasted(false, Node("ol", Node("li", Text("First")), Node("li", Text("Second")))));

    [Fact]
    public void ALinkTheAppCannotOpenKeepsOnlyItsWords() =>
        Assert.Equal("Click me", Pasted(false, Node("p", Link("javascript:alert(1)", "Click me"))));

    [Fact]
    public void SymbolsInPastedTextNeverTurnIntoFormatting()
    {
        string markdown = Pasted(false, Node("p", Text("Use *stars* and [brackets]")), Node("p", Text("# not a heading")), Node("p", Text("1. not a list")));

        RichTextDocument document = RichTextParser.Parse(markdown);
        Assert.All(document.Blocks, block => Assert.IsType<ParagraphBlock>(block));
        Assert.All(document.Blocks.Cast<ParagraphBlock>(), paragraph => Assert.All(paragraph.Content, run => Assert.IsType<TextRun>(run)));
        Assert.Equal(["Use *stars* and [brackets]", "# not a heading", "1. not a list"],
            document.Blocks.Cast<ParagraphBlock>().Select(paragraph => string.Concat(paragraph.Content.Cast<TextRun>().Select(run => run.Text))));
    }

    [Fact]
    public void HeadingsStayHeadingsOnlyWhereThePlaceAllowsThem()
    {
        PastedNode heading = Node("h", Text("Christmas"));

        Assert.Equal("## Christmas", Pasted(true, heading));
        Assert.Equal("**Christmas**", Pasted(false, heading));
    }

    [Fact]
    public void WhatTheScriptOnThePageSendsIsRead() =>
        Assert.Equal("**Hi** there", PastedText.FromJson("""{"k":"p","c":[{"k":"p","c":[{"k":"b","c":[{"k":"t","t":"Hi"}]},{"k":"t","t":" there"}]}]}""", headings: false));
}
