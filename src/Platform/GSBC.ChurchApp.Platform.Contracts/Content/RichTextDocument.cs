namespace GSBC.ChurchApp.Platform.Contracts.Content;

public sealed record RichTextDocument(
    IReadOnlyList<Block> Blocks, IReadOnlyList<string> Links, IReadOnlyList<string> MediaIds, IReadOnlyList<VideoLink> Videos, IReadOnlyList<string> Problems);

public abstract record Block;

public sealed record HeadingBlock(int Level, IReadOnlyList<Inline> Content) : Block;

public sealed record ParagraphBlock(IReadOnlyList<Inline> Content) : Block;

public sealed record ListBlock(bool Ordered, int Start, IReadOnlyList<ListItem> Items) : Block;

public sealed record ListItem(IReadOnlyList<Inline> Content, ListBlock? Nested);

public sealed record QuoteBlock(IReadOnlyList<ParagraphBlock> Paragraphs) : Block;

public sealed record MediaBlock(string MediaId, string Alt, string Caption = "") : Block;

public sealed record GalleryBlock(IReadOnlyList<MediaBlock> Items) : Block;

public sealed record EmbedBlock(VideoLink Video, string Title) : Block;

public abstract record Inline;

public sealed record TextRun(string Text) : Inline;

public sealed record StrongRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record EmphasisRun(IReadOnlyList<Inline> Content) : Inline;

public sealed record LinkRun(string Href, IReadOnlyList<Inline> Content) : Inline;

public sealed record LineBreakRun : Inline;
