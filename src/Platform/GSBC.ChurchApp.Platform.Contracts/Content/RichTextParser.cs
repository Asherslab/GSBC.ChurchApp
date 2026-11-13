using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public static partial class RichTextParser
{
    public const string CardSplit = "===";
    public const string DirectivePrefix = "::";
    public static readonly IReadOnlyList<string> ReservedDirectives = ["event", "sermon", "ministry", "lifegroup"];

    private const int NestedIndent = 2;
    private const int MaximumHrefLength = 2_000;
    private const int LargestHeading = 6;

    public static RichTextDocument Parse(string markdown)
    {
        string[] lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        Collector collector = new(FootnoteDefinitions(lines));
        List<Block> blocks = [];
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (IsBlank(line) || collector.IsDefinitionLine(index))
            {
                index++;
            }
            else if (Fence.Of(line) is { } fence)
            {
                blocks.Add(Code(fence, lines, ref index));
            }
            else if (IsCardSplit(line))
            {
                blocks.Add(new CardSplitBlock());
                index++;
            }
            else if (IsRule(line))
            {
                blocks.Add(new RuleBlock());
                index++;
            }
            else if (HeadingOf(line) is var (level, text))
            {
                blocks.Add(new HeadingBlock(level, InlineParser.Parse(text, collector)));
                index++;
            }
            else if (DirectiveOf(line.Trim()) is { } directive)
            {
                blocks.Add(directive);
                index++;
            }
            else if (Table(lines, ref index, collector) is { } table)
            {
                blocks.Add(table);
            }
            else if (EmbedOf(line.Trim(), collector) is { } embed)
            {
                blocks.Add(embed);
                index++;
            }
            else if (MediaOf(line.Trim(), collector) is { } media)
            {
                blocks.Add(MediaRun(media, lines, ref index, collector));
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

        return new RichTextDocument(blocks, collector.Links, collector.MediaIds, collector.Videos, collector.Problems) { Footnotes = collector.Footnotes() };
    }

    public static bool IsCardSplit(string line)
    {
        string trimmed = line.Trim();
        return trimmed.Length >= CardSplit.Length && trimmed.All(character => character == '=');
    }

    private static ParagraphBlock Paragraph(string[] lines, ref int index, Collector collector)
    {
        List<string> paragraph = [lines[index++].Trim()];
        while (index < lines.Length && !IsBlank(lines[index]) && !StartsBlock(lines[index]) && !collector.IsDefinitionLine(index))
            paragraph.Add(lines[index++].Trim());
        return new ParagraphBlock(InlineParser.ParseLines(paragraph, collector));
    }

    private static CodeBlock Code(Fence fence, string[] lines, ref int index)
    {
        List<string> code = [];
        index++;
        while (index < lines.Length && !fence.ClosedBy(lines[index]))
        {
            string line = lines[index++];
            int strip = Math.Min(fence.Indent, line.Length - line.TrimStart(' ').Length);
            code.Add(line[strip..]);
        }
        index++;
        return new CodeBlock(string.Join('\n', code));
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

    private static TableBlock? Table(string[] lines, ref int index, Collector collector)
    {
        if (index + 1 >= lines.Length || !lines[index].Contains('|'))
            return null;
        List<string> header = Cells(lines[index]);
        List<string> delimiters = Cells(lines[index + 1]);
        if (header.Count == 0 || delimiters.Count != header.Count || !delimiters.All(cell => DelimiterCell().IsMatch(cell)))
            return null;

        List<TableAlignment> alignments = delimiters.Select(cell => (cell.StartsWith(':'), cell.EndsWith(':')) switch
        {
            (true, true) => TableAlignment.Centre,
            (true, false) => TableAlignment.Left,
            (false, true) => TableAlignment.Right,
            _ => TableAlignment.None,
        }).ToList();
        index += 2;

        List<TableRow> rows = [];
        while (index < lines.Length && !IsBlank(lines[index]) && lines[index].Contains('|') && !StartsBlock(lines[index]))
            rows.Add(Row(Cells(lines[index++])));
        return new TableBlock(alignments, Row(header), rows);

        TableRow Row(List<string> cells) => new(Enumerable.Range(0, header.Count)
            .Select(column => InlineParser.Parse(column < cells.Count ? cells[column] : "", collector))
            .ToList());
    }

    private static List<string> Cells(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
            trimmed = trimmed[1..];
        if (trimmed.EndsWith('|') && !trimmed.EndsWith("\\|", StringComparison.Ordinal))
            trimmed = trimmed[..^1];

        List<string> cells = [];
        StringBuilder cell = new();
        for (int position = 0; position < trimmed.Length; position++)
        {
            if (trimmed[position] == '\\' && position + 1 < trimmed.Length && trimmed[position + 1] == '|')
            {
                cell.Append('|');
                position++;
            }
            else if (trimmed[position] == '|')
            {
                cells.Add(cell.ToString().Trim());
                cell.Clear();
            }
            else
            {
                cell.Append(trimmed[position]);
            }
        }
        cells.Add(cell.ToString().Trim());
        return cells;
    }

    private static bool StartsBlock(string line) =>
        HeadingOf(line) is not null || IsQuote(line) || ListMarker.Of(line) is { Indent: < NestedIndent } || IsMediaLine(line.Trim()) || IsEmbedLine(line.Trim())
        || Fence.Of(line) is not null || IsRule(line) || DirectiveOf(line.Trim()) is not null;

    private static (int Level, string Text)? HeadingOf(string line)
    {
        int level = 0;
        while (level < line.Length && line[level] == '#')
            level++;
        if (level is 0 or > LargestHeading || level >= line.Length || line[level] != ' ')
            return null;
        string text = line[(level + 1)..].Trim();
        string closed = text.TrimEnd('#');
        if (closed.Length < text.Length && (closed.Length == 0 || closed.EndsWith(' ')))
            text = closed.Trim();
        return text.Length == 0 ? null : (level, text);
    }

    private static bool IsRule(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length < 3 || line.Length - line.TrimStart(' ').Length > 3 || trimmed[0] is not ('-' or '*' or '_'))
            return false;
        char mark = trimmed[0];
        return trimmed.All(character => character == mark || character == ' ') && trimmed.Count(character => character == mark) >= 3;
    }

    private static bool IsQuote(string line) => line.TrimStart().StartsWith('>');

    private static bool IsBlank(string line) => string.IsNullOrWhiteSpace(line);

    private static bool IsMediaLine(string line) =>
        line.StartsWith("![", StringComparison.Ordinal) && line.EndsWith(')') && line.Contains("](" + LinkTargets.MediaScheme, StringComparison.Ordinal);

    private static bool IsEmbedLine(string line) =>
        line.StartsWith("![", StringComparison.Ordinal) && line.EndsWith(')') && line.Contains("](https://", StringComparison.Ordinal);

    private static DirectiveBlock? DirectiveOf(string line)
    {
        Match match = Directive().Match(line);
        if (!match.Success)
            return null;
        Dictionary<string, string> attributes = new(StringComparer.Ordinal);
        foreach (Match attribute in DirectiveAttribute().Matches(match.Groups["attributes"].Value))
            attributes[attribute.Groups["key"].Value] = attribute.Groups["quoted"].Success ? attribute.Groups["quoted"].Value : attribute.Groups["bare"].Value;
        return new DirectiveBlock(match.Groups["name"].Value, attributes);
    }

    private static Block MediaRun(MediaBlock first, string[] lines, ref int index, Collector collector)
    {
        List<MediaBlock> run = [first];
        index++;
        while (index < lines.Length && MediaOf(lines[index].Trim(), collector) is { } next)
        {
            run.Add(next);
            index++;
        }
        return run.Count == 1 ? first : new GalleryBlock(run);
    }

    private static MediaBlock? MediaOf(string line, Collector collector)
    {
        if (!IsMediaLine(line))
            return null;

        int split = line.IndexOf("](" + LinkTargets.MediaScheme, StringComparison.Ordinal);
        string alt = line[2..split];
        string target = line[(split + 2 + LinkTargets.MediaScheme.Length)..^1].Trim();
        int quote = target.IndexOf(" \"", StringComparison.Ordinal);
        string id = quote < 0 ? target : target[..quote].Trim();
        string caption = quote < 0 ? "" : target[(quote + 1)..];
        if (caption.Length > 0 && (caption.Length < 2 || !caption.EndsWith('"')))
            return null;
        if (alt.Contains(']') || id.Length == 0 || id.Any(character => char.IsWhiteSpace(character) || character is '(' or ')' or '"'))
            return null;

        collector.MediaIds.Add(id);
        return new MediaBlock(id, alt.Trim(), caption.Length == 0 ? "" : caption[1..^1].Trim());
    }

    private static EmbedBlock? EmbedOf(string line, Collector collector)
    {
        if (!IsEmbedLine(line))
            return null;

        int split = line.IndexOf("](", StringComparison.Ordinal);
        string title = line[2..split];
        string target = line[(split + 2)..^1].Trim();
        int quote = target.IndexOf(" \"", StringComparison.Ordinal);
        if (quote >= 0 && target.EndsWith('"'))
        {
            title = target[(quote + 2)..^1];
            target = target[..quote];
        }
        if (title.Contains(']') || VideoLink.Of(target) is not { } video)
            return null;

        collector.Videos.Add(video);
        return new EmbedBlock(video, title.Trim());
    }

    private static Dictionary<string, (int Line, List<string> Text)> FootnoteDefinitions(string[] lines)
    {
        Dictionary<string, (int Line, List<string> Text)> definitions = new(StringComparer.Ordinal);
        Fence? fence = null;
        for (int index = 0; index < lines.Length; index++)
        {
            if (fence is not null)
            {
                if (fence.ClosedBy(lines[index]))
                    fence = null;
                continue;
            }
            fence = Fence.Of(lines[index]);
            if (fence is not null)
                continue;

            Match match = FootnoteDefinition().Match(lines[index]);
            if (!match.Success || definitions.ContainsKey(match.Groups["label"].Value))
                continue;
            List<string> text = [match.Groups["text"].Value.Trim()];
            definitions[match.Groups["label"].Value] = (index, text);
            while (index + 1 < lines.Length && lines[index + 1].StartsWith("  ", StringComparison.Ordinal) && !IsBlank(lines[index + 1]))
                text.Add(lines[++index].Trim());
        }
        return definitions;
    }

    [GeneratedRegex(@"^\[\^(?<label>[A-Za-z0-9_-]{1,40})\]:\s?(?<text>.*)$")]
    private static partial Regex FootnoteDefinition();

    [GeneratedRegex(@"^::(?<name>[a-z][a-z0-9-]{0,30})\{(?<attributes>[^{}]{0,500})\}$")]
    private static partial Regex Directive();

    [GeneratedRegex("(?<key>[a-z][a-z0-9-]{0,30})=(?:\"(?<quoted>[^\"]*)\"|(?<bare>[^\\s\"]+))")]
    private static partial Regex DirectiveAttribute();

    [GeneratedRegex("^:?-+:?$")]
    private static partial Regex DelimiterCell();

    private sealed record Fence(char Mark, int Length, int Indent)
    {
        public static Fence? Of(string line)
        {
            int indent = line.Length - line.TrimStart(' ').Length;
            if (indent > 3)
                return null;
            string rest = line[indent..];
            if (rest.Length < 3 || rest[0] is not ('`' or '~'))
                return null;
            char mark = rest[0];
            int length = 0;
            while (length < rest.Length && rest[length] == mark)
                length++;
            if (length < 3 || (mark == '`' && rest[length..].Contains('`')))
                return null;
            return new Fence(mark, length, indent);
        }

        public bool ClosedBy(string line)
        {
            string trimmed = line.Trim();
            return trimmed.Length >= Length && trimmed.All(character => character == Mark);
        }
    }

    private sealed record ListMarker(int Indent, bool Ordered, int Number, string Content)
    {
        public static ListMarker? Of(string line)
        {
            int indent = line.Length - line.TrimStart(' ').Length;
            string rest = line[indent..];
            if (rest.Length >= 2 && rest[0] is '-' or '*' or '+' && rest[1] == ' ')
                return new ListMarker(indent, Ordered: false, 1, rest[2..].Trim());

            int digits = 0;
            while (digits < rest.Length && digits < 4 && char.IsAsciiDigit(rest[digits]))
                digits++;
            if (digits is 0 or 4 || rest.Length < digits + 1 || rest[digits] is not ('.' or ')') || (rest.Length > digits + 1 && rest[digits + 1] != ' '))
                return null;
            return new ListMarker(indent, Ordered: true, int.Parse(rest[..digits], CultureInfo.InvariantCulture), rest[(digits + 1)..].Trim());
        }
    }

    private sealed class Collector(Dictionary<string, (int Line, List<string> Text)> definitions)
    {
        private readonly HashSet<int> _definitionLines = DefinitionLines(definitions);
        private readonly List<string> _referenced = [];

        public List<string> Links { get; } = [];
        public List<string> MediaIds { get; } = [];
        public List<VideoLink> Videos { get; } = [];
        public List<string> Problems { get; } = [];

        public bool IsDefinitionLine(int line) => _definitionLines.Contains(line);

        public int? Reference(string label)
        {
            if (!definitions.ContainsKey(label))
                return null;
            int at = _referenced.IndexOf(label);
            if (at >= 0)
                return at + 1;
            _referenced.Add(label);
            return _referenced.Count;
        }

        public IReadOnlyList<Footnote> Footnotes()
        {
            List<Footnote> notes = [];
            for (int index = 0; index < _referenced.Count; index++)
            {
                string label = _referenced[index];
                notes.Add(new Footnote(label, index + 1, InlineParser.ParseLines(definitions[label].Text, this)));
            }
            return notes;
        }

        private static HashSet<int> DefinitionLines(Dictionary<string, (int Line, List<string> Text)> definitions) =>
            [.. definitions.Values.SelectMany(definition => Enumerable.Range(definition.Line, definition.Text.Count))];
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

                    if (character == '`')
                    {
                        int run = RunLength(position, end, '`');
                        int close = FindCodeClose(position + run, end, run);
                        if (close < 0)
                        {
                            pending.Append(text, position, run);
                            position += run;
                            continue;
                        }
                        Emit(new CodeRun(CodeText(text[(position + run)..close])));
                        position = close + run;
                        continue;
                    }

                    if (character is '*' or '_' && TryEmphasis(position, end, out Inline? emphasis, out int after))
                    {
                        Emit(emphasis!);
                        position = after;
                        continue;
                    }

                    if (character is '*' or '_')
                    {
                        int run = RunLength(position, end, character);
                        pending.Append(text, position, run);
                        position += run;
                        continue;
                    }

                    if (character == '~' && TryDelimited(position, end, "~~", spaces: true, out int closeStrike))
                    {
                        Emit(new StrikeRun(Inlines(position + 2, closeStrike)));
                        position = closeStrike + 2;
                        continue;
                    }

                    if (character == '=' && TryDelimited(position, end, "==", spaces: true, out int closeMark))
                    {
                        Emit(new HighlightRun(Inlines(position + 2, closeMark)));
                        position = closeMark + 2;
                        continue;
                    }

                    if (character == '~' && TryDelimited(position, end, "~", spaces: false, out int closeSub))
                    {
                        Emit(new SubscriptRun(Inlines(position + 1, closeSub)));
                        position = closeSub + 1;
                        continue;
                    }

                    if (character == '^' && TryDelimited(position, end, "^", spaces: false, out int closeSup))
                    {
                        Emit(new SuperscriptRun(Inlines(position + 1, closeSup)));
                        position = closeSup + 1;
                        continue;
                    }

                    if (character == ':' && TryEmoji(position, end, out string? emoji, out int emojiEnd))
                    {
                        pending.Append(emoji);
                        position = emojiEnd;
                        continue;
                    }

                    if (character == '[' && TryFootnote(position, end, out int number, out string label, out int footnoteEnd))
                    {
                        Emit(new FootnoteRun(label, number));
                        position = footnoteEnd;
                        continue;
                    }

                    if (character == '!' && position + 1 < end && text[position + 1] == '[' && TryLink(position + 1, end, out _, out _, out int imageEnd))
                    {
                        collector.Problems.Add("Pictures and files have to come from the media library, and videos from YouTube or Vimeo, each on a line of its own.");
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

            private bool TryEmphasis(int position, int end, out Inline? inline, out int after)
            {
                inline = null;
                after = position;
                char mark = text[position];
                int run = RunLength(position, end, mark);
                if (mark == '_' && !StartsWord(position))
                    return false;
                int opener = Math.Min(run, 3);
                int start = position + run - opener;
                if (start + opener >= end || char.IsWhiteSpace(text[start + opener]))
                    return false;
                if (start > position)
                    return false;

                int contentStart = position + opener;
                if (opener == 3)
                {
                    if (FindCloser(mark, contentStart, end, 3) is var (both, _) and ( >= 0, _))
                    {
                        inline = new StrongRun([new EmphasisRun(Inlines(contentStart, both))]);
                        after = both + 3;
                        return true;
                    }
                    if (FindCloser(mark, contentStart, end, 2) is var (strongClose, _) and ( >= 0, _) && FindCloser(mark, strongClose + 2, end, 1) is var (emClose, _) and ( >= 0, _))
                    {
                        inline = new EmphasisRun([new StrongRun(Inlines(contentStart, strongClose)), .. Inlines(strongClose + 2, emClose)]);
                        after = emClose + 1;
                        return true;
                    }
                    if (FindCloser(mark, contentStart, end, 1) is var (innerClose, _) and ( >= 0, _) && FindCloser(mark, innerClose + 1, end, 2) is var (outerClose, _) and ( >= 0, _))
                    {
                        inline = new StrongRun([new EmphasisRun(Inlines(contentStart, innerClose)), .. Inlines(innerClose + 1, outerClose)]);
                        after = outerClose + 2;
                        return true;
                    }
                    return false;
                }

                (int close, _) = FindCloser(mark, contentStart, end, opener);
                if (close < 0)
                    return false;
                IReadOnlyList<Inline> content = Inlines(contentStart, close);
                inline = opener == 2 ? new StrongRun(content) : new EmphasisRun(content);
                after = close + opener;
                return true;
            }

            private (int Close, int Run) FindCloser(char mark, int from, int end, int wanted)
            {
                string key = $"{mark}{wanted}";
                if (_noneFrom.TryGetValue(key, out int none) && from >= none)
                    return (-1, 0);

                for (int position = from; position < end; position++)
                {
                    if (text[position] == '\\')
                    {
                        position++;
                        continue;
                    }
                    if (text[position] == '`')
                    {
                        int ticks = RunLength(position, end, '`');
                        int skip = FindCodeClose(position + ticks, end, ticks);
                        position = skip < 0 ? position + ticks - 1 : skip + ticks - 1;
                        continue;
                    }
                    if (text[position] != mark)
                        continue;

                    int run = RunLength(position, end, mark);
                    bool closes = position > from && !char.IsWhiteSpace(text[position - 1]) && (mark != '_' || !EndsWordBefore(position + run, end));
                    if (closes && Fits(run, wanted))
                        return (position + run - wanted, run);
                    position += run - 1;
                }

                if (end == text.Length)
                    _noneFrom[key] = Math.Min(from, _noneFrom.GetValueOrDefault(key, int.MaxValue));
                return (-1, 0);
            }

            private static bool Fits(int run, int wanted) => wanted switch
            {
                3 => run >= 3,
                2 => run is 2 or 3,
                _ => run is 1 or 3,
            };

            private bool TryDelimited(int position, int end, string delimiter, bool spaces, out int close)
            {
                close = -1;
                if (string.CompareOrdinal(text, position, delimiter, 0, delimiter.Length) != 0)
                    return false;
                char mark = delimiter[0];
                if (RunLength(position, end, mark) != delimiter.Length)
                    return false;

                int from = position + delimiter.Length;
                if (from >= end || char.IsWhiteSpace(text[from]))
                    return false;

                string key = delimiter + (spaces ? "+" : "-");
                if (_noneFrom.TryGetValue(key, out int none) && from >= none)
                    return false;
                for (int at = from; at < end; at++)
                {
                    if (text[at] == '\\')
                    {
                        at++;
                        continue;
                    }
                    if (!spaces && char.IsWhiteSpace(text[at]))
                        return false;
                    if (text[at] != mark)
                        continue;
                    int run = RunLength(at, end, mark);
                    if (run == delimiter.Length && !char.IsWhiteSpace(text[at - 1]) && at > from)
                    {
                        close = at;
                        return true;
                    }
                    at += run - 1;
                }
                if (end == text.Length && spaces)
                    _noneFrom[key] = Math.Min(from, _noneFrom.GetValueOrDefault(key, int.MaxValue));
                return false;
            }

            private bool TryEmoji(int position, int end, out string? emoji, out int after)
            {
                emoji = null;
                after = position;
                int close = position + 1;
                while (close < end && close - position <= 30 && (char.IsAsciiLetterLower(text[close]) || char.IsAsciiDigit(text[close]) || text[close] is '_' or '-' or '+'))
                    close++;
                if (close >= end || text[close] != ':' || close == position + 1)
                    return false;
                emoji = Emoji.Of(text[(position + 1)..close]);
                after = close + 1;
                return emoji is not null;
            }

            private bool TryFootnote(int position, int end, out int number, out string label, out int after)
            {
                number = 0;
                label = "";
                after = position;
                if (position + 2 >= end || text[position + 1] != '^')
                    return false;
                int close = text.IndexOf(']', position + 2, Math.Min(end, position + 44) - position - 2);
                if (close < 0)
                    return false;
                label = text[(position + 2)..close];
                if (collector.Reference(label) is not { } found)
                    return false;
                number = found;
                after = close + 1;
                return true;
            }

            private bool TryLink(int open, int end, out int labelEnd, out string href, out int linkEnd)
            {
                linkEnd = -1;
                href = "";
                labelEnd = FindPlain(']', open + 1, end);
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

            private int FindPlain(char wanted, int from, int end)
            {
                string key = wanted.ToString();
                if (_noneFrom.TryGetValue(key, out int none) && from >= none)
                    return -1;
                for (int position = from; position < end; position++)
                {
                    if (text[position] == '\\')
                    {
                        position++;
                        continue;
                    }
                    if (text[position] == wanted)
                        return position;
                }
                if (end == text.Length)
                    _noneFrom[key] = Math.Min(from, _noneFrom.GetValueOrDefault(key, int.MaxValue));
                return -1;
            }

            private int FindCodeClose(int from, int end, int ticks)
            {
                string key = "`" + ticks.ToString(CultureInfo.InvariantCulture);
                if (_noneFrom.TryGetValue(key, out int none) && from >= none)
                    return -1;
                for (int position = from; position < end; position++)
                {
                    if (text[position] != '`')
                        continue;
                    int run = RunLength(position, end, '`');
                    if (run == ticks)
                        return position;
                    position += run - 1;
                }
                if (end == text.Length)
                    _noneFrom[key] = Math.Min(from, _noneFrom.GetValueOrDefault(key, int.MaxValue));
                return -1;
            }

            private static string CodeText(string code) =>
                code.Length > 2 && code[0] == ' ' && code[^1] == ' ' && code.Trim().Length > 0 ? code[1..^1] : code;

            private int RunLength(int position, int end, char mark)
            {
                int run = 0;
                while (position + run < end && text[position + run] == mark)
                    run++;
                return run;
            }

            private static bool IsEscapable(char character) => char.IsPunctuation(character) || char.IsSymbol(character);

            private bool StartsWord(int position) => position == 0 || !char.IsLetterOrDigit(text[position - 1]);

            private bool EndsWordBefore(int after, int end) => after < end && char.IsLetterOrDigit(text[after]);
        }
    }
}
