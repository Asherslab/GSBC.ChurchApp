using System.Globalization;
using System.Text;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public static class RichTextParser
{
    private const int NestedIndent = 2;
    private const int MaximumHrefLength = 2_000;

    public static RichTextDocument Parse(string markdown)
    {
        Collector collector = new();
        string[] lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        List<Block> blocks = [];
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (IsBlank(line))
            {
                index++;
            }
            else if (HeadingLevel(line) is { } level)
            {
                blocks.Add(new HeadingBlock(level, InlineParser.Parse(line[(level + 1)..].Trim(), collector)));
                index++;
            }
            else if (ImageOf(line.Trim(), collector) is { } image)
            {
                blocks.Add(image);
                index++;
            }
            else if (IsQuote(line))
            {
                blocks.Add(Quote(lines, ref index, collector));
            }
            else if (ListMarker.Of(line) is { Indent: < NestedIndent })
            {
                blocks.Add(List(lines, ref index, collector));
            }
            else
            {
                blocks.Add(Paragraph(lines, ref index, collector));
            }
        }

        return new RichTextDocument(blocks, collector.Links, collector.ImageIds, collector.Problems);
    }

    private static ParagraphBlock Paragraph(string[] lines, ref int index, Collector collector)
    {
        List<string> paragraph = [lines[index++].Trim()];
        while (index < lines.Length && !IsBlank(lines[index]) && !StartsBlock(lines[index]))
            paragraph.Add(lines[index++].Trim());
        return new ParagraphBlock(InlineParser.ParseLines(paragraph, collector));
    }

    private static QuoteBlock Quote(string[] lines, ref int index, Collector collector)
    {
        List<ParagraphBlock> paragraphs = [];
        List<string> current = [];
        while (index < lines.Length && IsQuote(lines[index]))
        {
            string inner = lines[index++].TrimStart()[1..];
            inner = inner.StartsWith(' ') ? inner[1..] : inner;
            if (IsBlank(inner))
            {
                Flush();
                continue;
            }
            current.Add(inner.Trim());
        }
        Flush();
        return new QuoteBlock(paragraphs);

        void Flush()
        {
            if (current.Count == 0)
                return;
            paragraphs.Add(new ParagraphBlock(InlineParser.ParseLines(current, collector)));
            current = [];
        }
    }

    private static ListBlock List(string[] lines, ref int index, Collector collector)
    {
        ListMarker first = ListMarker.Of(lines[index])!;
        List<(List<string> Lines, List<(ListMarker Marker, List<string> Lines)> Nested)> items = [];

        while (index < lines.Length)
        {
            string line = lines[index];
            if (IsBlank(line))
            {
                int next = index + 1;
                if (next < lines.Length && ListMarker.Of(lines[next]) is { } after && (after.Indent >= NestedIndent || after.Ordered == first.Ordered))
                {
                    index = next;
                    continue;
                }
                break;
            }

            ListMarker? marker = ListMarker.Of(line);
            if (marker is { Indent: < NestedIndent })
            {
                if (marker.Ordered != first.Ordered)
                    break;
                items.Add(([marker.Content], []));
            }
            else if (marker is not null && items.Count > 0)
            {
                items[^1].Nested.Add((marker, [marker.Content]));
            }
            else if (line.StartsWith(' ') && items.Count > 0)
            {
                List<string> continued = items[^1].Nested.Count > 0 ? items[^1].Nested[^1].Lines : items[^1].Lines;
                continued.Add(line.Trim());
            }
            else
            {
                break;
            }
            index++;
        }

        return new ListBlock(first.Ordered, first.Number, items.Select(item => new ListItem(
            InlineParser.ParseLines(item.Lines, collector),
            item.Nested.Count == 0
                ? null
                : new ListBlock(item.Nested[0].Marker.Ordered, item.Nested[0].Marker.Number,
                    item.Nested.Select(nested => new ListItem(InlineParser.ParseLines(nested.Lines, collector), null)).ToList()))).ToList());
    }

    private static bool StartsBlock(string line) =>
        HeadingLevel(line) is not null || IsQuote(line) || ListMarker.Of(line) is { Indent: < NestedIndent } || IsImageLine(line.Trim());

    private static int? HeadingLevel(string line) =>
        line.StartsWith("### ", StringComparison.Ordinal) ? 3 : line.StartsWith("## ", StringComparison.Ordinal) ? 2 : null;

    private static bool IsQuote(string line) => line.TrimStart().StartsWith('>');

    private static bool IsBlank(string line) => string.IsNullOrWhiteSpace(line);

    private static bool IsImageLine(string line) =>
        line.StartsWith("![", StringComparison.Ordinal) && line.EndsWith(')') && line.Contains("](" + LinkTargets.MediaScheme, StringComparison.Ordinal);

    private static ImageBlock? ImageOf(string line, Collector collector)
    {
        if (!IsImageLine(line))
            return null;

        int split = line.IndexOf("](" + LinkTargets.MediaScheme, StringComparison.Ordinal);
        string alt = line[2..split];
        string id = line[(split + 2 + LinkTargets.MediaScheme.Length)..^1].Trim();
        if (alt.Contains(']') || id.Length == 0 || id.Any(character => char.IsWhiteSpace(character) || character is '(' or ')'))
            return null;

        collector.ImageIds.Add(id);
        return new ImageBlock(id, alt.Trim());
    }

    private sealed record ListMarker(int Indent, bool Ordered, int Number, string Content)
    {
        public static ListMarker? Of(string line)
        {
            int indent = line.Length - line.TrimStart(' ').Length;
            string rest = line[indent..];
            if (rest.Length >= 2 && rest[0] is '-' or '*' && rest[1] == ' ')
                return new ListMarker(indent, Ordered: false, 1, rest[2..].Trim());

            int digits = 0;
            while (digits < rest.Length && digits < 4 && char.IsAsciiDigit(rest[digits]))
                digits++;
            if (digits is 0 or 4 || rest.Length < digits + 2 || rest[digits] is not ('.' or ')') || rest[digits + 1] != ' ')
                return null;
            return new ListMarker(indent, Ordered: true, int.Parse(rest[..digits], CultureInfo.InvariantCulture), rest[(digits + 2)..].Trim());
        }
    }

    private sealed class Collector
    {
        public List<string> Links { get; } = [];
        public List<string> ImageIds { get; } = [];
        public List<string> Problems { get; } = [];
    }

    private static class InlineParser
    {
        public static IReadOnlyList<Inline> ParseLines(IReadOnlyList<string> lines, Collector collector)
        {
            List<Inline> content = [];
            for (int index = 0; index < lines.Count; index++)
            {
                if (index > 0)
                    content.Add(new LineBreakRun());
                content.AddRange(Parse(lines[index], collector));
            }
            return content;
        }

        public static IReadOnlyList<Inline> Parse(string text, Collector collector) => new Scanner(text, collector).Inlines(0, text.Length);

        private sealed class Scanner(string text, Collector collector)
        {
            private readonly Dictionary<string, int> _noneFrom = [];

            public List<Inline> Inlines(int start, int end)
            {
                List<Inline> inlines = [];
                StringBuilder pending = new();
                int position = start;
                while (position < end)
                {
                    char character = text[position];
                    if (character == '\\' && position + 1 < end && IsEscapable(text[position + 1]))
                    {
                        pending.Append(text[position + 1]);
                        position += 2;
                        continue;
                    }

                    if (TryDelimited(position, end, "**", out int close))
                    {
                        Emit(new StrongRun(Inlines(position + 2, close)));
                        position = close + 2;
                        continue;
                    }

                    if ((character == '*' || (character == '_' && StartsWord(position))) && TryDelimited(position, end, character.ToString(), out close))
                    {
                        Emit(new EmphasisRun(Inlines(position + 1, close)));
                        position = close + 1;
                        continue;
                    }

                    if (character == '!' && position + 1 < end && text[position + 1] == '[' && TryLink(position + 1, end, out _, out _, out int imageEnd))
                    {
                        collector.Problems.Add("Images have to come from the media library, each on a line of its own.");
                        pending.Append(text, position, imageEnd - position);
                        position = imageEnd;
                        continue;
                    }

                    if (character == '[' && TryLink(position, end, out int labelEnd, out string href, out int linkEnd))
                    {
                        collector.Links.Add(href);
                        Emit(new LinkRun(href, Inlines(position + 1, labelEnd)));
                        position = linkEnd;
                        continue;
                    }

                    pending.Append(character);
                    position++;
                }

                Flush();
                return inlines;

                void Emit(Inline inline)
                {
                    Flush();
                    inlines.Add(inline);
                }

                void Flush()
                {
                    if (pending.Length == 0)
                        return;
                    inlines.Add(new TextRun(pending.ToString()));
                    pending.Clear();
                }
            }

            private bool TryDelimited(int position, int end, string delimiter, out int close)
            {
                close = -1;
                if (string.CompareOrdinal(text, position, delimiter, 0, delimiter.Length) != 0)
                    return false;
                if (delimiter == "*" && position + 1 < end && text[position + 1] == '*')
                    return false;

                int from = position + delimiter.Length;
                if (from >= end || char.IsWhiteSpace(text[from]))
                    return false;

                close = Find(delimiter, from, end);
                while (close >= 0 && (char.IsWhiteSpace(text[close - 1]) || IsDoubledSingle(delimiter, close, end) || (delimiter == "_" && EndsWordBefore(close + 1, end))))
                    close = Find(delimiter, close + 1, end);
                return close >= from;
            }

            private bool IsDoubledSingle(string delimiter, int close, int end) =>
                delimiter == "*" && ((close + 1 < end && text[close + 1] == '*') || text[close - 1] == '*');

            private bool TryLink(int open, int end, out int labelEnd, out string href, out int linkEnd)
            {
                labelEnd = linkEnd = -1;
                href = "";
                labelEnd = Find("]", open + 1, end);
                if (labelEnd < 0 || labelEnd + 1 >= end || text[labelEnd + 1] != '(')
                    return false;
                int closeParen = ClosingParenthesis(labelEnd + 2, end);
                if (closeParen < 0)
                    return false;

                href = text[(labelEnd + 2)..closeParen].Trim();
                linkEnd = closeParen + 1;
                return true;
            }

            private int ClosingParenthesis(int from, int end)
            {
                int depth = 0;
                for (int position = from; position < Math.Min(end, from + MaximumHrefLength); position++)
                {
                    if (text[position] == '(')
                        depth++;
                    else if (text[position] == ')' && depth-- == 0)
                        return position;
                }
                return -1;
            }

            private int Find(string delimiter, int from, int end)
            {
                if (_noneFrom.TryGetValue(delimiter, out int none) && from >= none)
                    return -1;

                for (int position = from; position <= end - delimiter.Length; position++)
                {
                    if (text[position] == '\\')
                    {
                        position++;
                        continue;
                    }
                    if (string.CompareOrdinal(text, position, delimiter, 0, delimiter.Length) == 0)
                        return position;
                }

                if (end == text.Length)
                    _noneFrom[delimiter] = Math.Min(from, _noneFrom.GetValueOrDefault(delimiter, int.MaxValue));
                return -1;
            }

            private static bool IsEscapable(char character) => char.IsPunctuation(character) || char.IsSymbol(character);

            private bool StartsWord(int position) => position == 0 || !char.IsLetterOrDigit(text[position - 1]);

            private bool EndsWordBefore(int after, int end) => after < end && char.IsLetterOrDigit(text[after]);
        }
    }
}
