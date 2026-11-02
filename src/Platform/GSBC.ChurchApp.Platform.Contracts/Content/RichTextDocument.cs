namespace GSBC.ChurchApp.Platform.Contracts.Content;

public sealed record RichTextDocument(IReadOnlyList<Block> Blocks, IReadOnlyList<string> Links, IReadOnlyList<string> ImageIds, IReadOnlyList<string> Problems);

public abstract record Block;

public sealed record HeadingBlock(int Level, IReadOnlyList<Inline> Content) : Block;

public sealed record ParagraphBlock(IReadOnlyList<Inline> Content) : Block;

public sealed record ListBlock(bool Ordered, int Start, IReadOnlyList<ListItem> Items) : Block;

public sealed record ListItem(IReadOnlyList<Inline> Content, ListBlock? Nested);

public sealed record QuoteBlock(IReadOnlyList<ParagraphBlock> Paragraphs) : Block;

public sealed record ImageBlock(string MediaId, string Alt) : Block;

public abstract record Inline;

public sealed record TextRun(string Text) : Inline;

public sealed record StrongRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record EmphasisRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record LinkRun(string Href, IReadOnlyList<Inline> Content) : Inline;

public sealed record LineBreakRun : Inline;
