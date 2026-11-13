namespace GSBC.ChurchApp.Platform.Contracts.Content;

public sealed record RichTextDocument(
    IReadOnlyList<Block> Blocks, IReadOnlyList<string> Links, IReadOnlyList<string> MediaIds, IReadOnlyList<VideoLink> Videos, IReadOnlyList<string> Problems)
{
    public IReadOnlyList<Footnote> Footnotes { get; init; } = [];

    public IReadOnlyList<DirectiveBlock> Directives => Blocks.OfType<DirectiveBlock>().ToList();

    public IReadOnlyList<IReadOnlyList<Block>> Cards()
    {
        List<IReadOnlyList<Block>> cards = [];
        List<Block> current = [];
        foreach (Block block in Blocks)
        {
            if (block is CardSplitBlock)
            {
                if (current.Count > 0)
                    cards.Add(current);
                current = [];
                continue;
            }
            current.Add(block);
        }
        if (current.Count > 0)
            cards.Add(current);
        return cards;
    }
}

public abstract record Block;

public sealed record HeadingBlock(int Level, IReadOnlyList<Inline> Content) : Block;

public sealed record ParagraphBlock(IReadOnlyList<Inline> Content) : Block;

public sealed record ListBlock(bool Ordered, int Start, IReadOnlyList<ListItem> Items) : Block;

public sealed record ListItem(IReadOnlyList<Inline> Content, ListBlock? Nested);

public sealed record QuoteBlock(IReadOnlyList<ParagraphBlock> Paragraphs) : Block;

public sealed record MediaBlock(string MediaId, string Alt, string Caption = "") : Block;

public sealed record GalleryBlock(IReadOnlyList<MediaBlock> Items) : Block;

public sealed record EmbedBlock(VideoLink Video, string Title) : Block;

public sealed record CodeBlock(string Text) : Block;

public sealed record RuleBlock : Block;

public sealed record CardSplitBlock : Block;

public enum TableAlignment
{
    None,
    Left,
    Centre,
    Right,
}

public sealed record TableBlock(IReadOnlyList<TableAlignment> Alignments, TableRow Header, IReadOnlyList<TableRow> Rows) : Block;

public sealed record TableRow(IReadOnlyList<IReadOnlyList<Inline>> Cells);

public sealed record DirectiveBlock(string Name, IReadOnlyDictionary<string, string> Attributes) : Block;

public sealed record Footnote(string Label, int Number, IReadOnlyList<Inline> Content);

public abstract record Inline;

public sealed record TextRun(string Text) : Inline;

public sealed record StrongRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record EmphasisRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record StrikeRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record HighlightRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record SubscriptRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record SuperscriptRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record CodeRun(string Text) : Inline;

public sealed record FootnoteRun(string Label, int Number) : Inline;

public sealed record LinkRun(string Href, IReadOnlyList<Inline> Content) : Inline;

public sealed record LineBreakRun : Inline;
