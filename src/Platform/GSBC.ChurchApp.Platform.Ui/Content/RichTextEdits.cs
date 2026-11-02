using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Ui.Content;

public readonly record struct EditorText(string Text, int Start, int End)
{
    public string Selected => Text[Start..End];
}

public enum LineStyle
{
    Heading,
    Subheading,
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

    public static EditorText Image(EditorText current, string mediaId, string alt)
    {
        string text = current.Text;
        int at = current.End;
        string before = at == 0 || text[..at].EndsWith("\n\n", StringComparison.Ordinal) ? "" : text[at - 1] == '\n' ? "\n" : "\n\n";
        string image = $"![{alt.Replace("]", "", StringComparison.Ordinal)}]({LinkTargets.MediaScheme}{mediaId})";
        string after = at == text.Length ? "\n" : text[at..].StartsWith("\n\n", StringComparison.Ordinal) ? "" : text[at] == '\n' ? "\n" : "\n\n";
        string inserted = before + image + after;
        int caret = at + inserted.Length;
        return new EditorText(text[..at] + inserted + text[at..], caret, caret);
    }

    public static EditorText Lines(EditorText current, LineStyle style)
    {
        string text = current.Text;
        int first = current.Start == 0 ? 0 : text.LastIndexOf('\n', current.Start - 1) + 1;
        int last = current.End > current.Start && current.End > 0 && text[current.End - 1] == '\n' ? current.End - 1 : current.End;
        int lineEnd = text.IndexOf('\n', last);
        lineEnd = lineEnd < 0 ? text.Length : lineEnd;

        string[] lines = text[first..lineEnd].Split('\n');
        bool allStyled = lines.Any(line => line.Length > 0) && lines.All(line => line.Length == 0 || Styled(line, style));
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
        LineStyle.Heading => "## ",
        LineStyle.Subheading => "### ",
        LineStyle.Bullets => "- ",
        LineStyle.Numbers => $"{index + 1}. ",
        _ => "> ",
    };

    private static bool Styled(string line, LineStyle style) => style switch
    {
        LineStyle.Heading => line.StartsWith("## ", StringComparison.Ordinal),
        LineStyle.Subheading => line.StartsWith("### ", StringComparison.Ordinal),
        LineStyle.Bullets => line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal),
        LineStyle.Numbers => NumberPrefixLength(line) > 0,
        _ => line.StartsWith('>'),
    };

    private static string Unstyled(string line)
    {
        foreach (string prefix in new[] { "### ", "## ", "- ", "* ", "> ", ">" })
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
