using GSBC.ChurchApp.Platform.Contracts.Content;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace GSBC.ChurchApp.Platform.Ui.Content;

public sealed class RichTextView : ComponentBase
{
    [Parameter, EditorRequired] public required RichText Content { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public string About { get; set; } = "";
    [Parameter] public bool SkipLeadingMedia { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        RichTextDocument document = RichTextParser.Parse(Content.Markdown);
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", Class is null ? "rich-text" : $"rich-text {Class}");
        IEnumerable<Block> blocks = SkipLeadingMedia && document.Blocks is [MediaBlock or GalleryBlock or EmbedBlock, ..] ? document.Blocks.Skip(1) : document.Blocks;
        foreach (Block block in blocks)
            AddBlock(builder, block);
        builder.CloseElement();
    }

    private void AddBlock(RenderTreeBuilder builder, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AddElement(builder, heading.Level == 2 ? "h2" : "h3", heading.Content);
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
            case MediaBlock or GalleryBlock or EmbedBlock:
                builder.OpenComponent<RichTextMediaBlock>(30);
                builder.AddComponentParameter(31, nameof(RichTextMediaBlock.Block), block);
                builder.AddComponentParameter(32, nameof(RichTextMediaBlock.Content), Content);
                builder.AddComponentParameter(33, nameof(RichTextMediaBlock.About), About);
                builder.CloseComponent();
                break;
        }
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

    private static void AddElement(RenderTreeBuilder builder, string element, IReadOnlyList<Inline> content)
    {
        builder.OpenElement(40, element);
        AddInlines(builder, content);
        builder.CloseElement();
    }

    private static void AddInlines(RenderTreeBuilder builder, IReadOnlyList<Inline> inlines)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case TextRun text:
                    builder.AddContent(50, text.Text);
                    break;
                case StrongRun strong:
                    AddElement(builder, "strong", strong.Content);
                    break;
                case EmphasisRun emphasis:
                    AddElement(builder, "em", emphasis.Content);
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

    private static void AddLink(RenderTreeBuilder builder, LinkRun link)
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
