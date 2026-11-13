using System.Text;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public static class PlainText
{
    private const string Marks = "\\*_[]()!#>-+`~=^|:";

    public static string Of(RichText content) => TextOf(RichTextParser.Parse(content.Markdown).Blocks);

    public static string OfChat(string text) => TextOf(RichTextParser.Parse(text, RichTextScope.Chat).Blocks);

    public static string Opening(RichText content) =>
        RichTextParser.Parse(content.Markdown).Cards().Select(TextOf).FirstOrDefault(text => text.Length > 0) ?? "";

    private static string TextOf(IEnumerable<Block> blocks)
    {
        StringBuilder text = new();
        foreach (Block block in blocks)
            AddBlock(text, block);
        return text.ToString().Trim();
    }

    public static RichText AsRichText(string text) => new() { Markdown = Escaped(text) };

    public static string Escaped(string text)
    {
        StringBuilder markdown = new(text.Length + 16);
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        foreach ((string line, int number) in lines.Select((line, number) => (line, number)))
        {
            if (number > 0)
                markdown.Append('\n');
            string trimmed = line.TrimStart();
            markdown.Append(line, 0, line.Length - trimmed.Length);
            int numberEnd = NumberedItemEnd(trimmed);
            for (int index = 0; index < trimmed.Length; index++)
            {
                char character = trimmed[index];
                if (Marks.Contains(character) || index == numberEnd)
                    markdown.Append('\\');
                markdown.Append(character);
            }
        }
        return markdown.ToString();
    }

    private static int NumberedItemEnd(string line)
    {
        int digits = 0;
        while (digits < line.Length && char.IsAsciiDigit(line[digits]))
            digits++;
        return digits > 0 && digits + 1 < line.Length && line[digits] == '.' && line[digits + 1] == ' ' ? digits : -1;
    }

    private static void AddBlock(StringBuilder text, Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                AddLine(text, heading.Content);
                break;
            case ParagraphBlock paragraph:
                AddLine(text, paragraph.Content);
                break;
            case QuoteBlock quote:
                foreach (ParagraphBlock paragraph in quote.Paragraphs)
                    AddLine(text, paragraph.Content);
                break;
            case TableBlock table:
                foreach (TableRow row in table.Rows.Prepend(table.Header))
                    foreach (IReadOnlyList<Inline> cell in row.Cells)
                        AddLine(text, cell);
                break;
            case ListBlock list:
                foreach (ListItem item in list.Items)
                {
                    AddLine(text, item.Content);
                    if (item.Nested is { } nested)
                        AddBlock(text, nested);
                }
                break;
        }
    }

    private static void AddLine(StringBuilder text, IReadOnlyList<Inline> inlines)
    {
        if (text.Length > 0)
            text.Append(' ');
        AddInlines(text, inlines);
    }

    private static void AddInlines(StringBuilder text, IReadOnlyList<Inline> inlines)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case TextRun run:
                    text.Append(run.Text);
                    break;
                case StrongRun { Content: var content }:
                    AddInlines(text, content);
                    break;
                case EmphasisRun { Content: var content }:
                    AddInlines(text, content);
                    break;
                case StrikeRun { Content: var content }:
                    AddInlines(text, content);
                    break;
                case HighlightRun { Content: var content }:
                    AddInlines(text, content);
                    break;
                case SubscriptRun { Content: var content }:
                    AddInlines(text, content);
                    break;
                case SuperscriptRun { Content: var content }:
                    AddInlines(text, content);
                    break;
                case CodeRun code:
                    text.Append(code.Text);
                    break;
                case LinkRun link:
                    AddInlines(text, link.Content);
                    break;
                case LineBreakRun:
                    text.Append(' ');
                    break;
            }
        }
    }
}
