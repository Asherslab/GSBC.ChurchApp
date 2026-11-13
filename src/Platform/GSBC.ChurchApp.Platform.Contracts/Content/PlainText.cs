using System.Text;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public static class PlainText
{
    private const string Marks = "\\*_[]()!#>-`";

    public static string Of(RichText content)
    {
        StringBuilder text = new();
        foreach (Block block in RichTextParser.Parse(content.Markdown).Blocks)
            AddBlock(text, block);
        return text.ToString().Trim();
    }

    public static RichText AsRichText(string text) => new() { Markdown = Escaped(text) };

    public static string Escaped(string text)
    {
        StringBuilder markdown = new(text.Length + 16);
        foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (markdown.Length > 0)
                markdown.Append('\n');
            string trimmed = line.TrimStart();
            markdown.Append(line, 0, line.Length - trimmed.Length);
            for (int index = 0; index < trimmed.Length; index++)
            {
                char character = trimmed[index];
                if (Marks.Contains(character) || (index == 0 && StartsNumberedItem(trimmed)) || (character == '+' && index == 0))
                    markdown.Append('\\');
                markdown.Append(character);
            }
        }
        return markdown.ToString();
    }

    private static bool StartsNumberedItem(string line)
    {
        int digits = 0;
        while (digits < line.Length && char.IsAsciiDigit(line[digits]))
            digits++;
        return digits > 0 && digits + 1 < line.Length && line[digits] is '.' or ')' && line[digits + 1] == ' ';
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
                case StrongRun strong:
                    AddInlines(text, strong.Content);
                    break;
                case EmphasisRun emphasis:
                    AddInlines(text, emphasis.Content);
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
