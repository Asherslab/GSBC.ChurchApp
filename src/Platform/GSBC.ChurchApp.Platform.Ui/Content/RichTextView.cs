using GSBC.ChurchApp.Platform.Contracts.Content;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace GSBC.ChurchApp.Platform.Ui.Content;

public sealed class RichTextView : ComponentBase
{
    private const int LowestHeading = 6;

    [Parameter, EditorRequired] public required RichText Content { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public string About { get; set; } = "";
    [Parameter] public bool SkipLeadingMedia { get; set; }
    [Parameter] public RichTextScope Scope { get; set; }
    [Parameter] public RenderFragment<string>? TextTemplate { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        RichTextDocument document = RichTextParser.Parse(Content.Markdown, Scope);
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", Class is null ? "rich-text" : $"rich-text {Class}");
        IReadOnlyList<Block> blocks = SkipLeadingMedia && document.Blocks is [MediaBlock or GalleryBlock or EmbedBlock, ..] ? document.Blocks.Skip(1).ToList() : document.Blocks;
        if (blocks.Any(block => block is CardSplitBlock))
            AddCards(builder, new RichTextDocument(blocks, [], [], [], []).Cards());
        else
            AddBlocks(builder, blocks);
        AddFootnotes(builder, document.Footnotes);
        builder.CloseElement();
    }

    private void AddCards(RenderTreeBuilder builder, IReadOnlyList<IReadOnlyList<Block>> cards)
    {
        foreach (IReadOnlyList<Block> card in cards)
        {
            builder.OpenElement(2, "section");
            builder.AddAttribute(3, "class", "rich-text-card");
            AddBlocks(builder, card);
            builder.CloseElement();
        }
    }

    private void AddBlocks(RenderTreeBuilder builder, IEnumerable<Block> blocks)
    {
        foreach (Block block in blocks)
            AddBlock(builder, block);
    }

    private void AddFootnotes(RenderTreeBuilder builder, IReadOnlyList<Footnote> footnotes)
    {
        if (footnotes.Count == 0)
            return;
        builder.OpenElement(4, "ol");
        builder.AddAttribute(5, "class", "rich-text-notes");
        builder.AddAttribute(6, "aria-label", "Notes");
        foreach (Footnote footnote in footnotes)
            AddElement(builder, "li", footnote.Content);
        builder.CloseElement();
    }

    private void AddBlock(RenderTreeBuilder builder, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AddElement(builder, $"h{Math.Min(heading.Level + 1, LowestHeading)}", heading.Content);
                break;
            case ParagraphBlock paragraph:
                AddElement(builder, "p", paragraph.Content);
                break;
            case ListBlock list:
                AddList(builder, list);
                break;
            case QuoteBlock quote:
                builder.OpenElement(10, "blockquote");
                foreach (ParagraphBlock paragraph in quote.Paragraphs)
                    AddElement(builder, "p", paragraph.Content);
                builder.CloseElement();
                break;
            case CodeBlock code:
                builder.OpenElement(11, "pre");
                builder.AddAttribute(12, "class", "rich-text-code");
                builder.OpenElement(13, "code");
                builder.AddContent(14, code.Text);
                builder.CloseElement();
                builder.CloseElement();
                break;
            case RuleBlock:
                builder.OpenElement(15, "hr");
                builder.CloseElement();
                break;
            case TableBlock table:
                AddTable(builder, table);
                break;
            case MediaBlock or GalleryBlock or EmbedBlock:
                builder.OpenComponent<RichTextMediaBlock>(30);
                builder.AddComponentParameter(31, nameof(RichTextMediaBlock.Block), block);
                builder.AddComponentParameter(32, nameof(RichTextMediaBlock.Content), Content);
                builder.AddComponentParameter(33, nameof(RichTextMediaBlock.About), About);
                builder.CloseComponent();
                break;
            case DirectiveBlock directive when AppCards.Of(directive) is { } card:
                builder.OpenComponent<AppCardBlock>(40);
                builder.AddComponentParameter(41, nameof(AppCardBlock.Card), card);
                builder.CloseComponent();
                break;
        }
    }

    private void AddTable(RenderTreeBuilder builder, TableBlock table)
    {
        builder.OpenElement(70, "div");
        builder.AddAttribute(71, "class", "rich-text-table");
        builder.OpenElement(72, "table");
        builder.OpenElement(73, "thead");
        AddRow(builder, table, table.Header, "th");
        builder.CloseElement();
        builder.OpenElement(74, "tbody");
        foreach (TableRow row in table.Rows)
            AddRow(builder, table, row, "td");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    private void AddRow(RenderTreeBuilder builder, TableBlock table, TableRow row, string cell)
    {
        builder.OpenElement(75, "tr");
        for (int column = 0; column < row.Cells.Count; column++)
        {
            builder.OpenElement(76, cell);
            if (table.Alignments[column] is not TableAlignment.None and var alignment)
                builder.AddAttribute(77, "data-align", alignment.ToString().ToLowerInvariant());
            AddInlines(builder, row.Cells[column]);
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private void AddList(RenderTreeBuilder builder, ListBlock list)
    {
        builder.OpenElement(20, list.Ordered ? "ol" : "ul");
        if (list.Ordered && list.Start != 1)
            builder.AddAttribute(21, "start", list.Start);
        foreach (ListItem item in list.Items)
        {
            builder.OpenElement(22, "li");
            AddInlines(builder, item.Content);
            if (item.Nested is { } nested)
                AddList(builder, nested);
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private void AddElement(RenderTreeBuilder builder, string element, IReadOnlyList<Inline> content)
    {
        builder.OpenElement(40, element);
        AddInlines(builder, content);
        builder.CloseElement();
    }

    private void AddInlines(RenderTreeBuilder builder, IReadOnlyList<Inline> inlines)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case TextRun text when TextTemplate is not null:
                    builder.AddContent(49, TextTemplate(text.Text));
                    break;
                case TextRun text:
                    builder.AddContent(50, text.Text);
                    break;
                case StrongRun strong:
                    AddElement(builder, "strong", strong.Content);
                    break;
                case EmphasisRun emphasis:
                    AddElement(builder, "em", emphasis.Content);
                    break;
                case StrikeRun strike:
                    AddElement(builder, "s", strike.Content);
                    break;
                case HighlightRun highlight:
                    AddElement(builder, "mark", highlight.Content);
                    break;
                case SubscriptRun subscript:
                    AddElement(builder, "sub", subscript.Content);
                    break;
                case SuperscriptRun superscript:
                    AddElement(builder, "sup", superscript.Content);
                    break;
                case CodeRun code:
                    builder.OpenElement(52, "code");
                    builder.AddContent(53, code.Text);
                    builder.CloseElement();
                    break;
                case FootnoteRun footnote:
                    builder.OpenElement(54, "sup");
                    builder.AddAttribute(55, "class", "rich-text-note-ref");
                    builder.AddAttribute(56, "aria-label", $"Note {footnote.Number}");
                    builder.AddContent(57, footnote.Number);
                    builder.CloseElement();
                    break;
                case LineBreakRun:
                    builder.OpenElement(51, "br");
                    builder.CloseElement();
                    break;
                case LinkRun link:
                    AddLink(builder, link);
                    break;
            }
        }
    }

    private void AddLink(RenderTreeBuilder builder, LinkRun link)
    {
        LinkKind kind = LinkTargets.KindOf(link.Href);
        if (kind == LinkKind.Unsafe)
        {
            AddInlines(builder, link.Content);
            return;
        }

        builder.OpenElement(60, "a");
        builder.AddAttribute(61, "href", kind == LinkKind.InApp ? LinkTargets.InAppRoute(link.Href) : link.Href);
        if (kind == LinkKind.Web)
        {
            builder.AddAttribute(62, "target", "_blank");
            builder.AddAttribute(63, "rel", "noopener noreferrer");
        }
        AddInlines(builder, link.Content);
        builder.CloseElement();
    }
}
