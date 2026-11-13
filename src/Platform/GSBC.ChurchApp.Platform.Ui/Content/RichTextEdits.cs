using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Ui.Content;

public readonly record struct EditorText(string Text, int Start, int End)
{
    public string Selected => Text[Start..End];
}

public enum LineStyle
{
    Text,
    Heading1,
    Heading2,
    Heading3,
    Bullets,
    Numbers,
    Quote,
}

public static class RichTextEdits
{
    public static EditorText Wrap(EditorText current, string marker, string placeholder)
    {
        (int start, int end) = TrimmedSelection(current);
        string text = current.Text;
        bool alreadyWrapped = start >= marker.Length && end + marker.Length <= text.Length
            && text.AsSpan(start - marker.Length, marker.Length).SequenceEqual(marker)
            && text.AsSpan(end, marker.Length).SequenceEqual(marker)
            && (marker.Length > 1 || !IsBold(text, start - 1, end));
        if (alreadyWrapped)
        {
            string unwrapped = text.Remove(end, marker.Length).Remove(start - marker.Length, marker.Length);
            return new EditorText(unwrapped, start - marker.Length, end - marker.Length);
        }

        string inner = start == end ? placeholder : text[start..end];
        string wrapped = text[..start] + marker + inner + marker + text[end..];
        return new EditorText(wrapped, start + marker.Length, start + marker.Length + inner.Length);
    }

    public static EditorText Link(EditorText current, string href, string placeholder)
    {
        (int start, int end) = TrimmedSelection(current);
        string label = start == end ? placeholder : current.Text[start..end];
        string text = current.Text[..start] + "[" + label + "](" + href + ")" + current.Text[end..];
        return new EditorText(text, start + 1, start + 1 + label.Length);
    }

    public static EditorText Block(EditorText current, string block, int? caretInBlock = null)
    {
        string text = current.Text;
        int lineEnd = text.IndexOf('\n', current.End);
        int at = lineEnd < 0 ? text.Length : lineEnd;
        int lineStart = current.End == 0 ? 0 : text.LastIndexOf('\n', current.End - 1) + 1;
        if (text[lineStart..at].Trim().Length == 0)
            at = lineStart;
        string before = at == 0 || text[..at].EndsWith("\n\n", StringComparison.Ordinal) ? "" : text[at - 1] == '\n' ? "\n" : "\n\n";
        string after = at == text.Length ? "\n" : text[at..].StartsWith("\n\n", StringComparison.Ordinal) ? "" : text[at] == '\n' ? "\n" : "\n\n";
        string inserted = before + block + after;
        int caret = caretInBlock is { } inside ? at + before.Length + inside : at + inserted.Length;
        return new EditorText(text[..at] + inserted + text[at..], caret, caret);
    }

    public static EditorText MediaLines(EditorText current, IEnumerable<string> lines) => Block(current, string.Join('\n', lines));

    public static string MediaLine(string mediaId, string alt) =>
        $"![{OneLine(alt).Replace("[", "", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal)}]({LinkTargets.MediaScheme}{mediaId})";

    public static string VideoLinkLine(VideoLink video, string caption) =>
        $"![{OneLine(caption).Replace("[", "", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal)}]({video.WatchUrl})";

    public static EditorText Footnote(EditorText current)
    {
        string text = current.Text;
        int number = 1;
        while (text.Contains($"[^{number}]", StringComparison.Ordinal))
            number++;
        string reference = $"[^{number}]";
        string withReference = text[..current.End] + reference + text[current.End..];
        string definition = (withReference.EndsWith('\n') ? withReference.EndsWith("\n\n", StringComparison.Ordinal) ? "" : "\n" : "\n\n") + reference + ": ";
        string result = withReference + definition;
        return new EditorText(result, result.Length, result.Length);
    }

    public static EditorText Insert(EditorText current, string inserted)
    {
        string text = current.Text[..current.Start] + inserted + current.Text[current.End..];
        int caret = current.Start + inserted.Length;
        return new EditorText(text, caret, caret);
    }

    public static LineStyle StyleOf(string line) =>
        line.StartsWith("### ", StringComparison.Ordinal) ? LineStyle.Heading3
        : line.StartsWith("## ", StringComparison.Ordinal) ? LineStyle.Heading2
        : line.StartsWith("# ", StringComparison.Ordinal) ? LineStyle.Heading1
        : LineStyle.Text;

    private static string OneLine(string text) => string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();

    public static EditorText Lines(EditorText current, LineStyle style)
    {
        string text = current.Text;
        int first = current.Start == 0 ? 0 : text.LastIndexOf('\n', current.Start - 1) + 1;
        int last = current.End > current.Start && current.End > 0 && text[current.End - 1] == '\n' ? current.End - 1 : current.End;
        int lineEnd = text.IndexOf('\n', last);
        lineEnd = lineEnd < 0 ? text.Length : lineEnd;

        string[] lines = text[first..lineEnd].Split('\n');
        bool allStyled = style != LineStyle.Text && lines.Any(line => line.Length > 0) && lines.All(line => line.Length == 0 || Styled(line, style));
        string[] restyled = lines.Select((line, index) => line.Length == 0 && lines.Length > 1
            ? line
            : allStyled ? Unstyled(line) : Prefix(style, index) + Unstyled(line)).ToArray();

        string block = string.Join('\n', restyled);
        string result = text[..first] + block + text[lineEnd..];
        if (current.Start != current.End)
            return new EditorText(result, first, first + block.Length);

        int caret = Math.Clamp(current.Start + block.Length - (lineEnd - first), first, first + block.Length);
        return new EditorText(result, caret, caret);
    }

    private static (int Start, int End) TrimmedSelection(EditorText current)
    {
        int start = current.Start;
        int end = current.End;
        while (start < end && char.IsWhiteSpace(current.Text[start]))
            start++;
        while (end > start && char.IsWhiteSpace(current.Text[end - 1]))
            end--;
        return (start, end);
    }

    private static bool IsBold(string text, int openingStar, int closingStar) =>
        openingStar > 0 && text[openingStar - 1] == '*' && closingStar + 1 < text.Length && text[closingStar + 1] == '*';

    private static string Prefix(LineStyle style, int index) => style switch
    {
        LineStyle.Text => "",
        LineStyle.Heading1 => "# ",
        LineStyle.Heading2 => "## ",
        LineStyle.Heading3 => "### ",
        LineStyle.Bullets => "- ",
        LineStyle.Numbers => $"{index + 1}. ",
        _ => "> ",
    };

    private static bool Styled(string line, LineStyle style) => style switch
    {
        LineStyle.Heading1 or LineStyle.Heading2 or LineStyle.Heading3 => StyleOf(line) == style,
        LineStyle.Bullets => line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal),
        LineStyle.Numbers => NumberPrefixLength(line) > 0,
        _ => line.StartsWith('>'),
    };

    private static string Unstyled(string line)
    {
        foreach (string prefix in new[] { "###### ", "##### ", "#### ", "### ", "## ", "# ", "- ", "* ", "+ ", "> ", ">" })
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                return line[prefix.Length..];
        }
        int number = NumberPrefixLength(line);
        return number > 0 ? line[number..] : line;
    }

    private static int NumberPrefixLength(string line)
    {
        int digits = 0;
        while (digits < line.Length && digits < 3 && char.IsAsciiDigit(line[digits]))
            digits++;
        return digits > 0 && line.Length > digits + 1 && line[digits] is '.' or ')' && line[digits + 1] == ' ' ? digits + 2 : 0;
    }
}
